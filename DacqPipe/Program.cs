/*==========================================================================
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Dacq.cs
 *  Desc:    Data acquisition pipeline (Dacq)
 *  Created: Feb-2011
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System.Configuration;
using Latino;
using Latino.Web;
using Latino.Persistance;
using Latino.TextMining;
using Latino.Workflows;
using Latino.Workflows.TextMining;
using Latino.Workflows.WebMining;
using Latino.Workflows.Persistance;

namespace Dacq
{
    class Program
    {
        static string TIME_FORMAT 
            = "yyyy-MM-dd HH:mm:ss";

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.MinValue;
            Logger rootLogger = Logger.GetRootLogger();
            rootLogger.LocalLevel = Logger.Level.Debug; 
            string LOG_FILE_NAME = ConfigurationManager.AppSettings["logFileName"];
            rootLogger.LocalOutputType = Logger.OutputType.Console;
            if (LOG_FILE_NAME != null)
            {
                rootLogger.OutputWriter = new StreamWriter(LOG_FILE_NAME, /*append=*/true);
                rootLogger.LocalOutputType |= Logger.OutputType.Writer;
            }
            string SOURCES_FILE_NAME = ConfigurationManager.AppSettings["dataSourcesFileName"];
            string DB_CONNECTION_STRING = ConfigurationManager.AppSettings["dbConnectionString"];
            string DB_CONNECTION_STRING_DUMP = ConfigurationManager.AppSettings["dbConnectionStringDump"];
            string CLIENT_IP = ConfigurationManager.AppSettings["clientIp"];
            string XML_DATA_ROOT = ConfigurationManager.AppSettings["xmlDataRoot"];
            string XML_DATA_ROOT_DUMP = ConfigurationManager.AppSettings["xmlDataRootDump"];
            string HTML_DATA_ROOT = ConfigurationManager.AppSettings["htmlDataRoot"];
            string HTML_DATA_ROOT_DUMP = ConfigurationManager.AppSettings["htmlDataRootDump"];
            string tmp = ConfigurationManager.AppSettings["enableZeroMQ"];
            bool ENABLE_ZEROMQ = tmp != null && new List<string>(new string[] { "true", "1", "yes", "on" }).Contains(tmp.ToLower());
            const int NUM_WRITERS = 8;
            const int NUM_DUMP_WRITERS = 8;
            const int SLEEP_BETWEEN_POLLS = 15 * 60000; // 15 minutes
            ArrayList<RssFeedComponent> rssComponents = new ArrayList<RssFeedComponent>();
            ArrayList<StreamDataConsumer> dataConsumers = new ArrayList<StreamDataConsumer>();
            Dictionary<IWorkflowComponent, Guid> components = new Dictionary<IWorkflowComponent, Guid>();
            // init logging 
            Logger logger = Logger.GetLogger("Latino.Workflows.Dacq");        
            // start HTTP server
            bool exit = false;
            bool httpServerRunning = false;
            if (CLIENT_IP != null)
            {
                new Thread(new ThreadStart(delegate()
                {
                    HttpListener listener = new HttpListener();
                    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    listener.Prefixes.Add("http://localhost/dacqexp/");
                    listener.Prefixes.Add("http://first.ijs.si/dacqexp/");
                    listener.Start();
                    logger.Info("Main.HttpServer", "HTTP server started.");
                    httpServerRunning = true;
                    DateTime prevRequestTime = DateTime.MinValue;
                    while (!exit)
                    {
                        try
                        {
                            HttpListenerContext ctx = null;
                            listener.BeginGetContext(new AsyncCallback(delegate(IAsyncResult ar)
                            {
                                try { ctx = listener.EndGetContext(ar); }
                                catch { }
                            }), /*state=*/null);
                            while (!exit && ctx == null) { Thread.Sleep(500); }
                            if (!exit)
                            {
                                // process requests one by one                                 
                                ctx.Response.AppendHeader("Content-Type", "application/xml");
                                XmlWriterSettings settings = new XmlWriterSettings();
                                settings.Encoding = Encoding.UTF8;
                                settings.CheckCharacters = false;
                                settings.Indent = true;
                                XmlWriter w = XmlTextWriter.Create(ctx.Response.OutputStream, settings);
                                w.WriteStartElement("DacqResponse");
                                w.WriteElementString("DacqStartTime", startTime.ToString(TIME_FORMAT));
                                if (prevRequestTime == DateTime.MinValue) { prevRequestTime = startTime; }
                                DateTime thisRequestTime = DateTime.Now;
                                string command = GetHttpRequestCommand(ctx.Request.Url.ToString());
                                if (command == "components")
                                {                                    
                                    w.WriteElementString("PreviousRequestTime", prevRequestTime.ToString(TIME_FORMAT));
                                    w.WriteElementString("ThisRequestTime", thisRequestTime.ToString(TIME_FORMAT));
                                }
                                w.WriteElementString("Request", ctx.Request.Url.ToString());                                
                                w.WriteElementString("Command", command);
                                w.WriteStartElement("ResponseBody");
                                if (command == "help")
                                {
                                    WriteSupportedCommands(w);  
                                }
                                else if (command == "components")
                                {
                                    WriteComponentInfo(w, components, thisRequestTime, prevRequestTime);
                                    prevRequestTime = thisRequestTime;
                                }
                                else if (command == "sources")
                                {
                                    WriteRssInfo(w, components);
                                }
                                w.WriteEndElement(); // ResponseBody
                                w.WriteEndElement(); // DacqResponse
                                w.Close();
                                ctx.Response.Close();
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Warn("Main.HttpServer", e);
                        }
                    }
                    listener.Stop();
                    logger.Info("Main.HttpServer", "HTTP server stopped.");
                    httpServerRunning = false;
                })).Start();
            }
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                logger.Info("Main", "*** Ctrl-C command received. ***");
                e.Cancel = true;
                exit = true;
                string componentsStr = "";
                foreach (RssFeedComponent c in rssComponents)
                {
                    if (c.IsRunning) { componentsStr += "\r\n" + c.GetType() + " : " + c.Name; }
                }
                foreach (StreamDataConsumer dataConsumer in dataConsumers)
                {
                    if (dataConsumer.IsRunning) { componentsStr += "\r\n" + dataConsumer.GetType() + " : " + dataConsumer.Load; }
                }
                logger.Info("Main", "Active components:" + componentsStr);
            };            
            logger.Info("Main", "Starting Dacq ...");
            string[] sources = File.ReadAllLines(SOURCES_FILE_NAME);
            // initialize database writers
            PassOnComponent lb = new PassOnComponent(); // load balancer
            lb.DispatchPolicy = DispatchPolicy.BalanceLoadMax;
            dataConsumers.Add(lb);
            PassOnComponent dlb = new PassOnComponent(); // dump load balancer
            dlb.DispatchPolicy = DispatchPolicy.BalanceLoadMax;
            dataConsumers.Add(dlb);
            ZeroMqEmitterComponent zmq = null;
            if (ENABLE_ZEROMQ)
            {
                zmq = new ZeroMqEmitterComponent();
                dataConsumers.Add(zmq);
            }
            DatabaseConnection dbConnection = new DatabaseConnection();
            if (DB_CONNECTION_STRING != null)
            {
                dbConnection.ConnectionString = DB_CONNECTION_STRING;
                dbConnection.Connect();
                UrlFilterComponent.InitializeHistory(dbConnection);
                UrlTreeBoilerplateRemoverComponent.InitializeHistory(dbConnection);
                dbConnection.Disconnect();
            }
            for (int i = 0; i < NUM_WRITERS; i++)
            {
                UrlFilterComponent ufc = new UrlFilterComponent(DB_CONNECTION_STRING); 
                UrlTreeBoilerplateRemoverComponent bpr = new UrlTreeBoilerplateRemoverComponent(DB_CONNECTION_STRING);
                DocumentCorpusWriterComponent cw = new DocumentCorpusWriterComponent(DB_CONNECTION_STRING, XML_DATA_ROOT, HTML_DATA_ROOT);                
                HtmlTokenizerComponent htc = new HtmlTokenizerComponent();
                SentenceSplitterComponent ssc = new SentenceSplitterComponent();
                EnglishTokenizerComponent tok = new EnglishTokenizerComponent();
                EnglishLemmatizerComponent lem = new EnglishLemmatizerComponent(EnglishLemmatizerComponent.Type.Both);
                EnglishPosTaggerComponent pt = new EnglishPosTaggerComponent();
                LanguageDetectorComponent ld = new LanguageDetectorComponent();
                ld.BlockSelector = "TextBlock/Content"; // due to problems with itar-tass.com
                pt.Subscribe(cw);
                if (ENABLE_ZEROMQ) { pt.Subscribe(zmq); }
                tok.Subscribe(lem);
                lem.Subscribe(pt);
                ssc.Subscribe(tok);
                ld.Subscribe(ssc);
                htc.Subscribe(bpr);
                bpr.Subscribe(ld);
                dataConsumers.AddRange(new StreamDataConsumer[] { ld, htc, ssc, tok, pt, cw, bpr, ufc, lem });
                ufc.Subscribe(htc);
                lb.Subscribe(ufc);
                ufc.SubscribeDumpConsumer(dlb);
            }
            for (int i = 0; i < NUM_DUMP_WRITERS; i++)
            {
                DocumentCorpusWriterComponent c = new DocumentCorpusWriterComponent(DB_CONNECTION_STRING_DUMP, XML_DATA_ROOT_DUMP, HTML_DATA_ROOT_DUMP);
                c.IsDumpWriter = true;
                dlb.Subscribe(c);
                dataConsumers.Add(c);
            }
            // initialize RSS feed components
            int j = 0;
            RssFeedComponent rssComp = null;
            Set<string> sites = new Set<string>();
            foreach (string _url in sources)
            {
                string url = _url.Trim();
                if (url != "" && !url.StartsWith("#"))
                {
                    Match m;
                    if ((m = Regex.Match(url, @"^site\s*:(?<siteId>.*)$", RegexOptions.IgnoreCase)).Success)
                    {
                        string siteId = m.Result("${siteId}").Trim();
                        if (sites.Contains(siteId)) { logger.Warn("Main", "Duplicated site identifier ({0}).", siteId); }
                        sites.Add(siteId);
                        rssComp = new RssFeedComponent(siteId);
                        rssComp.Name = siteId;
                        rssComp.TimeBetweenPolls = SLEEP_BETWEEN_POLLS;
                        rssComp.IncludeRssXml = true;
                        if (DB_CONNECTION_STRING != null)
                        {
                            dbConnection.ConnectionString = DB_CONNECTION_STRING;
                            dbConnection.Connect();
                            rssComp.Initialize(dbConnection);
                            dbConnection.Disconnect();
                        }                        
                        rssComp.IncludeRawData = true;
                        rssComp.Subscribe(lb); 
                        rssComponents.Add(rssComp);
                        j++;
                    }
                    else if (rssComp != null) 
                    {
                        rssComp.AddSource(url);
                    }
                }
            }
            foreach (RssFeedComponent c in rssComponents)
            {
                c.Start();
            }
            foreach (IWorkflowComponent obj in rssComponents) { components.Add(obj, Guid.NewGuid()); }
            foreach (IWorkflowComponent obj in dataConsumers) { components.Add(obj, Guid.NewGuid()); }
            startTime = DateTime.Now;
            while (!exit) { Thread.Sleep(500); }
            // shut down gracefully
            logger.Info("Main", "Please wait while shutting down ...");
            // wait for HTTP server shutdown
            while (httpServerRunning) { Thread.Sleep(500); }
            // stop RSS components
            foreach (RssFeedComponent c in rssComponents)
            {                
                c.Stop();
            }
            foreach (RssFeedComponent c in rssComponents)
            {
                while (c.IsRunning) { Thread.Sleep(500); }
            }
            // wait for all data consumers to finish 
            foreach (StreamDataConsumer dataConsumer in dataConsumers)
            {
                if (dataConsumer.IsRunning) { while (!dataConsumer.IsSuspended) { Thread.Sleep(500); } }
                dataConsumer.Dispose(); 
            }
            logger.Info("Main", "Dacq successfully stopped.");
        }

        static string[] mCommands 
            = new string[] { 
                "help", 
                "components",
                "sources"
            };

        static string GetHttpRequestCommand(string url)
        {
            foreach (string command in mCommands)
            {
                if (url.EndsWith(command)) { return command; }
            }
            return "help";
        }

        static void WriteSupportedCommands(XmlWriter w)
        {
            string[] desc = new string[] { 
                "Information about the supported commands.", 
                "Information about the pipeline components.",
                "Information about the RSS sources."
            };
            int i = 0;
            foreach (string command in mCommands)
            {
                w.WriteStartElement("SupportedCommand");
                w.WriteElementString("Identifier", command);
                w.WriteElementString("Description", desc[i++]);
                w.WriteEndElement(); // SupportedCommand
            }
        }

        static void WriteComponentInfo(XmlWriter w, Dictionary<IWorkflowComponent, Guid> components, DateTime thisRequestTime, DateTime prevRequestTime)
        {
            TimeSpan interval = thisRequestTime - prevRequestTime;
            int c = 0;
            int maxMaxLoad = 0;
            string maxMaxLoadRef = "";
            foreach (IWorkflowComponent component in components.Keys)
            {
                w.WriteStartElement("Component");
                w.WriteAttributeString("id", components[component].ToString("N"));
                string type = (component is IDataConsumer && component is IDataProducer) ? "Processor" : (component is IDataConsumer ? "Consumer" : "Producer");
                w.WriteElementString("Type", type);
                w.WriteElementString("Class", component.GetType().ToString());
                string name = null;
                if (component is StreamDataConsumer)
                {
                    StreamDataConsumer consumer = (StreamDataConsumer)component;
                    int maxLoad = consumer.GetMaxLoad();
                    w.WriteElementString("IntervalMaxLoad", maxLoad.ToString());
                    double processingTimePerc = consumer.GetProcessingTimeSec(thisRequestTime) / interval.TotalSeconds * 100.0;                    
                    w.WriteElementString("IntervalProcessingTimePerc", processingTimePerc.ToString());
                    double throughput = (double)consumer.GetNumItemsProcessed() / interval.TotalSeconds;
                    w.WriteElementString("IntervalThroughput", throughput.ToString());
                    if (consumer is DocumentConsumer || consumer is DocumentProcessor)
                    {
                        double docThroughput = (double)consumer.GetNumDocumentsProcessed() / interval.TotalSeconds;
                        w.WriteElementString("IntervalThroughputDocs", docThroughput.ToString());
                    }
                    if (maxLoad == maxMaxLoad)
                    {
                        maxMaxLoadRef += components[component].ToString("N") + ",";
                    }
                    else if (maxLoad > maxMaxLoad)
                    {
                        maxMaxLoadRef = components[component].ToString("N") + ",";
                        maxMaxLoad = maxLoad;
                    }
                    name = consumer.Name;
                }
                if (component is StreamDataProducer)
                {
                    StreamDataProducer producer = (StreamDataProducer)component;
                    //foreach (IDataConsumer consumer in producer.SubscribedConsumers)
                    //{
                    //    w.WriteStartElement("SubscribedConsumer");
                    //    w.WriteAttributeString("ref", components[consumer].ToString("N"));
                    //    w.WriteEndElement(); // SubscribedConsumer
                    //}
                    name = producer.Name;
                }
                if (name != null) { w.WriteElementString("Name", name); }
                else { w.WriteStartElement("Name"); w.WriteAttributeString("null", "true"); w.WriteEndElement(); }
                w.WriteEndElement(); // Component
                c++;
            }
            w.WriteElementString("ComponentCount", c.ToString());
            w.WriteStartElement("MaxMaxLoad");
            w.WriteAttributeString("ref", maxMaxLoadRef.TrimEnd(','));
            w.WriteString(maxMaxLoad.ToString());
            w.WriteEndElement(); // MaxMaxLoad
        }

        static void WriteRssInfo(XmlWriter w, Dictionary<IWorkflowComponent, Guid> components)
        {
            int numberOfSites = 0;
            int totalNumberOfSources = 0;
            foreach (IWorkflowComponent component in components.Keys)
            {
                if (component is RssFeedComponent)
                {
                    RssFeedComponent rssComponent = (RssFeedComponent)component;
                    w.WriteStartElement("RssFeedComponent");
                    w.WriteAttributeString("id", components[component].ToString("N"));
                    if (rssComponent.SiteId != null) { w.WriteElementString("SiteId", rssComponent.SiteId); }
                    else { w.WriteStartElement("SiteId"); w.WriteAttributeString("null", "true"); w.WriteEndElement(); } 
                    w.WriteElementString("NumberOfSources", rssComponent.Sources.Count.ToString());
                    foreach (string sourceUrl in rssComponent.Sources)
                    {
                        w.WriteElementString("Source", sourceUrl);
                    }
                    totalNumberOfSources += rssComponent.Sources.Count;
                    w.WriteEndElement(); // RssFeedComponent
                    numberOfSites++;
                }
            }
            w.WriteElementString("TotalNumberOfSources", totalNumberOfSources.ToString());
            w.WriteElementString("NumberOfSites", numberOfSites.ToString());
        }
    }
}