using System;
using System.IO;

namespace Stn.Core.IO
{
    public class FileSystemBrowser : FileBrowser
    {
        private DirectoryInfo? _currentDirectory = null;

        private void Repopulate(string path)
        {
            if (Directory.Exists(path))
            {
                _files.Clear();
                _directories.Clear();

                var filesInDirectory = Directory.GetFiles(path);
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

        public FileSystemBrowser(string path)
        {
            CurrentPath = path;
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
    }
}
