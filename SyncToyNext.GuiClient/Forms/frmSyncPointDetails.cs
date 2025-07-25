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
    public partial class frmSyncPointDetails : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SyncPoint? SyncPoint { get; set; } = null;

        public frmSyncPointDetails()
        {
            InitializeComponent();
        }

        public static void ShowSyncPointDetailsDialog(SyncPoint syncPoint, Form parent)
        {
            var form = new frmSyncPointDetails();
            form.SyncPoint = syncPoint;
            form.ShowDialog(parent);
        }

        private void frmSyncPointDetails_Load(object sender, EventArgs e)
        {
            if (SyncPoint != null)
            {
                txtID.Text = SyncPoint.SyncPointId;

                var localDateTime = SyncPoint.LastSyncTime.ToLocalTime();
                var localDate = localDateTime.ToShortDateString();
                var localTime = localDateTime.ToShortTimeString();
                txtCreatedOn.Text = $"{localDate} at {localTime}";
                checkBoxReferencePoint.Checked = SyncPoint.ReferencePoint;

                txtDescription.Text = SyncPoint.Description;

                foreach (var entry in SyncPoint.Entries)
                {
                    var item = listEntries.Items.Add(entry.SourcePath);
                    item.ToolTipText = entry.SourcePath;
                    item.SubItems.Add(entry.EntryType.ToString());
                }
            }

        }
    }
}
