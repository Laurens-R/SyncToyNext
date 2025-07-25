using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SyncToyNext.Core
{
    public enum SyncPointRootLoadResult
    {
        Failed,
        LoadedExisting,
        CreatedNew
    }

    public enum CompressionMode
    {
        Unspecified,
        Compressed,
        Uncompressed
    }

    public class SyncPointManagerRootLoadException(string message) : Exception(message) { }

    public class SyncPointManager
    {
        private List<SyncPoint> _syncPoints;
        private string _remotePath;
        private bool _isCompressed = false;
        private SyncPointRoot? _syncPointRoot;

        public bool IsZipped => _isCompressed;

        public SyncPointRoot? SyncPointRoot => _syncPointRoot;

        public string RemotePath => _remotePath;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remotePath"></param>
        /// <param name="compressionMode">If set to unspecified (default) we assume an existing remote will be loaded with an existing config. Otherwise the remote can be initialized with either compression setting.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public SyncPointManager(string remotePath, CompressionMode compressionMode = CompressionMode.Unspecified)
        {
            var originalPath = remotePath;

            if (!Directory.Exists(remotePath))
            {
                throw new InvalidOperationException("Local or remote paths don't exist");
            }

            _remotePath = remotePath;
            _syncPoints = new List<SyncPoint>();

            var syncRootLoadResult = LoadSyncPointRoot();

            if (syncRootLoadResult == SyncPointRootLoadResult.Failed && compressionMode != CompressionMode.Unspecified)
            {
                //if the existing syncroot cannot be found, create one.
                CreateNewSyncRoot(originalPath, compressionMode == CompressionMode.Compressed);
            } else if (syncRootLoadResult == SyncPointRootLoadResult.Failed) {
                return;
            }

            LoadSyncPoints();
            
        }

        public IReadOnlyList<SyncPoint> SyncPoints => _syncPoints.AsReadOnly();

        private SyncPointRootLoadResult LoadSyncPointRoot()
        {
            var rootFilePath = Path.Combine(_remotePath, "syncpointroot.json");
            if (File.Exists(rootFilePath))
            {
                var json = File.ReadAllText(rootFilePath);
                _syncPointRoot =  JsonSerializer.Deserialize(json, SyncPointRootJsonContext.Default.SyncPointRoot) ?? new SyncPointRoot();

                if(_syncPointRoot.IsCompressed)
                {
                    _isCompressed = true;
                }

                if (_syncPointRoot.IsCompressed != _isCompressed)
                {
                    UserIO.Error($"Cannot mix between zipped and non-zipped destination targets when working with syncpoints.");
                    return SyncPointRootLoadResult.Failed;
                }
                
                return SyncPointRootLoadResult.LoadedExisting;
            }

            _syncPointRoot = null;

            return SyncPointRootLoadResult.Failed;
        }

        private void CreateNewSyncRoot(string providedRemotePath, bool isCompressed)
        {
            _syncPointRoot = new SyncPointRoot();
            _syncPointRoot.IsCompressed = isCompressed;
            _isCompressed = isCompressed;

            if (_isCompressed)
            {
                _syncPointRoot.ZipFilename = _syncPointRoot.RootID + ".zip";
            }

            var json = JsonSerializer.Serialize(_syncPointRoot, SyncPointRootJsonContext.Default.SyncPointRoot);
            var rootFilePath = Path.Combine(_remotePath, "syncpointroot.json");
            File.WriteAllText(rootFilePath, json);
        }

        private void LoadSyncPoints()
        {
            var childDirectories = Directory.GetDirectories(_remotePath);

            foreach (var directory in childDirectories)
            {
                Directory.GetFiles(directory, "*.syncpoint.json").ToList().ForEach(file =>
                {
                    try
                    {
                        var syncPoint = SyncPoint.Load(file);
                        _syncPoints.Add(syncPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error loading sync point from {file}: {ex.Message}");
                    }
                });
            }

            // Sort sync points by LastSyncTime, descending
            _syncPoints.Sort((sp1, sp2) => sp2.LastSyncTime.CompareTo(sp1.LastSyncTime));
        }

        public void RefreshSyncPoints()
        {
            _syncPoints.Clear();
            LoadSyncPoints();
        }

        public SyncPoint AddSyncPoint(string sourcePath, string syncPointID = "", string description = "", bool isReferencePoint = false)
        {
            //first check if the syncPointID is already used
            if (_syncPoints.Any(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"SyncPoint ID '{syncPointID}' already exists.");
            }

            DateTime now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(syncPointID))
            {
                syncPointID = SyncPoint.GenerateSyncpointID();

                if (_syncPoints.Any(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Generated SyncPoint ID '{syncPointID}' already exists.");
                }
            }

            Directory.CreateDirectory(Path.Combine(_remotePath, syncPointID));

            var newSyncPoint = new SyncPoint
            {
                SyncPointId = syncPointID,
                Description = description,
                LastSyncTime = now,
                ReferencePoint = isReferencePoint,
                Entries = new List<SyncPointEntry>()
            };

            _syncPoints.Add(newSyncPoint);
            _syncPoints.Sort((sp1, sp2) => sp2.LastSyncTime.CompareTo(sp1.LastSyncTime));

            newSyncPoint.Save(Path.Combine(_remotePath, syncPointID, $"{syncPointID}.syncpoint.json"));

            return newSyncPoint;
        }

        /// <summary>
        /// Get a specific sync point by its ID.
        /// </summary>
        /// <param name="syncPointID"></param>
        /// <returns></returns>
        public SyncPoint? GetSyncPoint(string syncPointID)
        {
            return _syncPoints.FirstOrDefault(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all the file entries at a specific sync point. Not only the entries of the sync point itself, but also all previous sync points that were created before the given sync point.
        /// This way we get a complete state of the files at the time of the sync point.
        /// </summary>
        /// <param name="syncPointID">The syncpoint ID to check</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<SyncPointEntry> GetFileEntriesAtSyncpoint(string syncPointID)
        {
            var requestedSyncPoint = GetSyncPoint(syncPointID);

            if (_syncPointRoot == null) return new List<SyncPointEntry>();

            bool isCompressed = _syncPointRoot.IsCompressed;

            if (requestedSyncPoint == null)
                throw new InvalidOperationException($"SyncPoint with ID '{syncPointID}' not found.");

            var allRelevantSyncPoints = _syncPoints.Where(sp => sp.LastSyncTime < requestedSyncPoint.LastSyncTime)
                                                .OrderByDescending(sp => sp.LastSyncTime)
                                                .ToList();
            
            allRelevantSyncPoints.Insert(0, requestedSyncPoint);

            var result = new List<SyncPointEntry>();

            // Use a HashSet to track included restore targets
            HashSet<string> includedRestoreTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var syncPoint in allRelevantSyncPoints)
            {
                foreach (var entry in syncPoint.Entries)
                {
                    // If the entry is already included in the result, as we sorted the sync points by LastSyncTime descending,
                    // we know that this entry represents the most recent state of the file.
                    if (!includedRestoreTargets.Contains(entry.SourcePath))
                    {
                        //only add if the file has not been deleted.
                        if (entry.EntryType != SyncPointEntryType.Deleted)
                        {
                            //if the entry does not exist in the current sync point, we add it
                            result.Add(new SyncPointEntry
                            {
                                SourcePath = entry.SourcePath,
                                RelativeRemotePath = Path.Combine(entry.RelativeRemotePath),
                                EntryType = entry.EntryType,
                                SyncpointID = syncPoint.SyncPointId
                            });
                        }

                        //we do include the file if it is deleted to prevent historical version from the files to appear
                        //in the result set while it should appear as deleted.
                        includedRestoreTargets.Add(entry.SourcePath);
                    }
                }
            }

            return result;
        }
    }
}
