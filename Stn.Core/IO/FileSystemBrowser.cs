using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };

            var filesInDirectory = Directory.EnumerateFiles(path, "*.*", options);

            if (Directory.Exists(path))
            {
                _allEntries.Clear();
                _files.Clear();
                _directories.Clear();

                foreach (var file in filesInDirectory)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);

                        _files.Add(new FileBrowserEntry
                        {
                            Name = Path.GetFileName(file),
                            Path = file,
                            Created = fileInfo.CreationTimeUtc,
                            LastModified = fileInfo.LastWriteTimeUtc,
                            Extension = fileInfo.Extension,
                            Size = fileInfo.Length,
                            RelativePath = String.IsNullOrWhiteSpace(_rootPath) ? string.Empty : Path.GetRelativePath(_rootPath, file),
                            IsFile = true
                        });
                    } catch
                    {
                        continue;
                    }

                    _allEntries.Add(_files.Last());
                }

                var directoriesInDirectory = Directory.EnumerateDirectories(path, "*", options);
                foreach (var directory in directoriesInDirectory)
                {
                    var directoryInfo = new DirectoryInfo(directory);

                    _directories.Add(new FileBrowserEntry
                    {
                        Name = directoryInfo.Name,
                        Path = directoryInfo.FullName,
                        Created = directoryInfo.CreationTimeUtc,
                        LastModified = directoryInfo.LastWriteTimeUtc,
                        RelativePath = String.IsNullOrWhiteSpace(_rootPath) ? string.Empty : Path.GetRelativePath(_rootPath, directoryInfo.FullName)
                    });

                    _allEntries.Add(_directories.Last());
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
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };

            var filesInDirectory = Directory.EnumerateFiles(path, "*.*", options);
            foreach (var file in filesInDirectory)
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
                OnFileRemovedHandler?.Invoke(this, e);
            } else
            {
                OnDirectoryRemovedHandler?.Invoke(this, e);
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

        public override void NavigateTo(FileBrowserEntry directory)
        {
            if (directory.IsFile) return;
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
