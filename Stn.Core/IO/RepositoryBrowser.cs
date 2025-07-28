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
        private Repository _repository;
        private SyncPoint _currentSyncPoint;

        public override string RootPath
        {
            get 
            {
                return _rootPath;
            }

            set
            {
                _rootPath = string.Empty;
            }
        }

        public override string CurrentPath
        {
            get { return _currentPath; }
            set
            {
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
                CurrentPath = String.Empty;
            }
        }

        private void PopulateEntriesAtPath(string path)
        {
            _allEntries.Clear();
            AddNavigateUpEntry();

            _files.Clear();
            _directories.Clear();

            var remoteFiles = _repository.GetRemoteFiles(_currentSyncPoint.SyncPointId);
            var entriesInRepository = remoteFiles.Where(entry => entry.SourcePath.StartsWith(path));
            entriesInRepository.Concat(remoteFiles.Where(entry => entry.SourcePath.StartsWith(path.Replace('\\', '/'))));

            //first folders
            var subfolderEntriesInPath = entriesInRepository.Where(file =>
            {
                var pathParts = new string[] { };

                if (string.IsNullOrWhiteSpace(path))
                {
                    pathParts = file.SourcePath
                                        .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                } else
                {
                    pathParts = file.SourcePath
                                       .Replace(path, string.Empty).Replace(path.Replace('\\', '/'), string.Empty)
                                       .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                }

                if (pathParts.Length > 1) return true;

                return false;
            }).OrderBy(entry => entry.SourcePath);

            HashSet<string> subfolders = new HashSet<string>();

            foreach (var subFolderEntry in subfolderEntriesInPath)
            {
                var pathParts = new string[] { };

                if (string.IsNullOrWhiteSpace(path))
                {
                    pathParts = subFolderEntry.SourcePath
                                        .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    pathParts = subFolderEntry.SourcePath
                                       .Replace(path, string.Empty).Replace(path.Replace('\\', '/'), string.Empty)
                                       .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                }

                var folderName = pathParts[0];
                if (!subfolders.Contains(folderName))
                {
                    subfolders.Add(folderName);

                    _directories.Add(new FileBrowserEntry
                    {
                        Name = folderName,
                        Path = _currentPath + '/' + folderName,
                        RelativePath = _currentPath + "/" + folderName,
                        Type = "[ FOLDER ]"
                    });
                    _allEntries.Add(_directories.Last());
                }
            }

            //then files

            if (BrowserMode == FileBrowserMode.FoldersAndFiles)
            {
                var files = entriesInRepository.Where(file =>
                {
                    var pathParts = new string[] { };

                    if (string.IsNullOrWhiteSpace(path))
                    {
                        pathParts = file.SourcePath
                                            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        pathParts = file.SourcePath
                                           .Replace(path, string.Empty).Replace(path.Replace('\\', '/'), string.Empty)
                                           .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (pathParts.Length == 1) return true;

                    return false;
                }).OrderBy(entry => entry.SourcePath);

                foreach (var file in files)
                {
                    var remotePathParts = file.RelativeRemotePath.Split(['@'], StringSplitOptions.RemoveEmptyEntries);
                    var isCompressed = remotePathParts.Length > 1;

                    long size = 0;
                    string archivePath = string.Empty;
                    DateTime created = DateTime.MinValue;
                    DateTime modified = DateTime.MinValue;

                    if (!isCompressed)
                    {
                        var filePath = Path.Combine(_repository.RemotePath, _currentSyncPoint.SyncPointId, file.SourcePath);
                        var entryInfo = new FileInfo(filePath);
                        size = entryInfo.Length;
                        created = entryInfo.CreationTime;
                        modified = entryInfo.LastWriteTime;
                    }
                    else
                    {
                        archivePath = Path.Combine(_repository.RemotePath, remotePathParts[1]);
                        using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                        using var zipFile = new ZipArchive(stream, ZipArchiveMode.Read);
                        ZipArchiveEntry? entry = zipFile.GetEntry(file.SourcePath.Replace('\\', '/'));

                        if (entry != null)
                        {
                            size = entry.Length;
                            created = entry.LastWriteTime.UtcDateTime;
                            modified = entry.LastWriteTime.UtcDateTime;
                        }
                        else
                        {
                            throw new IOException("Could not find syncpoint entry in archive.");
                        }
                    }

                    _files.Add(new FileBrowserEntry()
                    {
                        Name = Path.GetFileName(file.SourcePath),
                        Path = file.SourcePath,
                        Type = Path.GetExtension(file.SourcePath),
                        RelativePath = file.SourcePath,
                        Size = size,
                        Created = created,
                        LastModified = modified,
                        IsCompressed = isCompressed,
                        ArchivePath = archivePath,
                        IsFile = true,
                        Tag = file
                    });

                    _allEntries.Add(_files.Last());
                }
            }
        }

        public RepositoryBrowser(Repository repository)
        {
            _repository = repository;
            _currentSyncPoint = _repository.LatestSyncPoint ?? throw new InvalidOperationException("There must be at least a single syncpoint in a repository.");
            CurrentPath = "";
            PopulateEntriesAtPath(CurrentPath);
        }

        public override void NavigateTo(FileBrowserEntry directory)
        {
            if (directory.IsFile) return;

            CurrentPath = Path.Combine(CurrentPath, directory.Name);
        }

        public override void NavigateUp()
        {
            var pathParts = CurrentPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            if(pathParts.Length > 1)
            {
                CurrentPath = string.Concat(pathParts.Take(pathParts.Length - 1).ToArray());
            } else
            {
                CurrentPath = string.Empty;
            }
        }

        public override void Refresh()
        {
            PopulateEntriesAtPath(CurrentPath);
        }
    }
}
