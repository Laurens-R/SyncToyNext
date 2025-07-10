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
        private readonly ISynchronizer _synchronizer;
        private readonly SyncInterval _syncInterval;
        private readonly Queue<(string srcPath, string destOrRelPath)>? _pendingChanges;
        private readonly object _queueLock = new object();
        private DateTime? _lastQueueProcessTime;
        private volatile bool _isProcessingQueue = false;
        private readonly Thread? _intervalThread;
        private volatile bool _shutdownRequested = false;
        private readonly bool _strictMode;
        private readonly ManualResetEventSlim _shutdownEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSyncWatcher"/> class.
        /// </summary>
        /// <param name="sourcePath">The directory to monitor for changes.</param>
        /// <param name="destinationPath">The directory or zip file to synchronize files to.</param>
        /// <param name="overwriteOption">The overwrite behavior for file synchronization.</param>
        /// <param name="destinationIsZip">Whether the destination is a zip file.</param>
        /// <param name="syncInterval">The synchronization interval.</param>
        /// <param name="strictMode">Whether to enable strict mode for synchronization.</param>
        public FileSyncWatcher(string sourcePath, string destinationPath, OverwriteOption overwriteOption, bool destinationIsZip = false, SyncInterval syncInterval = SyncInterval.Realtime, bool strictMode = false)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
            _overwriteOption = overwriteOption;
            _destinationIsZip = destinationIsZip;
            _logger = new Logger(_sourcePath);
            _syncInterval = syncInterval;
            _strictMode = strictMode;
            if (_destinationIsZip)
                _synchronizer = new ZipFileSynchronizer(_destinationPath, _overwriteOption, _logger, strictMode);
            else
                _synchronizer = new FileSynchronizer(_destinationPath, _overwriteOption, _logger, strictMode);

            if (_syncInterval != SyncInterval.Realtime)
            {
                _pendingChanges = new Queue<(string, string)>();
                _intervalThread = new Thread(IntervalThreadLoop) { IsBackground = true };
                _intervalThread.Start();
            }

            _watcher = new FileSystemWatcher(_sourcePath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.Created += OnChangedOrCreated;
            _watcher.Changed += OnChangedOrCreated;
            _watcher.Renamed += OnRenamed;
            _watcher.EnableRaisingEvents = true;
        }

        private void IntervalThreadLoop()
        {
            while (!_shutdownRequested)
            {
                DateTime now = DateTime.UtcNow;
                bool shouldProcess = false;
                if (_syncInterval == SyncInterval.Hourly)
                {
                    if (!_lastQueueProcessTime.HasValue || (now - _lastQueueProcessTime.Value).TotalMinutes >= 60)
                        shouldProcess = true;
                }
                else if (_syncInterval == SyncInterval.Daily)
                {
                    if (!_lastQueueProcessTime.HasValue || (now - _lastQueueProcessTime.Value).TotalHours >= 24)
                        shouldProcess = true;
                }
                if (shouldProcess)
                {
                    ProcessQueuedChanges();
                }
                // Wait for either the shutdown event or a minute to pass
                _shutdownEvent.Wait(TimeSpan.FromMinutes(1));
            }
        }

        private void OnChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    var relativePath = Path.GetRelativePath(_sourcePath, e.FullPath);
                    // Ignore any changes in the synclogs subfolder
                    if (relativePath.StartsWith("synclogs" + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        return;
                    if (_syncInterval == SyncInterval.Realtime)
                    {
                        if (_destinationIsZip)
                            _synchronizer.SynchronizeFile(e.FullPath, relativePath);
                        else
                            _synchronizer.SynchronizeFile(e.FullPath, System.IO.Path.Combine(_destinationPath, relativePath));
                    }
                    else
                    {
                        // Queue the change for interval-based processing
                        lock (_queueLock)
                        {
                            if (_isProcessingQueue) return;
                            if (_destinationIsZip)
                                _pendingChanges?.Enqueue((e.FullPath, relativePath));
                            else
                                _pendingChanges?.Enqueue((e.FullPath, System.IO.Path.Combine(_destinationPath, relativePath)));
                        }
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

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // Ignore changes in synclogs
                var relativePath = Path.GetRelativePath(_sourcePath, e.FullPath);

                if (relativePath.StartsWith("synclogs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return;


                //get the old full destination path
                var oldRelativePath = Path.GetRelativePath(_sourcePath, e.OldFullPath);
                var oldDestPath = _destinationIsZip ? oldRelativePath : System.IO.Path.Combine(_destinationPath, oldRelativePath);

                if (File.Exists(e.FullPath))
                {
                    if (_destinationIsZip)
                        _synchronizer.SynchronizeFile(e.FullPath, relativePath, oldDestPath);
                    else
                        _synchronizer.SynchronizeFile(e.FullPath, System.IO.Path.Combine(_destinationPath, relativePath), oldDestPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling rename event for '{e.OldFullPath}' to '{e.FullPath}'", ex);
            }
        }

        /// <summary>
        /// Processes all queued changes and executes the synchronization for each.
        /// </summary>
        public void ProcessQueuedChanges()
        {
            if (_pendingChanges == null) return;
            lock (_queueLock)
            {
                _isProcessingQueue = true;
                try
                {
                    while (_pendingChanges.Count > 0)
                    {
                        var (srcPath, destOrRelPath) = _pendingChanges.Dequeue();
                        _synchronizer.SynchronizeFile(srcPath, destOrRelPath);
                    }
                    _lastQueueProcessTime = DateTime.UtcNow;
                }
                finally
                {
                    _isProcessingQueue = false;
                }
            }
        }

        /// <summary>
        /// Disposes the underlying FileSystemWatcher and stops the interval thread.
        /// </summary>
        public void Dispose()
        {
            _shutdownRequested = true;
            _shutdownEvent.Set();
            _intervalThread?.Join();
            ProcessQueuedChanges();
            _watcher.Dispose();
            _shutdownEvent.Dispose();
        }
    }
}
