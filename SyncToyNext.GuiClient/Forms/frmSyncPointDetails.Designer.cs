namespace SyncToyNext.GuiClient.Forms
{
    partial class frmSyncPointDetails
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
            groupGeneral = new GroupBox();
            lblReferencePoint = new Label();
            checkBoxReferencePoint = new CheckBox();
            txtID = new TextBox();
            txtCreatedOn = new TextBox();
            txtDescription = new TextBox();
            lblCreatedOn = new Label();
            lblDescription = new Label();
            lvlSPID = new Label();
            listEntries = new ListView();
            columnFile = new ColumnHeader();
            columnType = new ColumnHeader();
            groupGeneral.SuspendLayout();
            SuspendLayout();
            // 
            // groupGeneral
            // 
            groupGeneral.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupGeneral.Controls.Add(lblReferencePoint);
            groupGeneral.Controls.Add(checkBoxReferencePoint);
            groupGeneral.Controls.Add(txtID);
            groupGeneral.Controls.Add(txtCreatedOn);
            groupGeneral.Controls.Add(txtDescription);
            groupGeneral.Controls.Add(lblCreatedOn);
            groupGeneral.Controls.Add(lblDescription);
            groupGeneral.Controls.Add(lvlSPID);
            groupGeneral.Location = new Point(22, 18);
            groupGeneral.Name = "groupGeneral";
            groupGeneral.Size = new Size(547, 726);
            groupGeneral.TabIndex = 0;
            groupGeneral.TabStop = false;
            groupGeneral.Text = "General Information";
            // 
            // lblReferencePoint
            // 
            lblReferencePoint.AutoSize = true;
            lblReferencePoint.Location = new Point(18, 89);
            lblReferencePoint.Name = "lblReferencePoint";
            lblReferencePoint.Size = new Size(93, 15);
            lblReferencePoint.TabIndex = 7;
            lblReferencePoint.Text = "Reference point:";
            // 
            // checkBoxReferencePoint
            // 
            checkBoxReferencePoint.AutoSize = true;
            checkBoxReferencePoint.Enabled = false;
            checkBoxReferencePoint.Location = new Point(122, 90);
            checkBoxReferencePoint.Name = "checkBoxReferencePoint";
            checkBoxReferencePoint.Size = new Size(15, 14);
            checkBoxReferencePoint.TabIndex = 6;
            checkBoxReferencePoint.UseVisualStyleBackColor = true;
            // 
            // txtID
            // 
            txtID.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtID.BackColor = Color.FromArgb(64, 64, 64);
            txtID.Location = new Point(121, 25);
            txtID.Name = "txtID";
            txtID.ReadOnly = true;
            txtID.Size = new Size(404, 23);
            txtID.TabIndex = 5;
            // 
            // txtCreatedOn
            // 
            txtCreatedOn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCreatedOn.BackColor = Color.FromArgb(64, 64, 64);
            txtCreatedOn.Location = new Point(121, 54);
            txtCreatedOn.Name = "txtCreatedOn";
            txtCreatedOn.ReadOnly = true;
            txtCreatedOn.Size = new Size(404, 23);
            txtCreatedOn.TabIndex = 4;
            // 
            // txtDescription
            // 
            txtDescription.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtDescription.BackColor = Color.FromArgb(64, 64, 64);
            txtDescription.Location = new Point(121, 114);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.ReadOnly = true;
            txtDescription.ScrollBars = ScrollBars.Both;
            txtDescription.Size = new Size(404, 592);
            txtDescription.TabIndex = 3;
            // 
            // lblCreatedOn
            // 
            lblCreatedOn.AutoSize = true;
            lblCreatedOn.Location = new Point(18, 57);
            lblCreatedOn.Name = "lblCreatedOn";
            lblCreatedOn.Size = new Size(68, 15);
            lblCreatedOn.TabIndex = 2;
            lblCreatedOn.Text = "Created on:";
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(18, 117);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(70, 15);
            lblDescription.TabIndex = 1;
            lblDescription.Text = "Description:";
            // 
            // lvlSPID
            // 
            lvlSPID.AutoSize = true;
            lvlSPID.Location = new Point(19, 28);
            lvlSPID.Name = "lvlSPID";
            lvlSPID.Size = new Size(21, 15);
            lvlSPID.TabIndex = 0;
            lvlSPID.Text = "ID:";
            // 
            // listEntries
            // 
            listEntries.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listEntries.Columns.AddRange(new ColumnHeader[] { columnFile, columnType });
            listEntries.Location = new Point(591, 28);
            listEntries.Name = "listEntries";
            listEntries.Size = new Size(862, 716);
            listEntries.TabIndex = 1;
            listEntries.UseCompatibleStateImageBehavior = false;
            listEntries.View = View.Details;
            // 
            // columnFile
            // 
            columnFile.Text = "File";
            columnFile.Width = 460;
            // 
            // columnType
            // 
            columnType.Text = "Entry Type";
            columnType.Width = 160;
            // 
            // frmSyncPointDetails
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1473, 766);
            Controls.Add(listEntries);
            Controls.Add(groupGeneral);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "frmSyncPointDetails";
            Text = "SyncPoint Details";
            Load += frmSyncPointDetails_Load;
            groupGeneral.ResumeLayout(false);
            groupGeneral.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupGeneral;
        private TextBox txtDescription;
        private Label lblCreatedOn;
        private Label lblDescription;
        private Label lvlSPID;
        private TextBox txtID;
        private TextBox txtCreatedOn;
        private ListView listEntries;
        private ColumnHeader columnFile;
        private ColumnHeader columnType;
        private Label lblReferencePoint;
        private CheckBox checkBoxReferencePoint;
    }
}