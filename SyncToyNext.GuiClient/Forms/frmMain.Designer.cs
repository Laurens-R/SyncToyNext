namespace SyncToyNext.GuiClient
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            menuMain = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuOpenLocalLocation = new ToolStripMenuItem();
            mainSplitContainer = new SplitContainer();
            toolStrip2 = new ToolStrip();
            btnPush = new ToolStripButton();
            lblLocalPath = new Label();
            fileBrowserLocal = new FileBrowserListView();
            toolStrip1 = new ToolStrip();
            btnRestore = new ToolStripButton();
            comboSyncPoints = new ComboBox();
            lblRemotePath = new Label();
            fileBrowserRemote = new FileBrowserListView();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            browserFolders = new FolderBrowserDialog();
            menuRemote = new ToolStripMenuItem();
            menuChangeRemote = new ToolStripMenuItem();
            menuMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            toolStrip2.SuspendLayout();
            toolStrip1.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuMain
            // 
            menuMain.Items.AddRange(new ToolStripItem[] { menuFile, menuRemote });
            menuMain.Location = new Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Size = new Size(1117, 24);
            menuMain.TabIndex = 0;
            menuMain.Text = "menuStrip1";
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuOpenLocalLocation });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(37, 20);
            menuFile.Text = "&File";
            // 
            // menuOpenLocalLocation
            // 
            menuOpenLocalLocation.Name = "menuOpenLocalLocation";
            menuOpenLocalLocation.Size = new Size(180, 22);
            menuOpenLocalLocation.Text = "Open local location";
            menuOpenLocalLocation.Click += menuOpenLocalLocation_Click;
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.FixedPanel = FixedPanel.Panel1;
            mainSplitContainer.Location = new Point(0, 24);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(toolStrip2);
            mainSplitContainer.Panel1.Controls.Add(lblLocalPath);
            mainSplitContainer.Panel1.Controls.Add(fileBrowserLocal);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(toolStrip1);
            mainSplitContainer.Panel2.Controls.Add(comboSyncPoints);
            mainSplitContainer.Panel2.Controls.Add(lblRemotePath);
            mainSplitContainer.Panel2.Controls.Add(fileBrowserRemote);
            mainSplitContainer.Size = new Size(1117, 768);
            mainSplitContainer.SplitterDistance = 561;
            mainSplitContainer.TabIndex = 1;
            // 
            // toolStrip2
            // 
            toolStrip2.Items.AddRange(new ToolStripItem[] { btnPush });
            toolStrip2.Location = new Point(0, 0);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new Size(561, 25);
            toolStrip2.TabIndex = 3;
            toolStrip2.Text = "toolStrip2";
            // 
            // btnPush
            // 
            btnPush.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnPush.Image = (Image)resources.GetObject("btnPush.Image");
            btnPush.ImageTransparentColor = Color.Magenta;
            btnPush.Name = "btnPush";
            btnPush.Size = new Size(23, 22);
            btnPush.Text = "Push Changes";
            btnPush.Click += btnPush_Click;
            // 
            // lblLocalPath
            // 
            lblLocalPath.AutoSize = true;
            lblLocalPath.Location = new Point(12, 36);
            lblLocalPath.Name = "lblLocalPath";
            lblLocalPath.Size = new Size(73, 15);
            lblLocalPath.TabIndex = 2;
            lblLocalPath.Text = "Local path: -";
            // 
            // fileBrowserLocal
            // 
            fileBrowserLocal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fileBrowserLocal.Location = new Point(12, 64);
            fileBrowserLocal.Name = "fileBrowserLocal";
            fileBrowserLocal.Size = new Size(546, 679);
            fileBrowserLocal.TabIndex = 0;
            fileBrowserLocal.DoubleClickSelectedItem += fileBrowserLocal_DoubleClickSelectedItem;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnRestore });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(552, 25);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnRestore
            // 
            btnRestore.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnRestore.Image = (Image)resources.GetObject("btnRestore.Image");
            btnRestore.ImageTransparentColor = Color.Magenta;
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(23, 22);
            btnRestore.Text = "Restore current syncpoint";
            btnRestore.TextAlign = ContentAlignment.MiddleRight;
            // 
            // comboSyncPoints
            // 
            comboSyncPoints.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboSyncPoints.FormattingEnabled = true;
            comboSyncPoints.Location = new Point(3, 64);
            comboSyncPoints.Name = "comboSyncPoints";
            comboSyncPoints.Size = new Size(537, 23);
            comboSyncPoints.TabIndex = 3;
            comboSyncPoints.SelectedIndexChanged += comboSyncPoints_SelectedIndexChanged;
            // 
            // lblRemotePath
            // 
            lblRemotePath.AutoSize = true;
            lblRemotePath.Location = new Point(3, 36);
            lblRemotePath.Name = "lblRemotePath";
            lblRemotePath.Size = new Size(86, 15);
            lblRemotePath.TabIndex = 2;
            lblRemotePath.Text = "Remote path: -";
            // 
            // fileBrowserRemote
            // 
            fileBrowserRemote.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fileBrowserRemote.Location = new Point(3, 93);
            fileBrowserRemote.Name = "fileBrowserRemote";
            fileBrowserRemote.Size = new Size(537, 650);
            fileBrowserRemote.TabIndex = 0;
            fileBrowserRemote.DoubleClickSelectedItem += fileBrowserRemote_DoubleClickSelectedItem;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 770);
            statusStrip.Name = "statusStrip";
            statusStrip.RenderMode = ToolStripRenderMode.Professional;
            statusStrip.Size = new Size(1117, 22);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(118, 17);
            lblStatus.Text = "toolStripStatusLabel1";
            // 
            // menuRemote
            // 
            menuRemote.DropDownItems.AddRange(new ToolStripItem[] { menuChangeRemote });
            menuRemote.Name = "menuRemote";
            menuRemote.Size = new Size(60, 20);
            menuRemote.Text = "&Remote";
            // 
            // menuChangeRemote
            // 
            menuChangeRemote.Name = "menuChangeRemote";
            menuChangeRemote.Size = new Size(180, 22);
            menuChangeRemote.Text = "Change";
            menuChangeRemote.Click += this.menuChangeRemote_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1117, 792);
            Controls.Add(statusStrip);
            Controls.Add(mainSplitContainer);
            Controls.Add(menuMain);
            MainMenuStrip = menuMain;
            Margin = new Padding(3, 2, 3, 2);
            Name = "frmMain";
            Text = "Sync Toy Next ";
            menuMain.ResumeLayout(false);
            menuMain.PerformLayout();
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel1.PerformLayout();
            mainSplitContainer.Panel2.ResumeLayout(false);
            mainSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuMain;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuOpenLocalLocation;
        private SplitContainer mainSplitContainer;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private FolderBrowserDialog browserFolders;
        private FileBrowserListView fileBrowserLocal;
        private FileBrowserListView fileBrowserRemote;
        private Label lblLocalPath;
        private Label lblRemotePath;
        private ComboBox comboSyncPoints;
        private ToolStrip toolStrip2;
        private ToolStrip toolStrip1;
        private ToolStripButton btnPush;
        private ToolStripButton btnRestore;
        private ToolStripMenuItem menuRemote;
        private ToolStripMenuItem menuChangeRemote;
    }
}
