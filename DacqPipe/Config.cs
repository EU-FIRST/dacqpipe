using Latino;

namespace Dacq
{
    public static class Config 
    {
        // user-configurable
        public static readonly string LogFileName
            = Utils.GetConfigValue<string>("logFileName");
        public static readonly string DataSourcesFileName
            = Utils.GetConfigValue<string>("dataSourcesFileName", @".\RssSources.txt");
        public static readonly string XmlDataRoot
            = Utils.GetConfigValue<string>("xmlDataRoot", @".\Data");
        public static readonly string HtmlDataRoot
            = Utils.GetConfigValue<string>("htmlDataRoot", @".\DataHtml");
        // hard-coded
        public static readonly string WebSiteId
            = null;
        public static readonly string DbConnectionString
            = null;
        public static readonly string DbConnectionStringDump
            = null;
        public static readonly string SqlDbConnectionString
            = null;
        public static readonly string SqlDbConnectionStringNew
            = null;
        public static readonly string ClientIp
            = null;
        public static readonly string XmlDataRootDump
            = null;
        public static readonly string HtmlDataRootDump
            = null;
        public static readonly string XmlDataRootNew
            = XmlDataRoot;
        public static readonly string HtmlDataRootNew
            = HtmlDataRoot;        
        public static readonly string XmlDataRootDumpNew
            = null;
        public static readonly string HtmlDataRootDumpNew
            = null;
        public static readonly string Language
            = "English";
        public static readonly bool EnableZmq
            = false;
        public static readonly int SleepBetweenPolls
            = 15 * 60000;
        public static readonly int NumPipes
            = 1;
    }
}