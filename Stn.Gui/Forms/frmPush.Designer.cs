namespace Stn.Gui
{
    partial class frmPush
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
            btnPush = new Button();
            txtID = new TextBox();
            txtDescription = new TextBox();
            lblID = new Label();
            lblDescription = new Label();
            SuspendLayout();
            // 
            // btnPush
            // 
            btnPush.Location = new Point(431, 264);
            btnPush.Name = "btnPush";
            btnPush.Size = new Size(159, 47);
            btnPush.TabIndex = 0;
            btnPush.Text = "Push";
            btnPush.UseVisualStyleBackColor = true;
            btnPush.Click += btnPush_Click;
            // 
            // txtID
            // 
            txtID.Location = new Point(131, 25);
            txtID.Name = "txtID";
            txtID.Size = new Size(459, 23);
            txtID.TabIndex = 1;
            // 
            // txtDescription
            // 
            txtDescription.Location = new Point(131, 60);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new Size(459, 187);
            txtDescription.TabIndex = 2;
            // 
            // lblID
            // 
            lblID.AutoSize = true;
            lblID.Location = new Point(12, 25);
            lblID.Name = "lblID";
            lblID.Size = new Size(18, 15);
            lblID.TabIndex = 3;
            lblID.Text = "ID";
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(12, 60);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(67, 15);
            lblDescription.TabIndex = 4;
            lblDescription.Text = "Description";
            // 
            // frmPush
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(606, 328);
            Controls.Add(lblDescription);
            Controls.Add(lblID);
            Controls.Add(txtDescription);
            Controls.Add(txtID);
            Controls.Add(btnPush);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmPush";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Push Changes";
            Load += frmPush_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnPush;
        private TextBox txtID;
        private TextBox txtDescription;
        private Label lblID;
        private Label lblDescription;
    }
}