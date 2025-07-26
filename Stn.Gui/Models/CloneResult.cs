using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Gui.Models
{
    public enum CloneResult
    {
        Cancelled,
        Completed
    }

    public class CloneInformation
    {
        public CloneResult Result { get; set; }
        public string NewLocalPath { get; set; } = string.Empty;
        public string NewRemotePath { get; set; } = string.Empty;
        public bool NewUseCompression { get; set; } = false;
    }
}
