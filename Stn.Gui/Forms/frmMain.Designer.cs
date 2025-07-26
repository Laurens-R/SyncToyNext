namespace Stn.Gui
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            menuMain = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuOpenLocalLocation = new ToolStripMenuItem();
            menuFileSeperator = new ToolStripSeparator();
            menuFileManualMerge = new ToolStripMenuItem();
            menuRemote = new ToolStripMenuItem();
            menuChangeRemote = new ToolStripMenuItem();
            menuRemoteClone = new ToolStripMenuItem();
            menuLog = new ToolStripMenuItem();
            mainSplitContainer = new SplitContainer();
            txtLocalSyncPoint = new TextBox();
            toolStripLocal = new ToolStrip();
            btnPush = new ToolStripButton();
            lblLocalPath = new Label();
            fileBrowserLocal = new FileBrowserListView();
            toolStripRemote = new ToolStrip();
            btnRestore = new ToolStripButton();
            btnInfo = new ToolStripButton();
            comboSyncPoints = new ComboBox();
            lblRemotePath = new Label();
            fileBrowserRemote = new FileBrowserListView();
            contextMenuRemote = new ContextMenuStrip(components);
            menuContextCompareLocal = new ToolStripMenuItem();
            menuContextEditor = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            contextMenuRestoreItems = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            browserFolders = new FolderBrowserDialog();
            menuMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            toolStripLocal.SuspendLayout();
            toolStripRemote.SuspendLayout();
            contextMenuRemote.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuMain
            // 
            menuMain.Items.AddRange(new ToolStripItem[] { menuFile, menuRemote, menuLog });
            menuMain.Location = new Point(0, 0);
            menuMain.Name = "menuMain";
            menuMain.Size = new Size(1117, 24);
            menuMain.TabIndex = 0;
            menuMain.Text = "menuStrip1";
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuOpenLocalLocation, menuFileSeperator, menuFileManualMerge });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(37, 20);
            menuFile.Text = "&File";
            // 
            // menuOpenLocalLocation
            // 
            menuOpenLocalLocation.Name = "menuOpenLocalLocation";
            menuOpenLocalLocation.Size = new Size(177, 22);
            menuOpenLocalLocation.Text = "Open local location";
            menuOpenLocalLocation.Click += menuOpenLocalLocation_Click;
            // 
            // menuFileSeperator
            // 
            menuFileSeperator.Name = "menuFileSeperator";
            menuFileSeperator.Size = new Size(174, 6);
            // 
            // menuFileManualMerge
            // 
            menuFileManualMerge.Name = "menuFileManualMerge";
            menuFileManualMerge.Size = new Size(177, 22);
            menuFileManualMerge.Text = "Manual Merge";
            menuFileManualMerge.Click += menuFileManualMerge_Click;
            // 
            // menuRemote
            // 
            menuRemote.DropDownItems.AddRange(new ToolStripItem[] { menuChangeRemote, menuRemoteClone });
            menuRemote.Name = "menuRemote";
            menuRemote.Size = new Size(60, 20);
            menuRemote.Text = "&Remote";
            // 
            // menuChangeRemote
            // 
            menuChangeRemote.Name = "menuChangeRemote";
            menuChangeRemote.Size = new Size(115, 22);
            menuChangeRemote.Text = "Change";
            menuChangeRemote.Click += menuChangeRemote_Click;
            // 
            // menuRemoteClone
            // 
            menuRemoteClone.Name = "menuRemoteClone";
            menuRemoteClone.Size = new Size(115, 22);
            menuRemoteClone.Text = "Clone";
            menuRemoteClone.Click += menuRemoteClone_Click;
            // 
            // menuLog
            // 
            menuLog.Name = "menuLog";
            menuLog.Size = new Size(39, 20);
            menuLog.Text = "Log";
            menuLog.Click += menuLog_Click;
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
            mainSplitContainer.Panel1.Controls.Add(txtLocalSyncPoint);
            mainSplitContainer.Panel1.Controls.Add(toolStripLocal);
            mainSplitContainer.Panel1.Controls.Add(lblLocalPath);
            mainSplitContainer.Panel1.Controls.Add(fileBrowserLocal);
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(toolStripRemote);
            mainSplitContainer.Panel2.Controls.Add(comboSyncPoints);
            mainSplitContainer.Panel2.Controls.Add(lblRemotePath);
            mainSplitContainer.Panel2.Controls.Add(fileBrowserRemote);
            mainSplitContainer.Size = new Size(1117, 768);
            mainSplitContainer.SplitterDistance = 561;
            mainSplitContainer.TabIndex = 1;
            // 
            // txtLocalSyncPoint
            // 
            txtLocalSyncPoint.BackColor = Color.FromArgb(64, 64, 64);
            txtLocalSyncPoint.BorderStyle = BorderStyle.FixedSingle;
            txtLocalSyncPoint.Location = new Point(12, 64);
            txtLocalSyncPoint.Name = "txtLocalSyncPoint";
            txtLocalSyncPoint.ReadOnly = true;
            txtLocalSyncPoint.Size = new Size(546, 23);
            txtLocalSyncPoint.TabIndex = 4;
            // 
            // toolStripLocal
            // 
            toolStripLocal.ImageScalingSize = new Size(48, 48);
            toolStripLocal.Items.AddRange(new ToolStripItem[] { btnPush });
            toolStripLocal.Location = new Point(0, 0);
            toolStripLocal.Name = "toolStripLocal";
            toolStripLocal.Size = new Size(561, 31);
            toolStripLocal.TabIndex = 3;
            // 
            // btnPush
            // 
            btnPush.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnPush.Image = (Image)resources.GetObject("btnPush.Image");
            btnPush.ImageScaling = ToolStripItemImageScaling.None;
            btnPush.ImageTransparentColor = Color.Magenta;
            btnPush.Name = "btnPush";
            btnPush.Size = new Size(28, 28);
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
            fileBrowserLocal.Location = new Point(12, 93);
            fileBrowserLocal.Name = "fileBrowserLocal";
            fileBrowserLocal.Size = new Size(546, 650);
            fileBrowserLocal.TabIndex = 0;
            fileBrowserLocal.DoubleClickSelectedItem += fileBrowserLocal_DoubleClickSelectedItem;
            // 
            // toolStripRemote
            // 
            toolStripRemote.ImageScalingSize = new Size(28, 28);
            toolStripRemote.Items.AddRange(new ToolStripItem[] { btnRestore, btnInfo });
            toolStripRemote.Location = new Point(0, 0);
            toolStripRemote.Name = "toolStripRemote";
            toolStripRemote.Size = new Size(552, 35);
            toolStripRemote.TabIndex = 4;
            toolStripRemote.Text = "toolStrip1";
            // 
            // btnRestore
            // 
            btnRestore.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnRestore.Image = (Image)resources.GetObject("btnRestore.Image");
            btnRestore.ImageScaling = ToolStripItemImageScaling.None;
            btnRestore.ImageTransparentColor = Color.Magenta;
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(28, 32);
            btnRestore.Text = "Restore current syncpoint";
            btnRestore.TextAlign = ContentAlignment.MiddleRight;
            btnRestore.Click += btnRestore_Click;
            // 
            // btnInfo
            // 
            btnInfo.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btnInfo.Image = (Image)resources.GetObject("btnInfo.Image");
            btnInfo.ImageTransparentColor = Color.Magenta;
            btnInfo.Name = "btnInfo";
            btnInfo.Size = new Size(32, 32);
            btnInfo.Text = "Current Syncpoint Information";
            btnInfo.Click += btnInfo_Click;
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
            fileBrowserRemote.ContextMenuStrip = contextMenuRemote;
            fileBrowserRemote.Location = new Point(3, 93);
            fileBrowserRemote.Name = "fileBrowserRemote";
            fileBrowserRemote.Size = new Size(537, 650);
            fileBrowserRemote.TabIndex = 0;
            fileBrowserRemote.PathChanged += fileBrowserRemote_PathChanged;
            fileBrowserRemote.DoubleClickSelectedItem += fileBrowserRemote_DoubleClickSelectedItem;
            // 
            // contextMenuRemote
            // 
            contextMenuRemote.Items.AddRange(new ToolStripItem[] { menuContextCompareLocal, menuContextEditor, toolStripMenuItem1, contextMenuRestoreItems });
            contextMenuRemote.Name = "contextMenuRemote";
            contextMenuRemote.Size = new Size(178, 76);
            // 
            // menuContextCompareLocal
            // 
            menuContextCompareLocal.Name = "menuContextCompareLocal";
            menuContextCompareLocal.Size = new Size(177, 22);
            menuContextCompareLocal.Text = "Compare with local";
            menuContextCompareLocal.Click += menuContextCompareLocal_Click;
            // 
            // menuContextEditor
            // 
            menuContextEditor.Name = "menuContextEditor";
            menuContextEditor.Size = new Size(177, 22);
            menuContextEditor.Text = "Open in Editor";
            menuContextEditor.Click += menuContextEditor_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(174, 6);
            // 
            // contextMenuRestoreItems
            // 
            contextMenuRestoreItems.Name = "contextMenuRestoreItems";
            contextMenuRestoreItems.Size = new Size(177, 22);
            contextMenuRestoreItems.Text = "Restore item(s)";
            contextMenuRestoreItems.Click += contextMenuRestoreItems_Click;
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
            lblStatus.Size = new Size(60, 17);
            lblStatus.Text = "Status: Ok";
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1117, 792);
            Controls.Add(statusStrip);
            Controls.Add(mainSplitContainer);
            Controls.Add(menuMain);
            Icon = (Icon)resources.GetObject("$this.Icon");
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
            toolStripLocal.ResumeLayout(false);
            toolStripLocal.PerformLayout();
            toolStripRemote.ResumeLayout(false);
            toolStripRemote.PerformLayout();
            contextMenuRemote.ResumeLayout(false);
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
        private ToolStrip toolStripLocal;
        private ToolStrip toolStripRemote;
        private ToolStripButton btnPush;
        private ToolStripButton btnRestore;
        private ToolStripMenuItem menuRemote;
        private ToolStripMenuItem menuChangeRemote;
        private ToolStripMenuItem menuLog;
        private ContextMenuStrip contextMenuRemote;
        private ToolStripMenuItem contextMenuRestoreItems;
        private ToolStripMenuItem menuContextEditor;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem menuContextCompareLocal;
        private ToolStripSeparator menuFileSeperator;
        private ToolStripMenuItem menuFileManualMerge;
        private ToolStripMenuItem menuRemoteClone;
        private TextBox txtLocalSyncPoint;
        private ToolStripButton btnInfo;
    }
}
