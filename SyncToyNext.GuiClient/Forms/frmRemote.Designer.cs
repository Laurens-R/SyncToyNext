namespace SyncToyNext.GuiClient.Forms
{
    partial class frmRemote
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
            txtRemotePath = new TextBox();
            btnBrowseRemote = new Button();
            browserRemote = new FolderBrowserDialog();
            lblRemotePath = new Label();
            checkBoxCompression = new CheckBox();
            lblCompression = new Label();
            btnOK = new Button();
            SuspendLayout();
            // 
            // txtRemotePath
            // 
            txtRemotePath.BackColor = Color.FromArgb(64, 64, 64);
            txtRemotePath.ForeColor = SystemColors.WindowText;
            txtRemotePath.Location = new Point(244, 31);
            txtRemotePath.Name = "txtRemotePath";
            txtRemotePath.ReadOnly = true;
            txtRemotePath.Size = new Size(524, 23);
            txtRemotePath.TabIndex = 0;
            // 
            // btnBrowseRemote
            // 
            btnBrowseRemote.Location = new Point(163, 30);
            btnBrowseRemote.Name = "btnBrowseRemote";
            btnBrowseRemote.Size = new Size(75, 23);
            btnBrowseRemote.TabIndex = 1;
            btnBrowseRemote.Text = "Browse";
            btnBrowseRemote.UseVisualStyleBackColor = true;
            btnBrowseRemote.Click += btnBrowseRemote_Click;
            // 
            // lblRemotePath
            // 
            lblRemotePath.AutoSize = true;
            lblRemotePath.Location = new Point(21, 34);
            lblRemotePath.Name = "lblRemotePath";
            lblRemotePath.Size = new Size(78, 15);
            lblRemotePath.TabIndex = 2;
            lblRemotePath.Text = "Remote Path:";
            // 
            // checkBoxCompression
            // 
            checkBoxCompression.AutoSize = true;
            checkBoxCompression.Location = new Point(163, 59);
            checkBoxCompression.Name = "checkBoxCompression";
            checkBoxCompression.Size = new Size(15, 14);
            checkBoxCompression.TabIndex = 3;
            checkBoxCompression.UseVisualStyleBackColor = true;
            // 
            // lblCompression
            // 
            lblCompression.AutoSize = true;
            lblCompression.Location = new Point(21, 58);
            lblCompression.Name = "lblCompression";
            lblCompression.Size = new Size(99, 15);
            lblCompression.TabIndex = 4;
            lblCompression.Text = "Use Compression";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(616, 139);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(152, 43);
            btnOK.TabIndex = 5;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // frmRemote
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(797, 203);
            Controls.Add(btnOK);
            Controls.Add(lblCompression);
            Controls.Add(checkBoxCompression);
            Controls.Add(lblRemotePath);
            Controls.Add(btnBrowseRemote);
            Controls.Add(txtRemotePath);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmRemote";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Remote Configuration";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtRemotePath;
        private Button btnBrowseRemote;
        private FolderBrowserDialog browserRemote;
        private Label lblRemotePath;
        private CheckBox checkBoxCompression;
        private Label lblCompression;
        private Button btnOK;
    }
}