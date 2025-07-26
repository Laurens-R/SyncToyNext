using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.IO
{

    public abstract class FileBrowser
    {
        protected string _currentPath = string.Empty;
        protected string _rootPath = string.Empty;
        protected List<FileBrowserFile> _files = new List<FileBrowserFile>();
        protected List<FileBrowserDirectory> _directories = new List<FileBrowserDirectory>();

        public IEnumerable<FileBrowserFile> Files
        {
            get
            {
                return _files;
            }
        }

        public IEnumerable<FileBrowserDirectory> Directories
        {
            get
            {
                return _directories;
            }
        }

        protected bool IsPartOfRoot(string path)
        {
            if (String.IsNullOrWhiteSpace(_rootPath)) return true;
            if (path.StartsWith(_rootPath)) return true;
            return false;
        }

        public abstract string RootPath { get; set; }
        public abstract string CurrentPath { get; set; }

        public abstract void NavigateTo(FileBrowserDirectory directory);
        public abstract void NavigateUp();
    }
}
