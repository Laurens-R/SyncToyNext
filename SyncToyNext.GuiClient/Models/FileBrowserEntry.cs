using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.GuiClient.Models
{
    public class FileBrowserEntry
    {
        public string DisplayValue { get; set; } = string.Empty;
        public object? Value { get; set; } = null;

        public override string ToString()
        {
            return DisplayValue;
        }
    }
}
