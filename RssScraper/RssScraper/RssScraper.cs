using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using Latino;
using Latino.Web;

namespace RssScraper
{
    public partial class frmRssScraper : Form
    {
        public frmRssScraper()
        {
            InitializeComponent();
        }

        private string[] mExcludeList = new string[] { 
                "fusion.google.com",
                "add.my.yahoo.com",
                "www.bloglines.com",
                "www.newsgator.com",
                "www.netvibes.com",
                "www.google.com/ig/add",
                "my.msn.com/addtomymsn",
                "www.google.com/reader",
                "www.live.com"
            };

        private string[] mRegexList = new string[] { 
                @"href=[""'](?<rssUrl>[^""']*\.rss)[""']",
                @"href=[""'](?<rssUrl>[^""']*\.xml)[""']",
                @"href=[""'](?<rssUrl>[^""']*\.rdf)[""']",
                @"href=[""'](?<rssUrl>[^""']*rss[^""']*)[""']",
                @"href=[""'](?<rssUrl>[^""']*feed[^""']*)[""']"
            };

        // crude way to test RSS XML
        private bool TestRssXml(string rssXml, out bool itemsFound)
        {
            itemsFound = rssXml.Contains("<item");
            return rssXml.Contains("<channel");
        }

        private void TryInvoke(ThreadStart method)
        {
            try { Invoke(method); } catch { }
        }

        private void btnGo_Click(object sender, EventArgs args)
        {
            btnGo.Enabled = false;
            miSettings.Enabled = false;
            txtUrl.Enabled = false;
            txtLinks.Clear();
            new Thread(new ThreadStart(delegate() 
            {
                try
                {
                    bool first = true;
                    ArrayList<string> includeList = new ArrayList<string>();
                    int i = 0;
                    foreach (ToolStripMenuItem item in miInclude.DropDownItems)
                    {
                        if (item.Checked) { includeList.Add(mRegexList[i]); }
                        i++;
                    }
                    ArrayList<string> excludeList = new ArrayList<string>();
                    i = 0;
                    foreach (ToolStripMenuItem item in miExclude.DropDownItems)
                    {
                        if (item.Checked) { excludeList.Add(mExcludeList[i]); }
                        i++;
                    }
                    Set<string> links = new Set<string>();
                    Uri baseUrl = new Uri(txtUrl.Text);
                    string html = WebUtils.GetWebPageDetectEncoding(txtUrl.Text);
                    foreach (string regex in includeList)
                    {
                        Regex r = new Regex(regex, RegexOptions.IgnoreCase);
                        Match m = r.Match(html);
                        while (m.Success)
                        {
                            string url = m.Result("${rssUrl}").Trim();
                            url = new Uri(baseUrl, url).ToString();
                            string urlLower = url.ToLower();
                            // test whether to include link
                            bool ok = true;
                            foreach (string substr in excludeList)
                            {
                                if (urlLower.Contains(substr)) { ok = false; break; }
                            }
                            if (ok && !links.Contains(urlLower))
                            {
                                // test RSS file
                                bool removed = false;
                                if (miTestLinks.Checked)
                                {
                                    string xml = null;
                                    try
                                    {
                                        xml = WebUtils.GetWebPageDetectEncoding(url);
                                    }
                                    catch
                                    {
                                    }
                                    bool itemsFound = false;
                                    bool channelFound = xml != null && TestRssXml(xml, out itemsFound);
                                    if (miFeedburnerFormat.Checked && !channelFound && !itemsFound)
                                    {
                                        string newUrl = url + (url.Contains("?") ? "&" : "?") + "format=xml";
                                        try
                                        {
                                            xml = WebUtils.GetWebPageDetectEncoding(newUrl);
                                        }
                                        catch
                                        {
                                        }
                                        itemsFound = false;
                                        channelFound = xml != null && TestRssXml(xml, out itemsFound);
                                        if (channelFound || itemsFound) { url = newUrl; }
                                    }
                                    if (miRemoveNonRss.Checked && !channelFound && !itemsFound) { Invoke(new ThreadStart(delegate() { txtLinks.Text += "#"; removed = true; })); } 
                                    Invoke(new ThreadStart(delegate() { txtLinks.Text += url + "\r\n"; }));    
                                    if (miOutputTestResult.Checked)
                                    {
                                        Invoke(new ThreadStart(delegate() { txtLinks.Text += "# "; })); 
                                        if (!channelFound && !itemsFound)
                                        {
                                            Invoke(new ThreadStart(delegate() { txtLinks.Text += "This is most probably not an RSS feed. "; })); // *** is it ATOM?
                                        }
                                        else
                                        {
                                            if (channelFound) { Invoke(new ThreadStart(delegate() { txtLinks.Text += "Channel found. "; })); }
                                            if (itemsFound) { Invoke(new ThreadStart(delegate() { txtLinks.Text += "Items found. "; })); } 
                                        }
                                        Invoke(new ThreadStart(delegate() { txtLinks.Text += "\r\n"; })); 
                                    }
                                }
                                else
                                {
                                    Invoke(new ThreadStart(delegate() { txtLinks.Text += url + "\r\n"; })); 
                                }
                                if (!removed) { first = false; }
                                links.Add(urlLower);
                            }
                            m = m.NextMatch();
                        }
                    }
                }
                catch (Exception e)
                {
                    TryInvoke(new ThreadStart(delegate() { txtLinks.Text += e.Message + "\r\n" + e.StackTrace; })); 
                }
                finally
                {
                    TryInvoke(new ThreadStart(delegate() { btnGo.Enabled = txtUrl.Enabled = miSettings.Enabled = true; }));
                }
            })).Start();
        }   

        private void MenuItem_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)sender).Checked = !((ToolStripMenuItem)sender).Checked;
        }

        private void txtLinks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                txtLinks.SelectAll();
            }
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}