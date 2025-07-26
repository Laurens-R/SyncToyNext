namespace Stn.Gui
{
    partial class FileBrowserListView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileBrowserListView));
            listEntries = new ListView();
            columnItem = new ColumnHeader();
            columnType = new ColumnHeader();
            listViewIcons = new ImageList(components);
            SuspendLayout();
            // 
            // listEntries
            // 
            listEntries.Columns.AddRange(new ColumnHeader[] { columnItem, columnType });
            listEntries.Dock = DockStyle.Fill;
            listEntries.FullRowSelect = true;
            listEntries.LargeImageList = listViewIcons;
            listEntries.Location = new Point(0, 0);
            listEntries.Name = "listEntries";
            listEntries.Size = new Size(700, 474);
            listEntries.SmallImageList = listViewIcons;
            listEntries.TabIndex = 0;
            listEntries.UseCompatibleStateImageBehavior = false;
            listEntries.View = View.Details;
            listEntries.DoubleClick += listEntries_DoubleClick;
            // 
            // columnItem
            // 
            columnItem.Text = "Item";
            columnItem.Width = 500;
            // 
            // columnType
            // 
            columnType.Text = "Type";
            columnType.Width = 120;
            // 
            // listViewIcons
            // 
            listViewIcons.ColorDepth = ColorDepth.Depth32Bit;
            listViewIcons.ImageStream = (ImageListStreamer)resources.GetObject("listViewIcons.ImageStream");
            listViewIcons.TransparentColor = Color.Transparent;
            listViewIcons.Images.SetKeyName(0, "fileBlack");
            listViewIcons.Images.SetKeyName(1, "fileWhite");
            listViewIcons.Images.SetKeyName(2, "folderBlack");
            listViewIcons.Images.SetKeyName(3, "folderWhite");
            // 
            // FileBrowserListView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(listEntries);
            Name = "FileBrowserListView";
            Size = new Size(700, 474);
            ResumeLayout(false);
        }

        #endregion

        private ListView listEntries;
        private ColumnHeader columnItem;
        private ImageList listViewIcons;
        private ColumnHeader columnType;
    }
}
