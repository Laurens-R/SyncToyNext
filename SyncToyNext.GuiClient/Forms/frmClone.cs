using SyncToyNext.Core.SyncPoints;
using SyncToyNext.GuiClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncToyNext.GuiClient.Forms
{


    public partial class frmClone : Form
    {
        public frmClone()
        {
            InitializeComponent();
        }

        public static CloneInformation ShowCloneDialog()
        {
            var frmClone = new frmClone();
            if (frmClone.ShowDialog() == DialogResult.OK)
            {
                return new CloneInformation
                {
                    Result = CloneResult.Completed,
                    NewLocalPath = frmClone.txtNewLocal.Text,
                    NewRemotePath = frmClone.txtNewRemote.Text,
                    NewUseCompression = frmClone.checkboxCompressed.Checked
                };
            }

            return new CloneInformation
            {
                Result = CloneResult.Cancelled
            };
        }

        private void btnBrowseRemote_Click(object sender, EventArgs e)
        {
            if (browseDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtRemotePath.Text = browseDialog.SelectedPath;
            }
        }

        private void btnBrowseNewLocal_Click(object sender, EventArgs e)
        {
            if (browseDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtNewLocal.Text= browseDialog.SelectedPath;
            }
        }

        private void btnBrowseNewRemote_Click(object sender, EventArgs e)
        {
            if (browseDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtNewRemote.Text= browseDialog.SelectedPath;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(String.IsNullOrEmpty(txtRemotePath.Text)
                || String.IsNullOrEmpty(txtNewLocal.Text)
                || String.IsNullOrEmpty(txtNewRemote.Text))
            {
                MessageBox.Show("Make sure that you first fill in all fields before proceeding.");
            }

            try
            {
                string newLocalLocation = txtNewLocal.Text;
                string newRemoteLocation = txtNewRemote.Text;
                string remotePath = txtRemotePath.Text;
                bool isCompressed = checkboxCompressed.Checked;

                ExecuteAsyncTask(() =>
                {
                    Repository.CloneFromOtherRemote(newLocalLocation, newRemoteLocation, remotePath, isCompressed);
                })?.ContinueWith((task)=>
                {
                    this.Invoke(() =>
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private Task? ExecuteAsyncTask(Action task)
        {
            var progressForm = frmProgress.ShowProgressDialog(this);
            Repository.UpdateProgressHandler = progressForm.UpdateHandler;
            var result = Task.Run(task).ContinueWith((task) =>
            {
                Repository.UpdateProgressHandler = null;
                if (progressForm.InvokeRequired)
                {
                    progressForm.Invoke(() =>
                    {
                        Repository.UpdateProgressHandler = null;
                        progressForm.Close();
                        progressForm.Dispose();
                    });
                }
                else
                {
                    Repository.UpdateProgressHandler = null;
                    progressForm.Close();
                    progressForm.Dispose();
                }
            });

            return result;
        }
    }
}
