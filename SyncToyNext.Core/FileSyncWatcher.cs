using System;
using System.IO;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Monitors a directory for file creation and update events, and synchronizes affected files to a destination using FileSynchronizer.
    /// </summary>
    public class FileSyncWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _sourcePath;
        private readonly string _destinationPath;
        private readonly OverwriteOption _overwriteOption;
        private readonly bool _destinationIsZip;
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSyncWatcher"/> class.
        /// </summary>
        /// <param name="sourcePath">The directory to monitor for changes.</param>
        /// <param name="destinationPath">The directory or zip file to synchronize files to.</param>
        /// <param name="overwriteOption">The overwrite behavior for file synchronization.</param>
        /// <param name="destinationIsZip">Whether the destination is a zip file.</param>
        public FileSyncWatcher(string sourcePath, string destinationPath, OverwriteOption overwriteOption, bool destinationIsZip = false)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
            _overwriteOption = overwriteOption;
            _destinationIsZip = destinationIsZip;
            _logger = new Logger(_sourcePath);

            _watcher = new FileSystemWatcher(_sourcePath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.Created += OnChangedOrCreated;
            _watcher.Changed += OnChangedOrCreated;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    var relativePath = Path.GetRelativePath(_sourcePath, e.FullPath);
                    if (_destinationIsZip)
                    {
                        var zipSync = new ZipFileSynchronizer(_destinationPath, _overwriteOption, _logger);
                        zipSync.SynchronizeFile(e.FullPath, relativePath);
                    }
                    else
                    {
                        FileSynchronizer.SynchronizeFile(e.FullPath, Path.Combine(_destinationPath, relativePath), _overwriteOption, _logger);
                    }
                }
            }
            catch (IOException ioEx)
            {
                _logger.LogError($"IO error syncing '{e.FullPath}'", ioEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError($"Access denied syncing '{e.FullPath}'", uaEx);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error syncing '{e.FullPath}'", ex);
            }
        }

        /// <summary>
        /// Disposes the underlying FileSystemWatcher.
        /// </summary>
        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}
