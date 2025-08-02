using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ZipLib = ICSharpCode.SharpZipLib.Zip;

namespace Stn.Core.IO.Browsers
{
    public class ZipArchiveBrowser : FileBrowser, IDisposable
    {
        private ZipLib.ZipFile _archive;
        private FileStream _fileStream;
        private FileBrowserEntry? _currentDirectory = null;

        //Root path should always be empty for zip archives (as the are always relative).
        public override string RootPath { 
            get { return _rootPath; }
            set { _rootPath = string.Empty; }
        }

        public override string CurrentPath { 
            get
            {
                return _currentPath;
            }

            set
            {
                if (HasEntriesWithBasePath(value))
                {
                    PopulateEntriesAtPath(value);
                    _currentPath = value;
                } else
                {
                    throw new IOException("Relative folder is not present in archive.");
                }
            }
        }

        private bool HasEntriesWithBasePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return true;
            return _archive.Any(entry => entry.Name.StartsWith(basePath));
        }

        private void PopulateEntriesAtPath(string path)
        {
            _allEntries.Clear();

            //first process files
            if (BrowserMode == FileBrowserMode.FoldersAndFiles)
            {
                var filesAtLevel = _archive.Where(entry =>
                {
                    bool isRootedInPath = entry.Name.StartsWith(path);
                    if (!isRootedInPath) return false;

                    string strippedPath = entry.Name.Replace(path, string.Empty);
                    var pathParts = strippedPath.Split('/');

                    if (pathParts.Length != 1) return false;

                    return true;
                });

                _files.Clear();

                foreach (var file in filesAtLevel)
                {
                    _files.Add(new FileBrowserEntry
                    {
                        Name = Path.GetFileName(file.Name),
                        Path = file.Name,
                        Created = file.DateTime,
                        LastModified = file.DateTime,
                        Type = Path.GetExtension(file.Name),
                        RelativePath = file.Name,
                        Size = file.Size,
                        IsFile = true
                    });

                    _allEntries.Add(_files.Last());
                }
            }

            //then process directories.
            var childEntriesAtLevel = _archive.Where(entry =>
            {
                bool isRootedInPath = entry.Name.StartsWith(path);
                if (!isRootedInPath) return false;

                string strippedPath = entry.Name.Replace(path, string.Empty);
                var pathParts = strippedPath.Split('/');

                if (pathParts.Length > 1) return false;

                return true;
            });

            var directoriesAtLevel = new HashSet<string>();
            _directories.Clear();

            foreach (var entry in childEntriesAtLevel)
            {
                string strippedPath = entry.Name.Replace(path, string.Empty);
                var pathParts = strippedPath.Split('/');
                if(directoriesAtLevel.Contains(pathParts[0]))
                {
                    directoriesAtLevel.Add(pathParts[0]);

                    _directories.Add(new FileBrowserEntry
                    {
                        Name = pathParts[0],
                        Path = Path.Combine(path, pathParts[0]),
                        RelativePath = Path.Combine(path, pathParts[0])
                    });

                    _allEntries.Add(_directories.Last());
                }
            }
        }


        public ZipArchiveBrowser(string zipArchivePath)
        {
            var directoryOfArchive = Path.GetDirectoryName(zipArchivePath);
            if (directoryOfArchive == null) throw new IOException("Could not determine path of archive");
            if (!Directory.Exists(directoryOfArchive)) Directory.CreateDirectory(directoryOfArchive);

            _fileStream = new FileStream(zipArchivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _archive = new ZipLib.ZipFile(_fileStream, false);
        }

        public override void NavigateTo(FileBrowserEntry directory)
        {
            if (directory.IsFile) return;
            var newPath = CurrentPath + '/'+ directory.Name;
            if (!HasEntriesWithBasePath(newPath)) throw new IOException("No entries for subfolder.");
            _currentDirectory = directory;
            CurrentPath = newPath;
        }

        public override void NavigateUp()
        {
            if(_currentDirectory != null)
            {
                var pathParts = _currentDirectory.Path.Split("/", StringSplitOptions.RemoveEmptyEntries);
                if(pathParts.Length > 1)
                {
                    var parentPathParts = pathParts.Take(pathParts.Length - 1);
                    var parentPath = string.Concat(parentPathParts, "/");
                    CurrentPath = parentPath;
                }
            }
        }

        public override void Refresh()
        {
            PopulateEntriesAtPath(CurrentPath);
        }

        public void Dispose()
        {
            _archive.Close();
        }
    }
}
