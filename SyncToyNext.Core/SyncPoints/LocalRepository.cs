using SyncToyNext.Core.Helpers;
using SyncToyNext.Core.Runners;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.SyncPoints
{
    public class LocalRepository
    {
        private SyncPointManager _manager;
        private RemoteConfig? _remoteConfig;

        public string LocalPath { get; set; }
        public string RemotePath { get; set; }

        public IReadOnlyList<SyncPoint> SyncPoints
        {
            get
            {
                if (_manager == null)
                {
                    throw new InvalidOperationException("Manager not loaded for repository.");
                }

                return _manager.SyncPoints;
            }
        }

        public SyncPoint? LatestSyncPoint
        {
            get
            {
                if (_manager == null)
                {
                    throw new InvalidOperationException("Manager not loaded for repository.");
                }

                return _manager.SyncPoints.OrderByDescending(x => x.LastSyncTime).FirstOrDefault();
            }
        }

        public RemoteConfig? Config
        {
            get
            {
                return _remoteConfig;
            }
        }

        public string TempPath
        {
            get
            {
                var stnTempFolder = Path.Combine(LocalPath, ".stn", "temp");
                if (!Path.Exists(stnTempFolder)) Directory.CreateDirectory(stnTempFolder);
                return stnTempFolder;
            }
        }

        public LocalRepository(string localPath)
        {
            if(String.IsNullOrWhiteSpace(localPath) || !Path.Exists(localPath) || !RemoteConfig.RemoteConfigExists(localPath))
            {
                throw new InvalidOperationException("When only specifying a local path, the path must exist and a remote config must be present.");
            }

            LocalPath = localPath;

            _remoteConfig = RemoteConfig.Load(localPath);
            
            if(_remoteConfig == null)
            {
                throw new InvalidOperationException("Config should not be null");
            }

            RemotePath = _remoteConfig.RemotePath;

            _manager = new SyncPointManager(RemotePath, LocalPath);
        }

        protected void RestoreLatestToLocal()
        {
            if(_manager!= null)
            {
                if(_manager.SyncPoints.Count > 0)
                {
                    var latestSyncPoint = LatestSyncPoint?.SyncPointId ?? String.Empty;
                    if(!String.IsNullOrWhiteSpace(latestSyncPoint))
                    {
                        if (_manager.GetFileEntriesAtSyncpoint(latestSyncPoint).Count > 0)
                        {
                            Restore(latestSyncPoint);
                        }
                    }
                }
            }
        }

        public static LocalRepository Initialize(string localPath, string remotePath)
        {
            if (!Path.Exists(localPath))
            {
                throw new InvalidOperationException("Local path doesn't exist.");
            }

            if (!Path.Exists(remotePath))
            {
                throw new InvalidOperationException("Remote path doesn't exist.");
            }

            var config = new RemoteConfig(remotePath, localPath);
            config.Save(localPath);

            var repository = new LocalRepository(localPath);

            repository.RestoreLatestToLocal();

            return repository;
        }

        public void ChangeRemote(string newRemotePath)
        {
            if(String.IsNullOrWhiteSpace(newRemotePath) || !Path.Exists(newRemotePath))
            {
                throw new InvalidOperationException("New remote path is not valid.");
            }

            if (_remoteConfig == null) throw new InvalidOperationException("Remote config is not loaded for this repository.");

            _remoteConfig.RemotePath = newRemotePath;
            _remoteConfig.Save(LocalPath);

            _manager = new SyncPointManager(newRemotePath, LocalPath);
            RestoreLatestToLocal();
        }

        public IEnumerable<string> GetLocalFiles()
        {
            return FileHelpers.GetFilesInPath(LocalPath);
        }

        public IEnumerable<string> GetRemoteFiles(string syncpointId)
        {
            return (IEnumerable<string>)_manager.GetFileEntriesAtSyncpoint(syncpointId);
        }

        public void Restore(string syncPointID)
        {
            SyncPointRestorer.RestorePath = LocalPath;
            SyncPointRestorer.Run(syncPointID, RemotePath);

            if (_remoteConfig == null) throw new InvalidOperationException("Trying to work with remote config which is null.");
            _remoteConfig.CurrentSyncPoint = syncPointID;
            _remoteConfig.Save(LocalPath);
        }

        public void RestoreSingleFile(string syncPointID, string relativeFilePath)
        {
            SyncPointRestorer.RestorePath = LocalPath;
            SyncPointRestorer.Run(syncPointID, RemotePath, relativeFilePath);
        }

        public void RestoreMultipleEntriesFromSyncPoint(IEnumerable<SyncPointEntry> remoteSelectedItems, SyncPoint? currentSyncPoint)
        {
            SyncPointRestorer.RestorePath = LocalPath;

            bool isZipped = Path.HasExtension(RemotePath) && Path.GetExtension(RemotePath) == ".zip";

            if (remoteSelectedItems.Count() > 0 && currentSyncPoint != null)
            {
                foreach (var entry in remoteSelectedItems)
                {
                    if (entry != null)
                    {
                        var entryParts = entry.RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                        var relativeEntryPath = entryParts[0];

                        SyncPointRestorer.Run(currentSyncPoint.SyncPointId, string.Empty, relativeEntryPath);
                    }
                }
            }
        }

        public string GetTempCopyOfFile(SyncPointEntry selectedItem)
        {
            var pathParts = selectedItem.RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            var relativePath = pathParts[0];

            if (pathParts.Length > 0)
            {
                var remoteFolderPath = Path.GetDirectoryName(RemotePath);

                if (remoteFolderPath == null) throw new InvalidOperationException("Remote folder path could not be retrieved");

                var zipPath = Path.Combine(remoteFolderPath, pathParts[1]);
                using var zip = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                var entryPath = relativePath.Replace("\\", "/");
                var zipEntry = archive.GetEntry(entryPath);

                if (string.IsNullOrWhiteSpace(LocalPath)) throw new InvalidOperationException("Local path should be loaded");

                var tempPath = Path.Combine(TempPath, Path.GetFileName(relativePath));

                if (zipEntry != null)
                {
                    zipEntry.ExtractToFile(tempPath, true);
                    return tempPath;
                }
            }
            else
            {
                if (RemotePath == null)
                {
                    throw new InvalidOperationException("Remote folder path is not set.");
                }

                var fullRemotePath = Path.Combine(RemotePath, relativePath);
                var tempPath = Path.Combine(TempPath, Path.GetFileName(relativePath));
                File.Copy(fullRemotePath, tempPath);
                return fullRemotePath;
            }

            return string.Empty;
        }

        public void Push()
        {
            ManualRunner.Run(LocalPath, RemotePath, true);
            _manager.RefreshSyncPoints();
        }

        public void Push(string newSyncPointID, string newDescription)
        {
            ManualRunner.Run(LocalPath, RemotePath, true, newSyncPointID, newDescription);
            _manager.RefreshSyncPoints();
        }

        public void Merge(string otherLocalTarget)
        {
            SyncPointMerger.Merge(LocalPath, otherLocalTarget, Merging.TwoWayMergePolicy.Union);
        }

        
    }
}
