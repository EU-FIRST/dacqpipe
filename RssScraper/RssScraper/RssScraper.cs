using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Latino;
using Latino.Web;

namespace RssScraper
{
    public partial class RssScraperForm : Form
    {
        public RssScraperForm()
        {
            InitializeComponent();
        }

        private string[] mExcludeList = new string[] { 
                "fusion.google.com",
                "add.my.yahoo.com",
                "my.yahoo.com/add", 
                "www.bloglines.com",
                "www.newsgator.com",
                "www.netvibes.com",
                "www.google.com/ig/add",
                "my.msn.com/addtomymsn",
                "www.google.com/reader",
                "www.live.com",
                "www.addthis.com"
            };

        private string[] mRegexList = new string[] { 
                @"href=[""'](?<rssUrl>[^""']*\.rss)[""']",
                @"href=[""'](?<rssUrl>[^""']*\.xml)[""']",
                @"href=[""'](?<rssUrl>[^""']*\.rdf)[""']",
                @"href=[""'](?<rssUrl>[^""']*rss[^""']*)[""']",
                @"href=[""'](?<rssUrl>[^""']*feed[^""']*)[""']"
            };

        private bool TestRssXml(string rssXml)
        {
            return rssXml.Contains("<item") || rssXml.Contains("<channel");
        }

        private bool TestAtomXml(string atomXml)
        {
            return atomXml.Contains("<entry") || atomXml.Contains("<feed");
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
                            string message = "RSS feed NOT detected.";
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
                                    try { xml = WebUtils.GetWebPageDetectEncoding(url); } catch { }
                                    bool rssXmlFound = xml != null && TestRssXml(xml);
                                    if (rssXmlFound) { message = "RSS feed detected."; }
                                    // convert Atom to RSS
                                    if (xml != null && miConvertAtomToRss.Checked && !rssXmlFound && TestAtomXml(xml))
                                    {
                                        url = "http://www.devtacular.com/utilities/atomtorss/?url=" + HttpUtility.HtmlEncode(url);
                                        xml = null;
                                        try { xml = WebUtils.GetWebPageDetectEncoding(url); }
                                        catch { }
                                        rssXmlFound = xml != null && TestRssXml(xml);
                                        if (rssXmlFound) { message = "RSS feed detected after converting from Atom."; }
                                    }
                                    else // try the format=xml trick
                                    {
                                        if (miFeedburnerFormat.Checked && !rssXmlFound)
                                        {
                                            string newUrl = url + (url.Contains("?") ? "&" : "?") + "format=xml";
                                            try { xml = WebUtils.GetWebPageDetectEncoding(newUrl); } catch { }
                                            rssXmlFound = xml != null && TestRssXml(xml);
                                            if (rssXmlFound) 
                                            {
                                                message = "RSS feed detected after applying the format=xml trick.";
                                                url = newUrl; 
                                            }
                                        }
                                    }
                                    if (miRemoveNonRss.Checked && !rssXmlFound) { Invoke(new ThreadStart(delegate() { txtLinks.Text += "#"; removed = true; })); } 
                                    Invoke(new ThreadStart(delegate() { txtLinks.Text += url + "\r\n"; }));    
                                    if (miOutputTestResult.Checked)
                                    {
                                        Invoke(new ThreadStart(delegate() { txtLinks.Text += "# " + message + "\r\n"; }));                                           
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