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
                string[] lineParsed = line.Split('\t');
                mCache.AddRange(GenerateCacheKeys(lineParsed[0], lineParsed[1], lineParsed[2]));
            }
            reader.Close();
        }

        static ArrayList<string> GenerateCacheKeys(string corpusId, string docId, string lbl)
        {
            ArrayList<string> keys = new ArrayList<string>();
            string[] lblParsed = lbl.Split('/');
            string partLbl = lblParsed[0];
            keys.Add(corpusId + "\t" + docId + "\t" + partLbl);
            //Console.WriteLine("caching " + partLbl);
            for (int i = 1; i < lblParsed.Length; i++)
            {
                partLbl += '/' + lblParsed[i];
                keys.Add(corpusId + "\t" + docId + "\t" + partLbl);
                //Console.WriteLine("caching " + partLbl);
            }
            return keys;
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
                string cacheKey = corpusId + "\t" + docId + "\t" + lbl;
                if (!mCache.Contains(cacheKey))
                {
                    Console.WriteLine("Retrieving document # {0} / {1} ...", i, docRefs.Length);
                    try
                    {
                        string txt = service.GetDoc(corpusId, docId, "txt", false/*ignored*/, /*changesOnly=*/false, time);
                        if (!txt.StartsWith("*** "))
                        {
                            txt = Utils.ToOneLine(txt, /*compact=*/true).Replace('\t', ' ');
                            corpus.WriteLine(lbl + "\t" + txt);
                            corpus.Flush();
                            cache.WriteLine(cacheKey);
                            cache.Flush();
                            mCache.AddRange(GenerateCacheKeys(corpusId, docId, lbl));
                        }
                        else
                        {
                            Console.WriteLine(txt); // error message from the service
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
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
                }
                else
                {
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
            ArrayList<KeyDat<int, Pair<string, string>>> tmp = new ArrayList<KeyDat<int, Pair<string, string>>>();
            foreach (Pair<string, string> item in tax)
            {
                int lvl = 1;
                foreach (char ch in item.First) { if (ch == '/') { lvl++; } }
                tmp.Add(new KeyDat<int, Pair<string, string>>(lvl, item));
            }
            tmp.Sort(DescSort<KeyDat<int, Pair<string, string>>>.Instance);
            foreach (KeyDat<int, Pair<string, string>> item in tmp)
            {
                Console.WriteLine("Grounding \"{0}\" with {1} ...", item.Dat.First, item.Dat.Second);
                RetrieveDocuments(item.Dat.Second, item.Dat.First, corpusFileOut, cacheFileOut, timeStart, timeEnd);
            }
            Console.WriteLine("Grounding done.");
        }

        static void Main(string[] args)
        {
            //LoadFromCache(@"C:\Work\DacqPipe\DataClient\YahooRssTx_cache_2.txt");
            ArrayList<Pair<string, string>> tax = LoadTaxonomy(@"C:\Work\DacqPipe\DataClient\YahooNewsCategories_2.txt");
            GroundTaxonomy(tax,
                @"C:\Work\DacqPipe\DataClient\YahooRssTx_2.txt", 
                @"C:\Work\DacqPipe\DataClient\YahooRssTx_cache_2.txt", 
                "2012-06-01", "2012-08-01");
            //LoadFromCache(@"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt");
            //RetrieveDocuments("http://finance.yahoo.com/rss/%", 
            //    "YahooFinance",
            //    @"C:\Work\DacqPipe\DataClient\YahooFinance.txt", 
            //    @"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt", 
            //    "2012-07-01", "2012-07-05");
        }
    }
}
