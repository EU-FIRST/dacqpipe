using Latino;

namespace Dacq
{
    public static class Config
    {
        // user-configurable
        public static readonly string LogFileName
            = Utils.GetConfigValue<string>("logFileName");
        public static readonly string DataSourcesFileName
            = Utils.GetConfigValue<string>("dataSourcesFileName");
        public static readonly string WebSiteId
            = Utils.GetConfigValue<string>("webSiteId", "dacq");
        public static readonly string DbConnectionString
            = Utils.GetConfigValue<string>("dbConnectionString");
        public static readonly string DbConnectionStringDump
            = Utils.GetConfigValue<string>("dbConnectionStringDump");
        public static readonly string SqlDbConnectionString
            = Utils.GetConfigValue<string>("SqlDbConnectionString"); // *** inconsistent casing
        public static readonly string SqlDbConnectionStringNew
            = Utils.GetConfigValue<string>("SqlDbConnectionStringNew"); // *** inconsistent casing
        public static readonly string ClientIp
            = Utils.GetConfigValue<string>("clientIp");
        public static readonly string XmlDataRoot
            = Utils.GetConfigValue<string>("xmlDataRoot");
        public static readonly string XmlDataRootDump
            = Utils.GetConfigValue<string>("xmlDataRootDump");
        public static readonly string HtmlDataRoot
            = Utils.GetConfigValue<string>("htmlDataRoot");
        public static readonly string HtmlDataRootDump
            = Utils.GetConfigValue<string>("htmlDataRootDump");
        public static readonly string XmlDataRootNew
            = XmlDataRoot == null ? null : (XmlDataRoot.TrimEnd('\\') + "\\" + "New");
        public static readonly string XmlDataRootDumpNew
            = XmlDataRootDump == null ? null : (XmlDataRootDump.TrimEnd('\\') + "\\" + "New");
        public static readonly string HtmlDataRootNew
            = HtmlDataRoot == null ? null : (HtmlDataRoot.TrimEnd('\\') + "\\" + "New");
        public static readonly string HtmlDataRootDumpNew
            = HtmlDataRootDump == null ? null : (HtmlDataRootDump.TrimEnd('\\') + "\\" + "New");
        public static readonly string Language
            = Utils.GetConfigValue<string>("Language", "English"); // *** inconsistent casing
        public static readonly bool EnableZmq
            = Utils.GetConfigValue<bool>("enableZeroMQ", "false");
        // hard-coded
        public static readonly int NumWriters
            = 8;
        public static readonly int SleepBetweenPolls
            = 15 * 60000;
    }
}
