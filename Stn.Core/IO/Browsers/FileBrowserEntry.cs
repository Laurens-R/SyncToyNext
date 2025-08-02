using System;

namespace Stn.Core.IO.Browsers
{
    public class FileBrowserEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;
        public DateTime LastModified { get; set; } = DateTime.MinValue;
        public string Type { get; set; } = string.Empty;
        public long Size { get; set; } = 0;
        public bool IsCompressed { get; set; } = false;
        public string ArchivePath { get; set; } = string.Empty;
        public bool IsFile { get; set; } = false;
        public object? Tag { get; set; } = false;

        public bool IsValidCreated
        {
            get
            {
                return Created != DateTime.MinValue;
            }
        }

        public bool IsValidModified
        {
            get
            {
                return LastModified != DateTime.MinValue;
            }
        }

        public string DisplayValueForDate
        {
            get
            {
                if(IsFile) 
                { 
                    return LastModified.ToLocalTime().ToString();
                } else
                {
                    return string.Empty;
                }
            }
        }
    }
}
