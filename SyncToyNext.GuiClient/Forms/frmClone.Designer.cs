namespace SyncToyNext.GuiClient.Forms
{
    partial class frmClone
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
            lblRemotePath = new Label();
            browseDialog = new FolderBrowserDialog();
            btnOK = new Button();
            grpNew = new GroupBox();
            label1 = new Label();
            checkboxCompressed = new CheckBox();
            lblNewRemote = new Label();
            btnBrowseNewRemote = new Button();
            txtNewRemote = new TextBox();
            lblNewLocal = new Label();
            btnBrowseNewLocal = new Button();
            txtNewLocal = new TextBox();
            groupFrom = new GroupBox();
            btnBrowseRemote = new Button();
            txtRemotePath = new TextBox();
            grpNew.SuspendLayout();
            groupFrom.SuspendLayout();
            SuspendLayout();
            // 
            // lblRemotePath
            // 
            lblRemotePath.AutoSize = true;
            lblRemotePath.Location = new Point(17, 29);
            lblRemotePath.Name = "lblRemotePath";
            lblRemotePath.Size = new Size(78, 15);
            lblRemotePath.TabIndex = 5;
            lblRemotePath.Text = "Remote path:";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(628, 247);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(152, 43);
            btnOK.TabIndex = 12;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // grpNew
            // 
            grpNew.Controls.Add(label1);
            grpNew.Controls.Add(checkboxCompressed);
            grpNew.Controls.Add(lblNewRemote);
            grpNew.Controls.Add(btnBrowseNewRemote);
            grpNew.Controls.Add(txtNewRemote);
            grpNew.Controls.Add(lblNewLocal);
            grpNew.Controls.Add(btnBrowseNewLocal);
            grpNew.Controls.Add(txtNewLocal);
            grpNew.Location = new Point(19, 101);
            grpNew.Name = "grpNew";
            grpNew.Size = new Size(761, 128);
            grpNew.TabIndex = 13;
            grpNew.TabStop = false;
            grpNew.Text = "New repository information";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 95);
            label1.Name = "label1";
            label1.Size = new Size(97, 15);
            label1.TabIndex = 19;
            label1.Text = "Use compression";
            // 
            // checkboxCompressed
            // 
            checkboxCompressed.AutoSize = true;
            checkboxCompressed.Location = new Point(240, 96);
            checkboxCompressed.Name = "checkboxCompressed";
            checkboxCompressed.Size = new Size(15, 14);
            checkboxCompressed.TabIndex = 18;
            checkboxCompressed.UseVisualStyleBackColor = true;
            // 
            // lblNewRemote
            // 
            lblNewRemote.AutoSize = true;
            lblNewRemote.Location = new Point(17, 66);
            lblNewRemote.Name = "lblNewRemote";
            lblNewRemote.Size = new Size(102, 15);
            lblNewRemote.TabIndex = 17;
            lblNewRemote.Text = "New remote path:";
            // 
            // btnBrowseNewRemote
            // 
            btnBrowseNewRemote.Location = new Point(159, 62);
            btnBrowseNewRemote.Name = "btnBrowseNewRemote";
            btnBrowseNewRemote.Size = new Size(75, 23);
            btnBrowseNewRemote.TabIndex = 16;
            btnBrowseNewRemote.Text = "Browse";
            btnBrowseNewRemote.UseVisualStyleBackColor = true;
            btnBrowseNewRemote.Click += btnBrowseNewRemote_Click;
            // 
            // txtNewRemote
            // 
            txtNewRemote.BackColor = Color.FromArgb(64, 64, 64);
            txtNewRemote.ForeColor = SystemColors.WindowText;
            txtNewRemote.Location = new Point(240, 63);
            txtNewRemote.Name = "txtNewRemote";
            txtNewRemote.ReadOnly = true;
            txtNewRemote.Size = new Size(505, 23);
            txtNewRemote.TabIndex = 15;
            // 
            // lblNewLocal
            // 
            lblNewLocal.AutoSize = true;
            lblNewLocal.Location = new Point(17, 37);
            lblNewLocal.Name = "lblNewLocal";
            lblNewLocal.Size = new Size(89, 15);
            lblNewLocal.TabIndex = 14;
            lblNewLocal.Text = "New local path:";
            // 
            // btnBrowseNewLocal
            // 
            btnBrowseNewLocal.Location = new Point(159, 33);
            btnBrowseNewLocal.Name = "btnBrowseNewLocal";
            btnBrowseNewLocal.Size = new Size(75, 23);
            btnBrowseNewLocal.TabIndex = 13;
            btnBrowseNewLocal.Text = "Browse";
            btnBrowseNewLocal.UseVisualStyleBackColor = true;
            btnBrowseNewLocal.Click += btnBrowseNewLocal_Click;
            // 
            // txtNewLocal
            // 
            txtNewLocal.BackColor = Color.FromArgb(64, 64, 64);
            txtNewLocal.ForeColor = SystemColors.WindowText;
            txtNewLocal.Location = new Point(240, 34);
            txtNewLocal.Name = "txtNewLocal";
            txtNewLocal.ReadOnly = true;
            txtNewLocal.Size = new Size(505, 23);
            txtNewLocal.TabIndex = 12;
            // 
            // groupFrom
            // 
            groupFrom.Controls.Add(btnBrowseRemote);
            groupFrom.Controls.Add(txtRemotePath);
            groupFrom.Controls.Add(lblRemotePath);
            groupFrom.Location = new Point(19, 21);
            groupFrom.Name = "groupFrom";
            groupFrom.Size = new Size(761, 64);
            groupFrom.TabIndex = 14;
            groupFrom.TabStop = false;
            groupFrom.Text = "From";
            // 
            // btnBrowseRemote
            // 
            btnBrowseRemote.Location = new Point(159, 25);
            btnBrowseRemote.Name = "btnBrowseRemote";
            btnBrowseRemote.Size = new Size(75, 23);
            btnBrowseRemote.TabIndex = 6;
            btnBrowseRemote.Text = "Browse";
            btnBrowseRemote.UseVisualStyleBackColor = true;
            btnBrowseRemote.Click += btnBrowseRemote_Click;
            // 
            // txtRemotePath
            // 
            txtRemotePath.BackColor = Color.FromArgb(64, 64, 64);
            txtRemotePath.ForeColor = SystemColors.WindowText;
            txtRemotePath.Location = new Point(240, 26);
            txtRemotePath.Name = "txtRemotePath";
            txtRemotePath.ReadOnly = true;
            txtRemotePath.Size = new Size(505, 23);
            txtRemotePath.TabIndex = 5;
            // 
            // frmClone
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(799, 308);
            Controls.Add(groupFrom);
            Controls.Add(grpNew);
            Controls.Add(btnOK);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmClone";
            Text = "Clone Remote";
            grpNew.ResumeLayout(false);
            grpNew.PerformLayout();
            groupFrom.ResumeLayout(false);
            groupFrom.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lblRemotePath;
        private FolderBrowserDialog browseDialog;
        private Button btnOK;
        private GroupBox grpNew;
        private Label lblNewRemote;
        private Button btnBrowseNewRemote;
        private TextBox txtNewRemote;
        private Label lblNewLocal;
        private Button btnBrowseNewLocal;
        private TextBox txtNewLocal;
        private GroupBox groupFrom;
        private Button btnBrowseRemote;
        private TextBox txtRemotePath;
        private Label label1;
        private CheckBox checkboxCompressed;
    }
}