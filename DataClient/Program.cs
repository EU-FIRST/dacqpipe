using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Latino;

namespace DataClient
{
    class Program
    {
        static Set<string> mCache
            = new Set<string>();

        static void LoadFromCache(string cacheFile)
        {
            StreamReader reader = new StreamReader(cacheFile);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parsed = line.Split('\t');
                if (parsed.Length >= 2)
                {
                    mCache.Add(parsed[0] + " " + parsed[1]);
                }
            }
            reader.Close();
        }

        static void RetrieveDocuments(string sourceUrl, string lbl, string corpusFileOut, string cacheFileOut, string timeStart, string timeEnd)
        {
            Debug.Assert(!lbl.Contains("\t"));
            StreamWriter corpus = new StreamWriter(corpusFileOut, /*append=*/Utils.VerifyFileNameOpen(corpusFileOut), Encoding.UTF8);
            StreamWriter cache = new StreamWriter(cacheFileOut, /*append=*/Utils.VerifyFileNameOpen(cacheFileOut));
            DataService service = new DataService();
            Console.WriteLine("Retrieving document references ...");
            string[][] docRefs = service.GetDocRefs(sourceUrl, timeStart, timeEnd);
            int i = 0;
            foreach (string[] row in docRefs)
            {
                string time = row[0];
                string corpusId = row[1];
                string docId = row[2];
                i++;
                if (!mCache.Contains(corpusId + " " + docId))
                {
                    Console.WriteLine("Retrieving document # {0} / {1} ...", i, docRefs.Length);
                    string txt = service.GetDoc(corpusId, docId, "txt", false/*ignored*/, /*changesOnly=*/false, time);
                    if (!txt.StartsWith("*** "))
                    {
                        txt = Utils.ToOneLine(txt, /*compact=*/true).Replace('\t', ' ');
                        corpus.WriteLine(lbl + "\t" + txt);
                        corpus.Flush();
                        cache.WriteLine("{0:N}\t{1:N}", corpusId, docId);
                        cache.Flush();
                        mCache.Add(corpusId + " " + docId);
                    }
                    else
                    {
                        Console.WriteLine(txt); // error message from the service
                    }
                }
                else
                {
                    Console.WriteLine("*** Document found in cache.");
                }
            }
            corpus.Close();
            cache.Close();
        }

        static void UpdateLbl(string line, ArrayList<string> lblArr)
        {
            int lvl = 0;
            foreach (char ch in line)
            {
                if (ch == '\t') { lvl++; }
                else { break; }
            }
            string lbl = line.Trim();
            while (lblArr.Count < lvl) { lblArr.Add("?"); }
            if (lblArr.Count > lvl) { lblArr.RemoveRange(lvl, lblArr.Count - lvl); }
            lblArr.Add(lbl);
        }

        static ArrayList<Pair<string, string>> LoadTaxonomy(string fileName)
        {
            ArrayList<Pair<string, string>> tax = new ArrayList<Pair<string, string>>();
            string[] lines = File.ReadAllLines(fileName);
            ArrayList<string> lblArr = new ArrayList<string>();
            foreach (string line in lines)
            {
                string ln = line.Trim();
                if (!ln.StartsWith("http://"))
                {
                    Debug.Assert(!ln.Contains("/"));
                    UpdateLbl(line, lblArr);
                    //Console.WriteLine(lblArr);
                }
                else
                {
                    //Console.WriteLine("URL " + ln);
                    string lblFull = "";
                    foreach (string lbl in lblArr)
                    {
                        lblFull += lbl + "/";
                    }
                    tax.Add(new Pair<string, string>(lblFull.TrimEnd('/'), ln));                    
                }
            }
            return tax;
        }

        static void GroundTaxonomy(ArrayList<Pair<string, string>> tax, string corpusFileOut, string cacheFileOut, string timeStart, string timeEnd)
        {
            foreach (Pair<string, string> item in tax)
            {
                Console.WriteLine("Grounding \"{0}\" with {1} ...", item.First, item.Second);
                RetrieveDocuments(item.Second, item.First, corpusFileOut, cacheFileOut, timeStart, timeEnd);
            }
            Console.WriteLine("Grounding done.");
        }

        static void Main(string[] args)
        {
            ArrayList<Pair<string, string>> tax = LoadTaxonomy(@"C:\Work\DacqPipe\DataClient\YahooNewsCategories.txt");
            GroundTaxonomy(tax,
                @"C:\Work\DacqPipe\DataClient\YahooRssTx.txt", 
                @"C:\Work\DacqPipe\DataClient\YahooRssTx_cache.txt", 
                "2012-07-01", "2012-07-05");
            //LoadFromCache(@"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt");
            //RetrieveDocuments("http://finance.yahoo.com/rss/%", 
            //    "YahooFinance",
            //    @"C:\Work\DacqPipe\DataClient\YahooFinance.txt", 
            //    @"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt", 
            //    "2012-07-01", "2012-07-05");
        }
    }
}
