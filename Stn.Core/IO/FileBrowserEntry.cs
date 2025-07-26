using System;

namespace Stn.Core.IO
{
    public class FileBrowserEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.MinValue;
        public DateTime LastModified { get; set; } = DateTime.MinValue;

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
    }
}
