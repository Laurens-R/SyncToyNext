using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Stn.Core.IO
{
    public class ZipArchiveBrowser : FileBrowser, IDisposable
    {
        private ZipArchive _archive;
        private FileStream _fileStream;
        private FileBrowserDirectory? _currentDirectory = null;

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
            return _archive.Entries.Any(entry => entry.FullName.StartsWith(basePath));
        }

        private void PopulateEntriesAtPath(string path)
        {
            //first process files
            var filesAtLevel = _archive.Entries.Where(entry =>
            {
                bool isRootedInPath = entry.FullName.StartsWith(path);
                if (!isRootedInPath) return false;

                string strippedPath = entry.FullName.Replace(path, string.Empty);
                var pathParts = strippedPath.Split('/');

                if (pathParts.Length != 1) return false;

                return true;
            });

            _files.Clear();

            foreach (var file in filesAtLevel)
            {
                _files.Add(new FileBrowserFile
                {
                    Name = file.Name,
                    Path = file.FullName,
                    Created = file.LastWriteTime.UtcDateTime,
                    LastModified = file.LastWriteTime.UtcDateTime,
                    Extensionsion = Path.GetExtension(file.FullName),
                    RelativePath = file.FullName,
                    Size = file.Length
                });
            }

            //then process directories.
            var childEntriesAtLevel = _archive.Entries.Where(entry =>
            {
                bool isRootedInPath = entry.FullName.StartsWith(path);
                if (!isRootedInPath) return false;

                string strippedPath = entry.FullName.Replace(path, string.Empty);
                var pathParts = strippedPath.Split('/');

                if (pathParts.Length > 1) return false;

                return true;
            });

            var directoriesAtLevel = new HashSet<string>();
            _directories.Clear();

            foreach (var entry in childEntriesAtLevel)
            {
                string strippedPath = entry.FullName.Replace(path, string.Empty);
                var pathParts = strippedPath.Split('/');
                if(directoriesAtLevel.Contains(pathParts[0]))
                {
                    directoriesAtLevel.Add(pathParts[0]);

                    _directories.Add(new FileBrowserDirectory
                    {
                        Name = pathParts[0],
                        Path = Path.Combine(path, pathParts[0]),
                        RelativePath = Path.Combine(path, pathParts[0])
                    });
                }
            }
        }


        public ZipArchiveBrowser(string zipArchivePath)
        {
            var directoryOfArchive = Path.GetDirectoryName(zipArchivePath);
            if (directoryOfArchive == null) throw new IOException("Could not determine path of archive");
            if (!Directory.Exists(directoryOfArchive)) Directory.CreateDirectory(directoryOfArchive);

            _fileStream = new FileStream(zipArchivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _archive = new ZipArchive(_fileStream, ZipArchiveMode.Update);
        }

        public override void NavigateTo(FileBrowserDirectory directory)
        {
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

        public void Dispose()
        {
            _archive.Dispose();
            _fileStream.Dispose();
        }
    }
}
