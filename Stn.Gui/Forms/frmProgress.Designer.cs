namespace SyncToyNext.GuiClient.Forms
{
    partial class frmProgress
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
            progressBar = new ProgressBar();
            lblStatusText = new Label();
            SuspendLayout();
            // 
            // progressBar
            // 
            progressBar.Location = new Point(36, 55);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(508, 27);
            progressBar.TabIndex = 0;
            // 
            // lblStatusText
            // 
            lblStatusText.Location = new Point(37, 30);
            lblStatusText.Name = "lblStatusText";
            lblStatusText.Size = new Size(507, 23);
            lblStatusText.TabIndex = 1;
            lblStatusText.Text = "Please wait...";
            // 
            // frmProgress
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(583, 129);
            Controls.Add(lblStatusText);
            Controls.Add(progressBar);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmProgress";
            Text = "Progress";
            ResumeLayout(false);
        }

        #endregion

        private ProgressBar progressBar;
        private Label lblStatusText;
    }
}