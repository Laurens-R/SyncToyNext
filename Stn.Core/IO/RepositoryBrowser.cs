using Stn.Core.SyncPoints;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.IO
{
    public enum RepositoryBrowserFocus
    {
        Local,
        Remote
    }

    public class RepositoryBrowser : FileBrowser
    {
        private FileSystemBrowser _localBrowser;
        private Repository _repository;
        private SyncPoint _currentSyncPoint;
        private RepositoryBrowserFocus _repositoryFocus;

        public override string RootPath
        {
            get 
            {
                return _rootPath;
            }

            set
            {
                if (RepositoryFocus == RepositoryBrowserFocus.Local)
                {
                    _localBrowser.RootPath = value;
                } else
                {
                    _rootPath = string.Empty;
                }
            }
        }

        public override string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                if (RepositoryFocus == RepositoryBrowserFocus.Local)
                {
                    _localBrowser.CurrentPath = value;
                }

                _currentPath = value;
                PopulateEntriesAtPath(value);
            }
        }

        public SyncPoint CurrentSyncPoint
        {
            get {  return _currentSyncPoint; }
            set
            {
                if (!_repository.SyncPoints.Any(sp => sp == value)) throw new InvalidOperationException("Syncpoint not part of repository.");
                _currentSyncPoint = value;
            }
        }

        public RepositoryBrowserFocus RepositoryFocus
        {
            get { return _repositoryFocus; }
            set
            {
                _repositoryFocus = value;

                if (_repositoryFocus == RepositoryBrowserFocus.Local)
                {
                    RootPath = _repository.LocalPath;
                    CurrentPath = _repository.LocalPath;
                } else
                {
                    RootPath = String.Empty;
                    CurrentPath = String.Empty;
                }
            }
        }

        public IEnumerable<FileSystemEventArgs> LocalChangeHistory
        {
            get
            {
                return _localBrowser.ChangeHistory;
            }
        }

        private void PopulateEntriesAtPath(string path)
        {
            _allEntries.Clear();

            if (RepositoryFocus == RepositoryBrowserFocus.Local)
            {
                _files.Clear();
                _directories.Clear();
                _files.AddRange(_localBrowser.Files);
                _directories.AddRange(_localBrowser.Directories);
                
                foreach(var file in _files)
                {
                    _allEntries.Add(file);
                }

                foreach(var directory in _directories)
                {
                    _allEntries.Add(directory);
                }
            }
            else
            {
                path = path.Replace('\\', '/');

                _files.Clear();
                _directories.Clear();

                var entriesInRepository = _repository.GetRemoteFiles(_currentSyncPoint.SyncPointId).Where(entry => entry.SourcePath.StartsWith(path));

                var files = entriesInRepository.Where(file =>
                {
                    var pathParts = file.SourcePath
                                        .Replace(path, string.Empty)
                                        .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

                    if (pathParts.Length == 1) return true;

                    return false;
                });

                foreach (var file in files)
                {
                    var remotePathParts = file.RelativeRemotePath.Split(['@'], StringSplitOptions.RemoveEmptyEntries);
                    var isCompressed = remotePathParts.Length > 1;

                    long size = 0;
                    string archivePath = string.Empty;
                    DateTime created = DateTime.MinValue;
                    DateTime modified = DateTime.MinValue;

                    if(!isCompressed)
                    {
                        var filePath = Path.Combine(_repository.RemotePath, _currentSyncPoint.SyncPointId, file.SourcePath);
                        var entryInfo = new FileInfo(filePath);
                        size = entryInfo.Length;
                        created = entryInfo.CreationTime;
                        modified = entryInfo.LastWriteTime;
                    } else
                    {
                        archivePath = Path.Combine(_repository.RemotePath, _currentSyncPoint.SyncPointId, remotePathParts[1]);
                        using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                        using var zipFile = new ZipArchive(stream, ZipArchiveMode.Read);
                        ZipArchiveEntry? entry = zipFile.GetEntry(file.SourcePath);
                        
                        if (entry != null) {
                            size = entry.Length;
                            created = entry.LastWriteTime.UtcDateTime;
                            modified = entry.LastWriteTime.UtcDateTime;
                        } else
                        {
                            throw new IOException("Could not find syncpoint entry in archive.");
                        }
                    }

                    _files.Add(new FileBrowserEntry()
                    {
                        Name = Path.GetFileName(file.SourcePath),
                        Path = file.SourcePath,
                        Extension = Path.GetExtension(file.SourcePath),
                        RelativePath = file.SourcePath,
                        Size = size,
                        Created = created,
                        LastModified = modified,
                        IsCompressed = isCompressed,
                        ArchivePath = archivePath,
                        IsFile = true
                    });

                    _allEntries.Add(_files.Last());
                }

                var entriesInSubfolder = entriesInRepository.Where(file =>
                {
                    var pathParts = file.SourcePath
                                        .Replace(path, string.Empty)
                                        .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

                    if (pathParts.Length > 1) return true;

                    return false;
                });

                HashSet<string> subfolders = new HashSet<string>();

                foreach(var subFolderEntry in entriesInSubfolder)
                {
                    var pathParts = subFolderEntry.SourcePath
                                        .Replace(path, string.Empty)
                                        .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

                    var folderName = pathParts[0];
                    if(!subfolders.Contains(folderName))
                    {
                        _directories.Add(new FileBrowserEntry
                        {
                            Name = folderName,
                            Path = _currentPath + '/' + folderName,
                            RelativePath = _currentPath + "/" + folderName
                        });
                        _allEntries.Add(_directories.Last());
                    }
                }
            }
        }

        public RepositoryBrowser(Repository repository)
        {
            _repository = repository;
            _currentSyncPoint = _repository.LatestSyncPoint ?? throw new InvalidOperationException("There must be at least a single syncpoint in a repository.");
            _localBrowser = new FileSystemBrowser(_repository.LocalPath);
        }

        public override void NavigateTo(FileBrowserEntry directory)
        {
            if (directory.IsFile) return;

            if(RepositoryFocus == RepositoryBrowserFocus.Local)
            {
                _localBrowser.NavigateTo(directory);
            } else
            {
                CurrentPath = Path.Combine(CurrentPath, directory.Name);
            }
        }

        public override void NavigateUp()
        {
            if(RepositoryFocus == RepositoryBrowserFocus.Local)
            {
                _localBrowser.NavigateUp();
            } else
            {
                var pathParts = CurrentPath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
                if(pathParts.Length > 1)
                {
                    CurrentPath = string.Concat(pathParts.Take(pathParts.Length - 1).ToArray());
                }
            }
        }
    }
}
