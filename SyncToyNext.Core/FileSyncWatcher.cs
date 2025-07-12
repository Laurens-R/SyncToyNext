using System;
using System.Dynamic;
using System.IO;
using System.Text.Json.Serialization;

namespace SyncToyNext.Core
{
    enum SyncTypes
    {
        Create,
        Update,
        Rename,
        Delete
    }

    class FileSyncAction
    {
        public string SourcePath { get; set; } = String.Empty;
        public string DestinationPath { get; set; } = String.Empty;
        public string OldDestinationPath { get; set; } = string.Empty;
        public string OldFullSourcePath { get; set; } = string.Empty;
        public SyncTypes ActionType { get; set; } = SyncTypes.Create;
    }


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
        private readonly SyncMode _syncMode;
        private readonly Stack<FileSyncAction>? _pendingChanges;
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
        public FileSyncWatcher(string sourcePath, string destinationPath, OverwriteOption overwriteOption, bool destinationIsZip = false, SyncInterval syncInterval = SyncInterval.Realtime, SyncMode mode = SyncMode.Incremental, bool strictMode = false)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
            _overwriteOption = overwriteOption;
            _destinationIsZip = destinationIsZip;
            _logger = new Logger(_sourcePath);
            _syncInterval = syncInterval;
            _syncMode = mode;
            _strictMode = strictMode;

            if (_destinationIsZip)
                _synchronizer = new ZipFileSynchronizer(_destinationPath, _overwriteOption, _logger, strictMode);
            else
                _synchronizer = new FileSynchronizer(_destinationPath, _overwriteOption, _logger, strictMode);

            if (_syncInterval != SyncInterval.Realtime)
            {
                _pendingChanges = new Stack<FileSyncAction>();
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
                    if (_syncMode == SyncMode.Incremental)
                    {
                        ProcessQueuedChanges();
                    } else if (_syncMode == SyncMode.FullSync)
                    {
                        // Perform a full synchronization
                        FullSynchronization();
                    }   
                }
                // Wait for either the shutdown event or a minute to pass
                _shutdownEvent.Wait(TimeSpan.FromMinutes(1));
            }
        }

        private void OnChangedOrCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Ignore changes if the file is already being processed in the queue and we are not in realtime mode.
                if (_syncInterval != SyncInterval.Realtime && e.ChangeType == WatcherChangeTypes.Changed) 
                {
                    lock(_queueLock)
                    {
                        if(_pendingChanges != null && _pendingChanges.Any(x => x.SourcePath == e.FullPath))
                        {
                            return; // Ignore changes if the file is already queued because it would create a double operation.
                        }
                    }
                }

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
                    else if(_syncMode == SyncMode.Incremental)
                    {
                        // Queue the change for interval-based processing
                        lock (_queueLock)
                        {
                            if (_isProcessingQueue) return;
                            if (_destinationIsZip)
                                _pendingChanges?.Push(new FileSyncAction
                                {
                                    SourcePath = e.FullPath,
                                    DestinationPath = relativePath,
                                    ActionType = e.ChangeType == WatcherChangeTypes.Created ? SyncTypes.Create : SyncTypes.Update
                                });
                            else
                                _pendingChanges?.Push(new FileSyncAction
                                {
                                    SourcePath = e.FullPath,
                                    DestinationPath = System.IO.Path.Combine(_destinationPath, relativePath),
                                    ActionType = e.ChangeType == WatcherChangeTypes.Created ? SyncTypes.Create : SyncTypes.Update
                                });
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
                if (_syncInterval == SyncInterval.Realtime)
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

                        //TODO: add support fo renames in destination files (no mattr if destination is zip or folder)
                        if (_destinationIsZip)
                            _synchronizer.SynchronizeFile(e.FullPath, relativePath, oldDestPath);
                        else
                            _synchronizer.SynchronizeFile(e.FullPath, System.IO.Path.Combine(_destinationPath, relativePath), oldDestPath);
                    }
                }
                else if (_syncMode == SyncMode.Incremental)
                {
                    // Queue the rename for interval-based processing
                    lock (_queueLock)
                    {
                        if (_isProcessingQueue) return;

                        var relativePath = Path.GetRelativePath(_sourcePath, e.FullPath);

                        if (relativePath.StartsWith("synclogs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            return;

                        var existingEntryForFile = _pendingChanges.FirstOrDefault(x => x.SourcePath == e.OldFullPath);

                        if (existingEntryForFile != null)
                        {
                            // If the file is already in the queue, update its destination path
                            existingEntryForFile.SourcePath = e.FullPath;
                            existingEntryForFile.DestinationPath = _destinationIsZip ? relativePath : System.IO.Path.Combine(_destinationPath, relativePath);
                            existingEntryForFile.ActionType = SyncTypes.Rename;
                        }
                        else
                        {
                            //get the old full destination path
                            var oldRelativePath = Path.GetRelativePath(_sourcePath, e.OldFullPath);
                            var oldDestPath = _destinationIsZip ? oldRelativePath : System.IO.Path.Combine(_destinationPath, oldRelativePath);

                            _pendingChanges?.Push(new FileSyncAction
                            {
                                SourcePath = e.FullPath,
                                DestinationPath = _destinationIsZip ? relativePath : System.IO.Path.Combine(_destinationPath, relativePath),
                                OldDestinationPath = oldDestPath,
                                ActionType = SyncTypes.Rename,
                                OldFullSourcePath = e.OldFullPath
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling rename event for '{e.OldFullPath}' to '{e.FullPath}'", ex);
            }
        }

        public void FullSynchronization()
        {
            _synchronizer.FullSynchronization(_sourcePath);

        }

        /// <summary>
        /// Processes all queued changes and executes the synchronization for each.
        /// </summary>
        public void ProcessQueuedChanges()
        {
            if (_pendingChanges == null) return;
            lock (_queueLock)
            {
                HashSet<string> processedFiles = new HashSet<string>();

                _isProcessingQueue = true;
                try
                {
                    while (_pendingChanges.Count > 0)
                    {
                        var queueItem = _pendingChanges.Pop();

                        //as we are processing a stack the newest operations are on top
                        //so if the file was already processed, skip it. We already copied
                        //the latest version of the file in the previous iteration
                        //and we don't want to copy it again.
                        if (processedFiles.Contains(queueItem.SourcePath))
                            continue;

                        // the file might have been deleted in the meantime, so we check if it exists
                        if (File.Exists(queueItem.SourcePath))
                        {

                            if (queueItem.ActionType == SyncTypes.Rename)
                            {
                                var oldRelativePath = Path.GetRelativePath(_sourcePath, queueItem.OldDestinationPath);
                                var oldDestPath = _destinationIsZip ? oldRelativePath : System.IO.Path.Combine(_destinationPath, oldRelativePath);
                                _synchronizer.SynchronizeFile(queueItem.SourcePath, queueItem.DestinationPath, queueItem.OldDestinationPath);

                                processedFiles.Add(queueItem.SourcePath);
                                processedFiles.Add(queueItem.OldFullSourcePath);
                            }
                            else
                            {
                                _synchronizer.SynchronizeFile(queueItem.SourcePath, queueItem.DestinationPath);
                                processedFiles.Add(queueItem.SourcePath);
                            }
                        }
                    }
                    _lastQueueProcessTime = DateTime.UtcNow;
                }
                finally
                {
                    _isProcessingQueue = false;
                }

                processedFiles.Clear();
            }
        }

        /// <summary>
        /// Disposes the underlying FileSystemWatcher and stops the interval thread.
        /// </summary>
        public void Dispose()
        {
            _logger.Log($"Shutting down watcher and synchronizer for " + _sourcePath);
            _shutdownRequested = true;
            _shutdownEvent.Set();
            _intervalThread?.Join();

            if (_syncMode == SyncMode.Incremental)
            {
                ProcessQueuedChanges();
            }
            else if (_syncMode == SyncMode.FullSync)
            {
                FullSynchronization();
            }

            _watcher.Dispose();
            _shutdownEvent.Dispose();
        }
    }
}
