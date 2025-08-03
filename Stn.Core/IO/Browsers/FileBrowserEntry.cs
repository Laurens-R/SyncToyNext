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

        public string DisplayValueForSize
        {
            get
            {
                if (IsFile)
                {
                    if (Size < 1000)
                    {
                        return "1 KB";
                    }
                    else
                    {
                        //to be frank, this is modelled after how Windows rounds the sizes in explorer.
                        //not sure if that is universal across linux and macos, so I might
                        //need to align this calculation later with every platform.
                        var kbSize = (Size / 1024) + (Size % 1024 > 0 ? 1 : 0);
                        return kbSize.ToString("N0") + " KB";
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string IconPath
        {
            get
            {
                if (IsFile)
                    return "/Assets/file.png";
                else
                    return "/Assets/folder.png";
            }
        }
    }
}
