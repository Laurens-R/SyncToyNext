using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() => {
                    progressBar.Minimum = 0;
                    progressBar.Maximum = max;
                    progressBar.Value = current;
                }));   
            }
            else
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = max;
                progressBar.Value = current;
            }

            if (lblStatusText.InvokeRequired)
            {
                lblStatusText.Invoke(new Action(() =>
                {
                    lblStatusText.Text = message;
                }));
            } else
            {
                lblStatusText.Text = message;
            }


                Application.DoEvents();
        }
    }
}
