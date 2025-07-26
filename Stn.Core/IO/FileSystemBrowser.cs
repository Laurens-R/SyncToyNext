using System;
using System.Collections.Generic;
using System.IO;

namespace Stn.Core.IO
{
    public class FileSystemBrowser : FileBrowser
    {
        private readonly FileSystemWatcher _watcher;
        private DirectoryInfo? _currentDirectory = null;
        private HashSet<string> _allFiles = new HashSet<string>();
        private List<FileSystemEventArgs> _filesSystemChanges = new List<FileSystemEventArgs>();

        private void Repopulate(string path)
        {
            var filesInDirectory = Directory.GetFiles(path);

            if (Directory.Exists(path))
            {
                _files.Clear();
                _directories.Clear();

                foreach (var file in filesInDirectory)
                {
                    var fileInfo = new FileInfo(file);

                    _files.Add(new FileBrowserFile
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        Created = fileInfo.CreationTimeUtc,
                        LastModified = fileInfo.LastWriteTimeUtc,
                        Extensionsion = fileInfo.Extension,
                        Size = fileInfo.Length,
                        RelativePath = String.IsNullOrWhiteSpace(_rootPath) ? string.Empty : Path.GetRelativePath(_rootPath, file)
                    });
                }

                var directoriesInDirectory = Directory.GetDirectories(path);
                foreach (var directory in directoriesInDirectory)
                {
                    var directoryInfo = new DirectoryInfo(directory);

                    _directories.Add(new FileBrowserDirectory
                    {
                        Name = directoryInfo.Name,
                        Path = directoryInfo.FullName,
                        Created = directoryInfo.CreationTimeUtc,
                        LastModified = directoryInfo.LastWriteTimeUtc,
                        RelativePath = String.IsNullOrWhiteSpace(_rootPath) ? string.Empty : Path.GetRelativePath(_rootPath, directoryInfo.FullName)
                    });
                }
            }
        }

        public override string RootPath
        {
            get
            {
                return _rootPath;
            }

            set
            {
                if (!Directory.Exists(value)) throw new IOException("Path does not exists.");
                _rootPath = value;

                if(!IsPartOfRoot(value)) {
                    CurrentPath = value;
                }
            }
        }

        public override string CurrentPath
        {
            get { return _currentPath; }

            set
            {
                if (!Directory.Exists(value)) throw new IOException("Path does not exist.");
                if (!string.IsNullOrWhiteSpace(_rootPath) && !IsPartOfRoot(value)) throw new IOException("Path is outside of root path");
                
                _currentPath = value;
                _currentDirectory = new DirectoryInfo(value);

                Repopulate(_currentPath);
            }
        }

        public IEnumerable<string> AllFiles
        {
            get
            {
                return _allFiles;
            }
        }

        public IEnumerable<FileSystemEventArgs> ChangeHistory
        {
            get
            {
                return _filesSystemChanges;
            }
        }

        public event EventHandler<FileSystemEventArgs>? OnFileCreatedHandler;
        public event EventHandler<FileSystemEventArgs>? OnFileModifiedHandler;
        public event EventHandler<RenamedEventArgs>? OnFileRenamedHandler;
        public event EventHandler<FileSystemEventArgs>? OnFileRemovedHandler;
        public event EventHandler<FileSystemEventArgs>? OnDirectoryCreatedHandler;
        public event EventHandler<FileSystemEventArgs>? OnDirectoryModifiedHandler;
        public event EventHandler<RenamedEventArgs>? OnDirectoryRenamedHandler;
        public event EventHandler<FileSystemEventArgs>? OnDirectoryRemovedHandler;

        public FileSystemBrowser(string path)
        {
            var allFilesInDirectory = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in allFilesInDirectory)
            {
                _allFiles.Add(file);
            }

            CurrentPath = path;
            _watcher = new FileSystemWatcher(path);
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;

            _watcher.Renamed += _watcher_Renamed;
            _watcher.Created += _watcher_Created;
            _watcher.Deleted += _watcher_Deleted;
            _watcher.Changed += _watcher_Changed;
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if(Directory.Exists(e.FullPath))
            {
                OnDirectoryModifiedHandler?.Invoke(this, e);
            } else
            {
                OnFileModifiedHandler?.Invoke(this, e);
            }
            _filesSystemChanges.Add(e);
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if(_allFiles.Contains(e.FullPath))
            {
                _allFiles.Remove(e.FullPath);
                OnFileCreatedHandler?.Invoke(this, e);
            } else
            {
                OnDirectoryCreatedHandler?.Invoke(this, e);
            }
            _filesSystemChanges.Add(e);
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                OnDirectoryCreatedHandler?.Invoke(this, e);
            }
            else
            {
                _allFiles.Add(e.FullPath);
                OnFileCreatedHandler?.Invoke(this, e);
            }
            _filesSystemChanges.Add(e);
        }

        private void _watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                OnDirectoryRenamedHandler?.Invoke(this, e);
            }
            else
            {
                _allFiles.Add(e.FullPath);
                _allFiles.Remove(e.OldFullPath);
                OnFileRenamedHandler?.Invoke(this, e);
            }
            _filesSystemChanges.Add(e);
        }

        public override void NavigateTo(FileBrowserDirectory directory)
        {
            CurrentPath = Path.Combine(CurrentPath, directory.Name);
        }

        public override void NavigateUp()
        {
            if (_currentDirectory != null && _currentDirectory.Parent != null) { 
                var parent = _currentDirectory.Parent;
                if (!IsPartOfRoot(parent.FullName)) return;

                CurrentPath = parent.FullName;
            }
        }

        public void ClearChangeHistory()
        {
            _filesSystemChanges.Clear();
        }
    }
}
