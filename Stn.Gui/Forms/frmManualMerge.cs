using Stn.Core.Merging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Stn.Gui.Forms
{
    public partial class frmManualMerge : Form
    {
        public frmManualMerge()
        {
            InitializeComponent();
        }

        public static DialogResult ShowMergeDialog(Form parent)
        {
            var newForm = new frmManualMerge();
            return newForm.ShowDialog(parent);
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtSourcePath.Text) || !Directory.Exists(txtSourcePath.Text))
            {
                MessageBox.Show("Please provide a valid source path");
                return;
            }

            if (String.IsNullOrWhiteSpace(txtTargetPath.Text) || !Directory.Exists(txtTargetPath.Text))
            {
                MessageBox.Show("Please provide a valid target path");
                return;
            }

            TwoWayMergePolicy policy = radioPolicySourceLeading.Checked ? TwoWayMergePolicy.SourceWins : TwoWayMergePolicy.Union;

            Merger.ManualMerge(txtSourcePath.Text, txtTargetPath.Text, policy);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnBrowseSource_Click(object sender, EventArgs e)
        {
            if (browserFolders.ShowDialog(this) == DialogResult.OK)
            {
                txtSourcePath.Text = browserFolders.SelectedPath;
            }
        }

        private void btnBrowseTarget_Click(object sender, EventArgs e)
        {
            if (browserFolders.ShowDialog(this) == DialogResult.OK)
            {
                txtTargetPath.Text = browserFolders.SelectedPath;
            }
        }
    }
}
