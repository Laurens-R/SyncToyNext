using System;

namespace Stn.Core.IO
{
    public class FileBrowserFile : FileBrowserEntry
    {
        public string Extensionsion { get; set; } = string.Empty;
        public Int64 Size { get; set; } = 0;
        public bool IsCompressed { get; set; } = false;
        public string ArchivePath { get; set; } = string.Empty;
    }
}
