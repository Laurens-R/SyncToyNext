﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.GuiClient.Models
{
    public class RemoteDialogResult
    {
        public string RemotePath { get; set; } = string.Empty;
        public bool IsCompressed { get; set; } = false;
    }
}
