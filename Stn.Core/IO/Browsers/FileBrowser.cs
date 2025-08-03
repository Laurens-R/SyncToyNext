using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.IO.Browsers
{
    public enum FileBrowserMode
    {
        FoldersOnly,
        FoldersAndFiles
    }

    public abstract class FileBrowser
    {
        protected string _currentPath = string.Empty;
        protected string _rootPath = string.Empty;
        protected List<FileBrowserEntry> _files = new List<FileBrowserEntry>();
        protected List<FileBrowserEntry> _directories = new List<FileBrowserEntry>();
        protected ObservableCollection<FileBrowserEntry> _allEntries = new ObservableCollection<FileBrowserEntry>();

        public FileBrowserMode BrowserMode { get; set; } = FileBrowserMode.FoldersAndFiles;

        public ObservableCollection<FileBrowserEntry> AllEntries
        {
            get
            {
                return _allEntries;
            }
        }

        public IEnumerable<FileBrowserEntry> Files
        {
            get
            {
                return _files;
            }
        }

        public IEnumerable<FileBrowserEntry> Directories
        {
            get
            {
                return _directories;
            }
        }

        protected bool IsPartOfRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(_rootPath)) return true;
            if (path.StartsWith(_rootPath)) return true;
            return false;
        }

        protected void AddNavigateUpEntry()
        {
            if (_currentPath != _rootPath)
            {
                _allEntries.Add(new FileBrowserEntry
                {
                    Name = "..",
                    IsFile = false,
                    Type = string.Empty
                });
            }
        }

        public abstract string RootPath { get; set; }
        public abstract string CurrentPath { get; set; }

        public abstract void NavigateTo(FileBrowserEntry directory);
        public abstract void NavigateUp();
        public abstract void Refresh();
    }
}
