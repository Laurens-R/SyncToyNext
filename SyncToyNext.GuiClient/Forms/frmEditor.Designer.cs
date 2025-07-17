namespace SyncToyNext.GuiClient.Forms
{
    partial class frmEditor
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
            webViewEditor = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)webViewEditor).BeginInit();
            SuspendLayout();
            // 
            // webViewEditor
            // 
            webViewEditor.AllowExternalDrop = true;
            webViewEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webViewEditor.CreationProperties = null;
            webViewEditor.DefaultBackgroundColor = Color.White;
            webViewEditor.Location = new Point(0, 2);
            webViewEditor.Name = "webViewEditor";
            webViewEditor.Size = new Size(800, 443);
            webViewEditor.TabIndex = 0;
            webViewEditor.ZoomFactor = 1D;
            webViewEditor.WebMessageReceived += webViewEditor_WebMessageReceived;
            // 
            // frmEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(webViewEditor);
            Name = "frmEditor";
            Text = "frmEditor";
            Load += frmEditor_Load;
            ((System.ComponentModel.ISupportInitialize)webViewEditor).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webViewEditor;
    }
}