namespace RssScraper
{
    partial class frmRssScraper
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmRssScraper));
            this.btnGo = new System.Windows.Forms.Button();
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.txtLinks = new System.Windows.Forms.TextBox();
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.miSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude1 = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude2 = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude3 = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude4 = new System.Windows.Forms.ToolStripMenuItem();
            this.miInclude5 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude1 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude2 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude3 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude4 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude5 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude6 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude7 = new System.Windows.Forms.ToolStripMenuItem();
            this.miExclude8 = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.miTestLinks = new System.Windows.Forms.ToolStripMenuItem();
            this.miOutputTestResult = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.miRemoveNonRss = new System.Windows.Forms.ToolStripMenuItem();
            this.miFeedburnerFormat = new System.Windows.Forms.ToolStripMenuItem();
            this.miSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();
            this.MainMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnGo
            // 
            this.btnGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGo.Location = new System.Drawing.Point(725, 28);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(75, 23);
            this.btnGo.TabIndex = 0;
            this.btnGo.Text = ">>";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // txtUrl
            // 
            this.txtUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUrl.Location = new System.Drawing.Point(12, 30);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(707, 20);
            this.txtUrl.TabIndex = 1;
            this.txtUrl.Text = "http://edition.cnn.com/services/rss/";
            // 
            // txtLinks
            // 
            this.txtLinks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLinks.BackColor = System.Drawing.SystemColors.Window;
            this.txtLinks.Location = new System.Drawing.Point(12, 59);
            this.txtLinks.Multiline = true;
            this.txtLinks.Name = "txtLinks";
            this.txtLinks.ReadOnly = true;
            this.txtLinks.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLinks.Size = new System.Drawing.Size(788, 403);
            this.txtLinks.TabIndex = 2;
            this.txtLinks.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtLinks_KeyDown);
            // 
            // MainMenu
            // 
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miSettings});
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(812, 27);
            this.MainMenu.TabIndex = 6;
            this.MainMenu.Text = "menuStrip1";
            // 
            // miSettings
            // 
            this.miSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miInclude,
            this.miExclude,
            this.miSep1,
            this.miTestLinks,
            this.miSep2,
            this.miExit});
            this.miSettings.Name = "miSettings";
            this.miSettings.Size = new System.Drawing.Size(70, 23);
            this.miSettings.Text = "Settings";
            // 
            // miInclude
            // 
            this.miInclude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miInclude1,
            this.miInclude2,
            this.miInclude3,
            this.miInclude4,
            this.miInclude5});
            this.miInclude.Name = "miInclude";
            this.miInclude.Size = new System.Drawing.Size(152, 24);
            this.miInclude.Text = "Include";
            // 
            // miInclude1
            // 
            this.miInclude1.Checked = true;
            this.miInclude1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miInclude1.Name = "miInclude1";
            this.miInclude1.Size = new System.Drawing.Size(164, 24);
            this.miInclude1.Text = "Include *.rss";
            this.miInclude1.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miInclude2
            // 
            this.miInclude2.Checked = true;
            this.miInclude2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miInclude2.Name = "miInclude2";
            this.miInclude2.Size = new System.Drawing.Size(164, 24);
            this.miInclude2.Text = "Include *.xml";
            this.miInclude2.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miInclude3
            // 
            this.miInclude3.Checked = true;
            this.miInclude3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miInclude3.Name = "miInclude3";
            this.miInclude3.Size = new System.Drawing.Size(164, 24);
            this.miInclude3.Text = "Include *.rdf";
            this.miInclude3.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miInclude4
            // 
            this.miInclude4.Checked = true;
            this.miInclude4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miInclude4.Name = "miInclude4";
            this.miInclude4.Size = new System.Drawing.Size(164, 24);
            this.miInclude4.Text = "Include *rss*";
            this.miInclude4.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miInclude5
            // 
            this.miInclude5.Checked = true;
            this.miInclude5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miInclude5.Name = "miInclude5";
            this.miInclude5.Size = new System.Drawing.Size(164, 24);
            this.miInclude5.Text = "Include *feed*";
            this.miInclude5.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miExclude
            // 
            this.miExclude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miExclude1,
            this.miExclude2,
            this.miExclude3,
            this.miExclude4,
            this.miExclude5,
            this.miExclude6,
            this.miExclude7,
            this.miExclude8});
            this.miExclude.Name = "miExclude";
            this.miExclude.Size = new System.Drawing.Size(152, 24);
            this.miExclude.Text = "Exclude";
            // 
            // miExclude1
            // 
            this.miExclude1.Checked = true;
            this.miExclude1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude1.Name = "miExclude1";
            this.miExclude1.Size = new System.Drawing.Size(290, 24);
            this.miExclude1.Text = "Exclude fusion.google.com";
            this.miExclude1.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miExclude2
            // 
            this.miExclude2.Checked = true;
            this.miExclude2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude2.Name = "miExclude2";
            this.miExclude2.Size = new System.Drawing.Size(290, 24);
            this.miExclude2.Text = "Exclude add.my.yahoo.com";
            this.miExclude2.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miExclude3
            // 
            this.miExclude3.Checked = true;
            this.miExclude3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude3.Name = "miExclude3";
            this.miExclude3.Size = new System.Drawing.Size(290, 24);
            this.miExclude3.Text = "Exclude www.bloglines.com";
            this.miExclude3.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miExclude4
            // 
            this.miExclude4.Checked = true;
            this.miExclude4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude4.Name = "miExclude4";
            this.miExclude4.Size = new System.Drawing.Size(290, 24);
            this.miExclude4.Text = "Exclude www.newsgator.com";
            // 
            // miExclude5
            // 
            this.miExclude5.Checked = true;
            this.miExclude5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude5.Name = "miExclude5";
            this.miExclude5.Size = new System.Drawing.Size(290, 24);
            this.miExclude5.Text = "Exclude www.netvibes.com";
            // 
            // miExclude6
            // 
            this.miExclude6.Checked = true;
            this.miExclude6.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude6.Name = "miExclude6";
            this.miExclude6.Size = new System.Drawing.Size(290, 24);
            this.miExclude6.Text = "Exclude www.google.com/ig/add";
            // 
            // miExclude7
            // 
            this.miExclude7.Checked = true;
            this.miExclude7.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude7.Name = "miExclude7";
            this.miExclude7.Size = new System.Drawing.Size(290, 24);
            this.miExclude7.Text = "Exclude my.msn.com/addtomymsn";
            // 
            // miExclude8
            // 
            this.miExclude8.Checked = true;
            this.miExclude8.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miExclude8.Name = "miExclude8";
            this.miExclude8.Size = new System.Drawing.Size(290, 24);
            this.miExclude8.Text = "Exclude www.google.com/reader";
            // 
            // miSep1
            // 
            this.miSep1.Name = "miSep1";
            this.miSep1.Size = new System.Drawing.Size(149, 6);
            // 
            // miTestLinks
            // 
            this.miTestLinks.Checked = true;
            this.miTestLinks.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miTestLinks.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miOutputTestResult,
            this.miSep3,
            this.miRemoveNonRss,
            this.miFeedburnerFormat});
            this.miTestLinks.Name = "miTestLinks";
            this.miTestLinks.Size = new System.Drawing.Size(152, 24);
            this.miTestLinks.Text = "Test Links";
            this.miTestLinks.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miOutputTestResult
            // 
            this.miOutputTestResult.Name = "miOutputTestResult";
            this.miOutputTestResult.Size = new System.Drawing.Size(221, 24);
            this.miOutputTestResult.Text = "Output Test Result";
            this.miOutputTestResult.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miSep3
            // 
            this.miSep3.Name = "miSep3";
            this.miSep3.Size = new System.Drawing.Size(218, 6);
            // 
            // miRemoveNonRss
            // 
            this.miRemoveNonRss.Checked = true;
            this.miRemoveNonRss.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miRemoveNonRss.Name = "miRemoveNonRss";
            this.miRemoveNonRss.Size = new System.Drawing.Size(221, 24);
            this.miRemoveNonRss.Text = "Remove Non-RSS";
            this.miRemoveNonRss.Click += new System.EventHandler(this.MenuItem_Click);
            // 
            // miFeedburnerFormat
            // 
            this.miFeedburnerFormat.Checked = true;
            this.miFeedburnerFormat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.miFeedburnerFormat.Name = "miFeedburnerFormat";
            this.miFeedburnerFormat.Size = new System.Drawing.Size(221, 24);
            this.miFeedburnerFormat.Text = "Second Try format=xml";
            // 
            // miSep2
            // 
            this.miSep2.Name = "miSep2";
            this.miSep2.Size = new System.Drawing.Size(149, 6);
            // 
            // miExit
            // 
            this.miExit.Name = "miExit";
            this.miExit.Size = new System.Drawing.Size(152, 24);
            this.miExit.Text = "Exit";
            this.miExit.Click += new System.EventHandler(this.miExit_Click);
            // 
            // frmRssScraper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(812, 474);
            this.Controls.Add(this.txtLinks);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.MainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MainMenu;
            this.Name = "frmRssScraper";
            this.Text = "RSS Scraper";
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.TextBox txtLinks;
        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem miSettings;
        private System.Windows.Forms.ToolStripMenuItem miTestLinks;
        private System.Windows.Forms.ToolStripMenuItem miInclude;
        private System.Windows.Forms.ToolStripMenuItem miExclude;
        private System.Windows.Forms.ToolStripMenuItem miInclude1;
        private System.Windows.Forms.ToolStripMenuItem miInclude2;
        private System.Windows.Forms.ToolStripMenuItem miInclude3;
        private System.Windows.Forms.ToolStripMenuItem miInclude4;
        private System.Windows.Forms.ToolStripMenuItem miInclude5;
        private System.Windows.Forms.ToolStripMenuItem miExclude1;
        private System.Windows.Forms.ToolStripMenuItem miExclude2;
        private System.Windows.Forms.ToolStripMenuItem miExclude3;
        private System.Windows.Forms.ToolStripMenuItem miOutputTestResult;
        private System.Windows.Forms.ToolStripMenuItem miRemoveNonRss;
        private System.Windows.Forms.ToolStripSeparator miSep1;
        private System.Windows.Forms.ToolStripSeparator miSep2;
        private System.Windows.Forms.ToolStripSeparator miSep3;
        private System.Windows.Forms.ToolStripMenuItem miExclude4;
        private System.Windows.Forms.ToolStripMenuItem miExclude5;
        private System.Windows.Forms.ToolStripMenuItem miExit;
        private System.Windows.Forms.ToolStripMenuItem miExclude6;
        private System.Windows.Forms.ToolStripMenuItem miExclude7;
        private System.Windows.Forms.ToolStripMenuItem miExclude8;
        private System.Windows.Forms.ToolStripMenuItem miFeedburnerFormat;
    }
}

