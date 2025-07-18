using SyncToyNext.GuiClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncToyNext.GuiClient
{
    public partial class frmPush : Form
    {

        public string PushID
        {
            get
            {
                return txtID.Text;
            }
        }

        public string PushDescription
        {
            get
            {
                return txtDescription.Text;
            }
        }

        public frmPush()
        {
            InitializeComponent();
        }

        public static PushChangesDialogResult? ShowPushDialog(Form owner)
        {
            var dialog = new frmPush();
            var result = dialog.ShowDialog(owner);

            if (result == DialogResult.OK)
            {
                return new PushChangesDialogResult
                {
                    ID = dialog.PushID,
                    Description = dialog.PushDescription
                };
            }

            return null;
        }

        private void btnPush_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void frmPush_Load(object sender, EventArgs e)
        {
            var generatedId = $"{DateTime.Now:yyyyMMddHHmmss}UTC";
            txtID.Text = generatedId;
        }
    }
}
