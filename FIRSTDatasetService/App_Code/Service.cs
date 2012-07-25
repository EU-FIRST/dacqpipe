using System;
using System.Web.Services;
using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using Latino;
using Latino.Workflows.TextMining;

[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class Service : WebService
{
    [WebMethod]
    public string Ping() 
    {
        return "Pong!";
    }

    [WebMethod]
    public string GetDoc(string corpusId, string docId, string format, bool rmvRaw, bool changesOnly, string time)
    {
        string dataPath = Utils.GetConfigValue("DataPath", ".");
        if (corpusId == null || corpusId.Replace("-", "").Length != 32) { return "*** Invalid corpus ID."; }
        corpusId = corpusId.Replace("-", "");
        if (docId == null || docId.Replace("-", "").Length != 32) { return "*** Invalid document ID."; }
        docId = docId.Replace("-", "");
        string[] fileNames = null;
        try
        {
            DateTime dt = DateTime.Parse(time);
            string prefix = dt.ToString("HH_mm_ss_");
            string path = "\\" + dt.Year + "\\" + dt.Month + "\\" + dt.Day + "\\";
            string fileName = dataPath.TrimEnd('\\') + path + prefix + corpusId + ".xml";
            if (!Utils.VerifyFileNameOpen(fileName)) { return "*** Corpus not found."; }
            fileNames = new string[] { fileName };
        }
        catch { }
        if (fileNames == null) { fileNames = Directory.GetFiles(dataPath, "*" + corpusId + ".xml", SearchOption.AllDirectories); }
        if (fileNames.Length == 0) { return "*** Corpus not found."; }
        DocumentCorpus corpus = new DocumentCorpus();
        StreamReader reader = new StreamReader(fileNames[0]);
        XmlTextReader xmlReader = new XmlTextReader(reader);
        corpus.ReadXml(xmlReader);
        xmlReader.Close();
        reader.Close();
        Document document = null;
        foreach (Document doc in corpus.Documents)
        {
            if (new Guid(doc.Features.GetFeatureValue("guid")).ToString("N") == docId) { document = doc; break; }
        }
        if (document == null) { return "*** Document not found."; }
        if (rmvRaw) { document.Features.RemoveFeature("raw"); }
        string response;
        if (format == "html")
        {
            StringWriter writer = new StringWriter();
            document.MakeHtmlPage(writer, /*inlineCss=*/true);
            string html = new Regex(@"<!--back_button-->.*?<!--/back_button-->").Replace(writer.ToString(), "");
            response = html;
        }
        else if (format == "txt")
        {
            StringBuilder txt = new StringBuilder();
            string selector = "TextBlock/Content";
            if (changesOnly && document.Features.GetFeatureValue("rev") != "1") { selector = "TextBlock/Content/Unseen"; }
            foreach (TextBlock block in document.GetAnnotatedBlocks(selector))
            {
                txt.AppendLine(block.Text);
            }
            response = document.Name + "\r\n\r\n" + txt.ToString();
        }
        else
        {
            StringWriter writer = new StringWriter();
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter xmlWriter = XmlWriter.Create(writer, xmlSettings);
            if (format == "gate_xml")
            {
                document.WriteGateXml(xmlWriter, /*writeTopElement=*/true, /*removeBoilerplate=*/true);
                xmlWriter.Flush();
                response = writer.ToString();
            }
            else // xml
            {
                document.WriteXml(xmlWriter, /*writeTopElement=*/true);
                xmlWriter.Flush();
                response = writer.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", 
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            }
            xmlWriter.Close();
        }        
        return response;
    }
}
