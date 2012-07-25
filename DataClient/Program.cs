using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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

        static void RetrieveDocuments(string sourceUrl, string corpusFileOut, string cacheFileOut)
        {
            StreamWriter corpus = new StreamWriter(corpusFileOut, /*append=*/Utils.VerifyFileNameOpen(corpusFileOut), Encoding.UTF8);
            StreamWriter cache = new StreamWriter(cacheFileOut, /*append=*/Utils.VerifyFileNameOpen(cacheFileOut));
            DataService service = new DataService();
            Console.WriteLine("Retrieving document references ...");
            string[][] docRefs = service.GetDocRefs("http://finance.yahoo.com/rss/%");
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
                        txt = Utils.ToOneLine(txt, /*compact=*/true);
                        corpus.WriteLine(txt);
                        corpus.Flush();
                        cache.WriteLine("{0:N}\t{1:N}", corpusId, docId);
                        cache.Flush();
                        mCache.Add(corpusId + " " + docId);
                    }
                    else
                    {
                        Console.WriteLine(txt);
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

        static void Main(string[] args)
        {
            LoadFromCache(@"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt");
            RetrieveDocuments("http://finance.yahoo.com/rss/%", @"C:\Work\DacqPipe\DataClient\YahooFinance.txt", @"C:\Work\DacqPipe\DataClient\YahooFinance_cache.txt");
        }
    }
}
