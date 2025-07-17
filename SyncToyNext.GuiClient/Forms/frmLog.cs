using SyncToyNext.Core;
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
    public partial class frmLog : Form
    {
        public frmLog()
        {
            InitializeComponent();
            txtLog.Text = UserIO.MessageLog;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            UserIO.ClearLog();
            txtLog.Text = UserIO.MessageLog;
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            txtLog.Text = UserIO.MessageLog;
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void txtLog_Enter(object sender, EventArgs e)
        {
            timerRefresh.Enabled = false;
        }

        private void txtLog_Leave(object sender, EventArgs e)
        {
            timerRefresh.Enabled = false;
        }
    }
}
