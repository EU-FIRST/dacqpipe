/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    Dacq.cs
 *  Desc:    Data acquisition pipeline cmd line utility
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
using Latino;
using Latino.Workflows;
using Latino.Workflows.TextMining;
using Latino.Workflows.Persistance;
using Latino.Web;
using Latino.Persistance;

namespace Dacq
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger rootLogger = Logger.GetRootLogger();
            rootLogger.LocalLevel = Logger.Level.Debug;
            rootLogger.LocalOutputType = Logger.OutputType.Console | Logger.OutputType.Writer;
            rootLogger.LocalWriter = new StreamWriter("c:\\work\\dacqpipe\\log.txt");
            const string SOURCES_FILE_NAME = "c:\\work\\dacqpipe\\rsssources\\RssSourcesBig.txt";
            const string DB_CONNECTION_STRING = "Provider=SQLNCLI10;Server=(local);Database=DacqPipeTmp;Trusted_Connection=Yes";
            //rootLogger.LocalWriter = new StreamWriter(@"E:\Users\miha\Work\DacqPipeBig\log.txt");
            //const string SOURCES_FILE_NAME = @"E:\Users\miha\Work\DacqPipeBig\RssSourcesBig.txt";
            //const string DB_CONNECTION_STRING = "Provider=SQLNCLI10;Server=(local);Database=DacqPipeBig;Trusted_Connection=Yes";
            //const string PROCESSOR_AFFINITY = "111111";
            const int SLEEP_BETWEEN_INITS = 0;
            const int NUM_WRITERS = 10;
            const int SLEEP_BETWEEN_POLLS = 15 * 60 * 1000; // 15 minutes
            // init logging and history
            Logger logger = Logger.GetLogger("Latino.Workflows.Dacq");        
            //WorkflowUtils.SetProcessorAffinity(PROCESSOR_AFFINITY);
            bool exit = false;
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                logger.Info("Main", "*** Ctrl-C command received. ***");
                e.Cancel = true;
                exit = true;
            };            
            logger.Info("Main", "Starting Dacq ...");
            string[] sources = File.ReadAllLines(SOURCES_FILE_NAME);
            ArrayList<RssFeedComponent> rssComponents = new ArrayList<RssFeedComponent>();
            ArrayList<DocumentCorpusWriterComponent> writers = new ArrayList<DocumentCorpusWriterComponent>();
            // initialize database writers
            for (int i = 0; i < NUM_WRITERS; i++)
            {
                DocumentCorpusWriterComponent c = new DocumentCorpusWriterComponent(DB_CONNECTION_STRING);
                writers.Add(c);
            }                   
            // initialize RSS feed components
            int j = 0;
            RssFeedComponent rssComp = null;
            foreach (string _url in sources)
            {
                string url = _url.Trim();
                if (url != "" && !url.StartsWith("#"))
                {
                    Match m;
                    if ((m = Regex.Match(url, @"^site\s*:(?<siteId>.*)$", RegexOptions.IgnoreCase)).Success)
                    {
                        DatabaseConnection historyDatabase = new DatabaseConnection();
                        historyDatabase.ConnectionString = DB_CONNECTION_STRING;
                        historyDatabase.Connect();
                        string siteId = m.Result("${siteId}").Trim();
                        rssComp = new RssFeedComponent(siteId);
                        rssComp.TimeBetweenPolls = SLEEP_BETWEEN_POLLS;
                        rssComp.IncludeRssXml = true;
                        rssComp.HistoryDatabase = historyDatabase;
                        rssComp.LoadHistory();
                        //rssComp.IncludeRawData = true;                      
                        rssComp.Subscribe(writers[j % NUM_WRITERS]);
                        rssComponents.Add(rssComp);
                        j++;
                    }
                    else if (rssComp != null) 
                    {
                        rssComp.AddSource(url);
                    }
                }
            }
            j = 0;
            foreach (RssFeedComponent c in rssComponents)
            {
                c.Start();
                if (exit) { break; }
                if (sources.Length > j + 1) { Thread.Sleep(SLEEP_BETWEEN_INITS); }
                if (exit) { break; }
                j++;
            }
            while (!exit) { Thread.Sleep(500); }
            // shut down gracefully
            logger.Info("Main", "Please wait while shutting down ...");
            foreach (RssFeedComponent c in rssComponents)
            {                
                c.Stop();
            }
            foreach (RssFeedComponent c in rssComponents)
            {
                while (c.IsRunning) { Thread.Sleep(500); }
                c.HistoryDatabase.Disconnect();
            }
            // wait for all writers to finish
            bool _exit = true;
            do
            {
                foreach (DocumentCorpusWriterComponent c in writers)
                {
                    if (c.QueueSize == 0 && c.IsRunning)
                    {
                        c.Dispose();
                    }
                    if (c.IsRunning) { _exit = false; }
                }
                Thread.Sleep(500);
            } while (!_exit);
            logger.Info("Main", "Dacq successfully stopped.");
        }
    }
}
