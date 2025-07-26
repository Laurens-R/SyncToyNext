using SyncToyNext.Core;
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

namespace SyncToyNext.GuiClient.Forms
{
    public partial class frmRemote : Form
    {
        public string RemotePath
        {
            get
            {
                return txtRemotePath.Text;
            }
        }

        public bool IsCompressed
        {
            get
            {
                return checkBoxCompression.Checked;
            }
        }

        public frmRemote()
        {
            InitializeComponent();
        }

        public static RemoteDialogResult? ShowRemoteDialog(Form parent)
        {
            var form = new frmRemote();

            if (form.ShowDialog(parent) == DialogResult.OK)
            {
                return new RemoteDialogResult
                {
                    IsCompressed = form.IsCompressed,
                    RemotePath = form.RemotePath
                };
            }

            return null;
        }

        public static RemoteDialogResult? ShowRemoteDialog(Form parent, RemoteConfig existingConfig)
        {
            var form = new frmRemote();
            form.checkBoxCompression.Checked = Path.HasExtension(existingConfig.RemotePath) && Path.GetExtension(existingConfig.RemotePath) == ".zip";
            form.txtRemotePath.Text = form.checkBoxCompression.Checked ? (Path.GetDirectoryName(existingConfig.RemotePath) ?? string.Empty) : existingConfig.RemotePath;

            if (form.ShowDialog(parent) == DialogResult.OK)
            {
                return new RemoteDialogResult
                {
                    IsCompressed = form.IsCompressed,
                    RemotePath = form.RemotePath
                };
            }

            return null;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var confirmationResult = MessageBox.Show(
                "Are you sure you want to proceed with the provided information?",
                "Are you sure",
                MessageBoxButtons.YesNo);

            if (confirmationResult == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void btnBrowseRemote_Click(object sender, EventArgs e)
        {
            if(browserRemote.ShowDialog(this) == DialogResult.OK)
            {
                txtRemotePath.Text = browserRemote.SelectedPath;
            }
        }
    }
}
