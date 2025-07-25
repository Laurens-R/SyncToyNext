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
    public partial class frmProgress : Form
    {
        public frmProgress()
        {
            InitializeComponent();
        }

        public static frmProgress ShowProgressDialog(Form owner)
        {
            var form = new frmProgress();
            form.Show(owner);
            return form;
        }


        public void UpdateHandler(int current, int max, string message)
        {
            progressBar.Minimum = 0;
            progressBar.Maximum = max;
            progressBar.Value = current;
            lblStatusText.Text = message;
        }
    }
}
