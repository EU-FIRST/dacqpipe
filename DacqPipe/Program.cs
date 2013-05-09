/*==========================================================================
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Dacq.cs
 *  Desc:    Data acquisition pipeline (Dacq)
 *  Created: Feb-2011
 *
 *  Author:  Miha Grcar
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
            WebUtils.DefaultTimeout = 10000;
            WebUtils.NumRetries = 3;
            WebUtils.WaitBetweenRetries = 5000;
            WebUtils.ReadWriteTimeout = 30000;
            ServicePointManager.DefaultConnectionLimit = 8;
            
            DateTime startTime = DateTime.MinValue;
            Logger rootLogger = Logger.GetRootLogger();
            rootLogger.LocalLevel = Logger.Level.Debug; 
            string LOG_FILE_NAME = ConfigurationManager.AppSettings["logFileName"];
            rootLogger.LocalOutputType = Logger.OutputType.Console;
            if (LOG_FILE_NAME != null)
            {
                rootLogger.LocalOutputWriter = new StreamWriter(LOG_FILE_NAME, /*append=*/true);
                rootLogger.LocalOutputType |= Logger.OutputType.Writer;
            }
            string SOURCES_FILE_NAME = ConfigurationManager.AppSettings["dataSourcesFileName"];
            string WEB_SITE_ID = Utils.GetConfigValue("webSiteId", "dacq");
            string DB_CONNECTION_STRING = ConfigurationManager.AppSettings["dbConnectionString"];
            string SQL_DB_CONNECTION_STRING = ConfigurationManager.AppSettings["SqlDbConnectionString"];
            string DB_CONNECTION_STRING_DUMP = ConfigurationManager.AppSettings["dbConnectionStringDump"];
            string CLIENT_IP = ConfigurationManager.AppSettings["clientIp"];
            string XML_DATA_ROOT = ConfigurationManager.AppSettings["xmlDataRoot"];
            string XML_DATA_ROOT_DUMP = ConfigurationManager.AppSettings["xmlDataRootDump"];
            string HTML_DATA_ROOT = ConfigurationManager.AppSettings["htmlDataRoot"];
            string HTML_DATA_ROOT_DUMP = ConfigurationManager.AppSettings["htmlDataRootDump"];
            string XML_DATA_ROOT_NEW = XML_DATA_ROOT == null ? null : (XML_DATA_ROOT.TrimEnd('\\') + "\\" + "New");
            string HTML_DATA_ROOT_NEW = HTML_DATA_ROOT == null ? null : (HTML_DATA_ROOT.TrimEnd('\\') + "\\" + "New");
            string XML_DATA_ROOT_DUMP_NEW = XML_DATA_ROOT_DUMP == null ? null : (XML_DATA_ROOT_DUMP.TrimEnd('\\') + "\\" + "New");
            string HTML_DATA_ROOT_DUMP_NEW = HTML_DATA_ROOT_DUMP == null ? null : (HTML_DATA_ROOT_DUMP.TrimEnd('\\') + "\\" + "New");
            string DB_CONNECTION_STRING_NEW = ConfigurationManager.AppSettings["SqlDbConnectionStringNew"];
            string tmp = ConfigurationManager.AppSettings["enableZeroMQ"];
            bool ENABLE_ZEROMQ = tmp != null && new List<string>(new string[] { "true", "1", "yes", "on" }).Contains(tmp.ToLower());
            const int NUM_WRITERS = 8;
            const int SLEEP_BETWEEN_POLLS = 15 * 60000; // 15 minutes
            ArrayList<StreamDataProducerPoll> dataReaders = new ArrayList<StreamDataProducerPoll>();
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
                    listener.Prefixes.Add(string.Format("http://localhost/{0}/", WEB_SITE_ID));
                    listener.Prefixes.Add(string.Format("http://first.ijs.si/{0}/", WEB_SITE_ID));
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
                foreach (StreamDataProducerPoll c in dataReaders)
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
            // initialize database writers
            PassOnComponent lb = new PassOnComponent(); // data load balancer
            lb.DispatchPolicy = DispatchPolicy.BalanceLoadMax;
            dataConsumers.Add(lb);
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
                UrlTreeBoilerplateRemoverComponent.InitializeHistory(dbConnection);
                dbConnection.Disconnect();
                RssFeedComponent.DatabaseConnectionString = SQL_DB_CONNECTION_STRING;
            }
            for (int i = 0; i < NUM_WRITERS; i++)
            {
                DocumentCorpusWriterComponent dcw = new DocumentCorpusWriterComponent(DB_CONNECTION_STRING_DUMP, XML_DATA_ROOT_DUMP, HTML_DATA_ROOT_DUMP);
                DocumentWriterComponent dwc = new DocumentWriterComponent(/*connectionString=*/null, XML_DATA_ROOT_DUMP_NEW, HTML_DATA_ROOT_DUMP_NEW);
                dcw.IsDumpWriter = true;
                UrlTreeBoilerplateRemoverComponent bpr = new UrlTreeBoilerplateRemoverComponent(DB_CONNECTION_STRING);
                DocumentCorpusWriterComponent cw = new DocumentCorpusWriterComponent(DB_CONNECTION_STRING, XML_DATA_ROOT, HTML_DATA_ROOT);
                DocumentWriterComponent dw = new DocumentWriterComponent(DB_CONNECTION_STRING_NEW, XML_DATA_ROOT_NEW, HTML_DATA_ROOT_NEW);
                HtmlTokenizerComponent htc = new HtmlTokenizerComponent();
                SentenceSplitterComponent ssc = new SentenceSplitterComponent();
                EnglishTokenizerComponent tok = new EnglishTokenizerComponent();
                EnglishLemmatizerComponent lem = new EnglishLemmatizerComponent(EnglishLemmatizerComponent.Type.Both);
                EnglishPosTaggerComponent pt = new EnglishPosTaggerComponent();
                LanguageDetectorComponent ld = new LanguageDetectorComponent();
                DocumentFilterComponent df = new DocumentFilterComponent();
                df.OnFilterDocument += new DocumentFilterComponent.FilterDocumentHandler(delegate(Document document, Logger dfLogger) {
                    string docId = document.Features.GetFeatureValue("guid");
                    // remove items without link in RSS document
                    if (document.Features.GetFeatureValue("contentType") != "Text") 
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: contentType not Text (id={0}).", docId);
                        return false;                    
                    }
                    // remove items with blacklisted URL
                    if (document.Features.GetFeatureValue("blacklisted") == "True")
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: responseUrl blacklisted (id={0}).", docId);
                        return false;
                    }
                    // remove items with not enough content
                    if (Convert.ToInt32(document.Features.GetFeatureValue("bprContentCharCount")) < 100)
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: bprContentCharCount < 100 (id={0}).", docId);
                        return false;
                    }
                    // remove non-English items
                    if (document.Features.GetFeatureValue("detectedCharRange") != "Basic Latin")
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: detectedCharRange not Basic Latin (id={0}).", docId);
                        return false;
                    }
                    if (document.Features.GetFeatureValue("detectedLanguage") != "English")
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: detectedLanguage not English (id={0}).", docId);
                        return false;
                    }
                    // remove exact duplicates
                    if (document.Features.GetFeatureValue("unseenContent") == "No")
                    {
                        dfLogger.Info("OnFilterDocument", "Document rejected: no unseen content (id={0}).", docId);
                        return false;
                    }
                    return true;
                });
                ld.BlockSelector = "TextBlock/Content"; // due to problems with itar-tass.com
                df.SubscribeDumpConsumer(dcw);
                df.SubscribeDumpConsumer(dwc);

                lb.Subscribe(htc);
                htc.Subscribe(bpr);
                bpr.Subscribe(ld);
                ld.Subscribe(df);

                df.Subscribe(ssc);
                ssc.Subscribe(tok);
                tok.Subscribe(lem);
                lem.Subscribe(pt);

                pt.Subscribe(cw);
                pt.Subscribe(dw);
                if (ENABLE_ZEROMQ) { pt.Subscribe(zmq); }
                                                                                      
                dataConsumers.AddRange(new StreamDataConsumer[] { dcw, dwc, df, ld, htc, ssc, tok, pt, cw, dw, lem, bpr });         
            }
            // initialize stream simulator
            string offlineSource = ConfigurationManager.AppSettings["offlineSource"];
            if (offlineSource != null) 
            {
                DocumentStreamReaderComponent dsr = new DocumentStreamReaderComponent(offlineSource);
                dsr.Name = offlineSource;
                dataReaders.Add(dsr);
                dsr.Subscribe(lb);
            }
            // initialize RSS feed components
            int j = 0;
            RssFeedComponent rssComp = null;
            Set<string> sites = new Set<string>();
            if (SOURCES_FILE_NAME != null)
            {
                string[] sources = File.ReadAllLines(SOURCES_FILE_NAME);
                foreach (string _url in sources)
                {
                    string url = _url.Trim();
                    if (url != "" && !url.StartsWith("#"))
                    {
                        Match m;
                        if ((m = Regex.Match(url, @"^site\s*:(?<siteId>.*)$", RegexOptions.IgnoreCase)).Success)
                        {
                            string siteId = m.Result("${siteId}").Trim().ToLower();
                            if (sites.Contains(siteId)) { throw new Exception(string.Format("Duplicated site identifier ({0}).", siteId)); }
                            sites.Add(siteId);
                            rssComp = new RssFeedComponent(siteId);
                            rssComp.MaxDocsPerCorpus = Convert.ToInt32(Utils.GetConfigValue("MaxDocsPerCorpus", "50"));
                            rssComp.RandomDelayAtStart = new ArrayList<string>("yes,on,true,1,y".Split(','))
                                .Contains(Utils.GetConfigValue("RandomDelayAtStart", "true").ToLower());
                            rssComp.Name = siteId;
                            rssComp.TimeBetweenPolls = SLEEP_BETWEEN_POLLS;
                            rssComp.IncludeRssXml = true;
                            if (SQL_DB_CONNECTION_STRING != null)
                            {
                                rssComp.Initialize(SQL_DB_CONNECTION_STRING);
                            }
                            rssComp.IncludeRawData = true;
                            rssComp.Subscribe(lb);
                            dataReaders.Add(rssComp);
                            j++;
                        }
                        else if (rssComp != null)
                        {
                            rssComp.AddSource(url);
                        }
                    }
                }
            }
            foreach (StreamDataProducerPoll c in dataReaders)
            {
                c.Start();
            }
            foreach (IWorkflowComponent obj in dataReaders) { components.Add(obj, Guid.NewGuid()); }
            foreach (IWorkflowComponent obj in dataConsumers) { components.Add(obj, Guid.NewGuid()); }
            startTime = DateTime.Now;
            while (!exit) { Thread.Sleep(500); }
            // shut down gracefully
            logger.Info("Main", "Please wait while shutting down ...");
            // wait for HTTP server shutdown
            while (httpServerRunning) { Thread.Sleep(500); }
            // stop RSS components
            foreach (StreamDataProducerPoll c in dataReaders)
            {                
                c.Stop();
            }
            foreach (StreamDataProducerPoll c in dataReaders)
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