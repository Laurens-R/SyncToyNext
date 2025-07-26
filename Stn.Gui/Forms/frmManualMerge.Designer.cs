namespace Stn.Gui.Forms
{
    partial class frmManualMerge
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
            txtSourcePath = new TextBox();
            txtTargetPath = new TextBox();
            btnBrowseSource = new Button();
            btnBrowseTarget = new Button();
            lblSourceFolder = new Label();
            lblTargetFolder = new Label();
            browserFolders = new FolderBrowserDialog();
            btnMerge = new Button();
            radioPolicySourceLeading = new RadioButton();
            radioPolicyUnion = new RadioButton();
            lblMergePolicy = new Label();
            SuspendLayout();
            // 
            // txtSourcePath
            // 
            txtSourcePath.Location = new Point(123, 38);
            txtSourcePath.Name = "txtSourcePath";
            txtSourcePath.Size = new Size(469, 23);
            txtSourcePath.TabIndex = 0;
            // 
            // txtTargetPath
            // 
            txtTargetPath.Location = new Point(123, 79);
            txtTargetPath.Name = "txtTargetPath";
            txtTargetPath.Size = new Size(469, 23);
            txtTargetPath.TabIndex = 1;
            // 
            // btnBrowseSource
            // 
            btnBrowseSource.Location = new Point(610, 38);
            btnBrowseSource.Name = "btnBrowseSource";
            btnBrowseSource.Size = new Size(75, 23);
            btnBrowseSource.TabIndex = 2;
            btnBrowseSource.Text = "Browse";
            btnBrowseSource.UseVisualStyleBackColor = true;
            btnBrowseSource.Click += btnBrowseSource_Click;
            // 
            // btnBrowseTarget
            // 
            btnBrowseTarget.Location = new Point(610, 78);
            btnBrowseTarget.Name = "btnBrowseTarget";
            btnBrowseTarget.Size = new Size(75, 23);
            btnBrowseTarget.TabIndex = 3;
            btnBrowseTarget.Text = "Browse";
            btnBrowseTarget.UseVisualStyleBackColor = true;
            btnBrowseTarget.Click += btnBrowseTarget_Click;
            // 
            // lblSourceFolder
            // 
            lblSourceFolder.AutoSize = true;
            lblSourceFolder.Location = new Point(28, 42);
            lblSourceFolder.Name = "lblSourceFolder";
            lblSourceFolder.Size = new Size(77, 15);
            lblSourceFolder.TabIndex = 4;
            lblSourceFolder.Text = "Source folder";
            // 
            // lblTargetFolder
            // 
            lblTargetFolder.AutoSize = true;
            lblTargetFolder.Location = new Point(28, 82);
            lblTargetFolder.Name = "lblTargetFolder";
            lblTargetFolder.Size = new Size(74, 15);
            lblTargetFolder.TabIndex = 5;
            lblTargetFolder.Text = "Target folder";
            // 
            // btnMerge
            // 
            btnMerge.Location = new Point(567, 175);
            btnMerge.Name = "btnMerge";
            btnMerge.Size = new Size(118, 48);
            btnMerge.TabIndex = 6;
            btnMerge.Text = "Merge";
            btnMerge.UseVisualStyleBackColor = true;
            btnMerge.Click += btnMerge_Click;
            // 
            // radioPolicySourceLeading
            // 
            radioPolicySourceLeading.AutoSize = true;
            radioPolicySourceLeading.Checked = true;
            radioPolicySourceLeading.Location = new Point(123, 118);
            radioPolicySourceLeading.Name = "radioPolicySourceLeading";
            radioPolicySourceLeading.Size = new Size(184, 19);
            radioPolicySourceLeading.TabIndex = 7;
            radioPolicySourceLeading.TabStop = true;
            radioPolicySourceLeading.Text = "Source files are always leading";
            radioPolicySourceLeading.UseVisualStyleBackColor = true;
            // 
            // radioPolicyUnion
            // 
            radioPolicyUnion.AutoSize = true;
            radioPolicyUnion.Location = new Point(123, 150);
            radioPolicyUnion.Name = "radioPolicyUnion";
            radioPolicyUnion.Size = new Size(214, 19);
            radioPolicyUnion.TabIndex = 8;
            radioPolicyUnion.Text = "Perform union on source and target";
            radioPolicyUnion.UseVisualStyleBackColor = true;
            // 
            // lblMergePolicy
            // 
            lblMergePolicy.AutoSize = true;
            lblMergePolicy.Location = new Point(28, 118);
            lblMergePolicy.Name = "lblMergePolicy";
            lblMergePolicy.Size = new Size(76, 15);
            lblMergePolicy.TabIndex = 9;
            lblMergePolicy.Text = "Merge Policy";
            // 
            // frmManualMerge
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(718, 264);
            Controls.Add(lblMergePolicy);
            Controls.Add(radioPolicyUnion);
            Controls.Add(radioPolicySourceLeading);
            Controls.Add(btnMerge);
            Controls.Add(lblTargetFolder);
            Controls.Add(lblSourceFolder);
            Controls.Add(btnBrowseTarget);
            Controls.Add(btnBrowseSource);
            Controls.Add(txtTargetPath);
            Controls.Add(txtSourcePath);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmManualMerge";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Manual Merge";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtSourcePath;
        private TextBox txtTargetPath;
        private Button btnBrowseSource;
        private Button btnBrowseTarget;
        private Label lblSourceFolder;
        private Label lblTargetFolder;
        private FolderBrowserDialog browserFolders;
        private Button btnMerge;
        private RadioButton radioPolicySourceLeading;
        private RadioButton radioPolicyUnion;
        private Label lblMergePolicy;
    }
}