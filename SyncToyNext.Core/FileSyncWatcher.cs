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
                        var logger = new Logger(_sourcePath);
                        var zipSync = new ZipFileSynchronizer(_destinationPath, _overwriteOption, logger);
                        zipSync.SynchronizeFile(e.FullPath, relativePath);
                    }
                    else
                    {
                        FileSynchronizer.SynchronizeFile(e.FullPath, Path.Combine(_destinationPath, relativePath), _overwriteOption, new Logger(_sourcePath));
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.Error.WriteLine($"[FileSyncWatcher] IO error syncing '{e.FullPath}': {ioEx.Message}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Console.Error.WriteLine($"[FileSyncWatcher] Access denied syncing '{e.FullPath}': {uaEx.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FileSyncWatcher] Unexpected error syncing '{e.FullPath}': {ex.Message}");
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
