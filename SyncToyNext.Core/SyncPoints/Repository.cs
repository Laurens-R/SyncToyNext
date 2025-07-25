using SyncToyNext.Core.Helpers;
using SyncToyNext.Core.Runners;
using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.SyncPoints
{
    public class Repository
    {
        private SyncPointManager _manager;
        private RemoteConfig? _remoteConfig;

        public string LocalPath { get; set; }
        public string RemotePath { get; set; }

        public SyncPointManager Manager => _manager;
        public RemoteConfig? Config => _remoteConfig;

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

        public string LocalSyncPointID
        {
            get
            {
                if (_remoteConfig == null) throw new InvalidOperationException("Remote config cannot be null when repository is opened.");
                return _remoteConfig.CurrentSyncPoint;
            }
        }

        public SyncPoint? LatestReferenceSyncPoint
        {
            get
            {
                if(_manager == null)
                {
                    throw new InvalidOperationException("Manager not loaded for repository.");
                }

                return _manager.SyncPoints.OrderByDescending(x => x.LastSyncTime).FirstOrDefault(s => s.ReferencePoint == true);
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

        public Repository(string localPath)
        {
            if (String.IsNullOrWhiteSpace(localPath) || !Path.Exists(localPath) || !RemoteConfig.RemoteConfigExists(localPath))
            {
                throw new InvalidOperationException("When only specifying a local path, the path must exist and a remote config must be present.");
            }

            //we try to load the configuration from the current directory. But we also want to account for the fact
            //that the user might be working somewhere in a subfolder of the repo, so we will recursively inspect
            //parent folders for the config if we cannot directly load the RemoteConfig from the current path;
            var currentDirectory = localPath;
            _remoteConfig = RemoteConfig.Load(currentDirectory);
            
            while (_remoteConfig == null)
            {
                var parentDirectoryInfo = Directory.GetParent(currentDirectory);
                if (parentDirectoryInfo == null)
                {
                    throw new InvalidOperationException("Could not find repo root in the path hierarchy. Has a repo been initialized in this location?");
                }

                currentDirectory = parentDirectoryInfo.FullName;
                _remoteConfig = RemoteConfig.Load(currentDirectory);
            }
                        
            LocalPath = currentDirectory;
            RemotePath = _remoteConfig.RemotePath;

            _manager = new SyncPointManager(RemotePath);
        }

        /// <summary>
        /// Initializes the content of a local/remote pair. If the remote already has syncpoints
        /// it is leading: the latest syncpoint gets restored into the local removing all local
        /// files which are already present. Otherwise we could get potentially in a state where
        /// we could corrupt the remote if it is not in sync.
        /// 
        /// If the remote doesn't contain syncpoints but the local folder does contain files already
        /// we push it to create an initial syncpoint.
        /// </summary>
        protected void InitializeContent()
        {
            if(_manager!= null)
            {
                if(_manager.SyncPoints.Count > 0)
                {
                    //remote syncpoints are always leading. You should never end up in a state
                    //where you corrupt a remote.
                    var latestSyncPoint = LatestSyncPoint?.SyncPointId ?? String.Empty;
                    if(!String.IsNullOrWhiteSpace(latestSyncPoint))
                    {
                        if (_manager.GetFileEntriesAtSyncpoint(latestSyncPoint).Count() > 0)
                        {
                            Restore(latestSyncPoint);
                        }
                    }
                } else if(GetLocalFiles().Count() > 0)
                {
                    //if no remote syncpoints exist but there are already files present in the
                    //local location, push a reference syncpoint.
                    Push("INIT", "Init of a new local/remote pair", true);
                }
            }
        }

        public static bool RemoteExists(string remotePath)
        {
            if(Path.HasExtension(remotePath) && Path.GetExtension(remotePath) == ".zip") 
            {
                var parentDirectory = Path.GetDirectoryName(remotePath);
                return Directory.Exists(parentDirectory);
            } 
            else
            {
                return Directory.Exists(remotePath);
            }
        }        

        /// <summary>
        /// Initializes a new local/remote pair.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="remotePath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Repository Initialize(string localPath, string remotePath, bool compressedRemote)
        {
            if (!Directory.Exists(localPath))
            {
                throw new InvalidOperationException("Local path doesn't exist.");
            }

            if (!Directory.Exists(remotePath))
            {
                throw new InvalidOperationException("Remote path doesn't exist.");
            }

            var config = new RemoteConfig(remotePath, localPath);
            config.Save(localPath);

            Repository repository = new Repository(localPath);

            if (repository._manager.SyncPointRoot == null)
            {
                //NOTE: Yes this is hacky/wrong because I'm basically using Exceptions to deal with application logic. This can probably be done
                //better/reworked at a later point but I'm running out of bright ideas. This situation basically occurs if a local is initialized
                //and it's remote pair isn't yet. (e.g. missing a syncroot file with the remote repository config).
                repository._manager = new SyncPointManager(repository.RemotePath, compressedRemote ? CompressionMode.Compressed : CompressionMode.Uncompressed);
            }

            repository.InitializeContent();

            return repository;
        }

        /// <summary>
        /// Creates a new local/remote pair by cloning a different remote.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="newRemotePath"></param>
        /// <param name="otherRemotePath"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Repository CloneFromOtherRemote(string localPath, string newRemotePath, string otherRemotePath, bool isCompressed)
        {
            if (!Directory.Exists(localPath))
            {
                throw new InvalidOperationException("Local path doesn't exist.");
            }

            if (!Directory.Exists(newRemotePath))
            {
                throw new InvalidOperationException("Remote path doesn't exist.");
            }

            if (!Directory.Exists(otherRemotePath))
            {
                throw new InvalidOperationException("Other remote path doesn't exist.");
            }

            var config = new RemoteConfig(newRemotePath, localPath);
            config.Save(localPath);

            var repository = Repository.Initialize(localPath, newRemotePath, isCompressed);
            var otherRemoteManager = new SyncPointManager(otherRemotePath);
            var latestSyncPoint = otherRemoteManager.SyncPoints.Count > 0 ? otherRemoteManager.SyncPoints[0] : null;

            if (latestSyncPoint != null)
            {
                SyncPointRestorer.RestorePath = repository.LocalPath;
                SyncPointRestorer.Run(latestSyncPoint.SyncPointId, otherRemotePath);

                //we use exactly the same syncpoint id as the latest syncpoint id from the remote which was cloned
                //so we can refer back to it when merging.
                repository.Push(latestSyncPoint.SyncPointId, $"Init push after clone from {otherRemotePath}", true);
            } 
            else
            {
                UserIO.Error("A clone from another remote requires that remote to have at least one syncpoint.");
            }

            return repository;
        }

        public void ChangeRemote(string newRemotePath, bool isCompressed)
        {
            if(String.IsNullOrWhiteSpace(newRemotePath) || !Path.Exists(newRemotePath))
            {
                throw new InvalidOperationException("New remote path is not valid.");
            }

            if (_remoteConfig == null) throw new InvalidOperationException("Remote config is not loaded for this repository.");

            _remoteConfig.RemotePath = newRemotePath;
            _remoteConfig.Save(LocalPath);

            _manager = new SyncPointManager(newRemotePath, isCompressed ? CompressionMode.Compressed : CompressionMode.Uncompressed);
            InitializeContent();
        }

        public bool HasSyncPointID(string syncPointID)
        {
            return SyncPoints.Any(s => s.SyncPointId == syncPointID);
        }

        public IEnumerable<string> GetLocalFiles()
        {
            return FileHelpers.GetFilesInPath(LocalPath);
        }

        public IEnumerable<SyncPointEntry> GetRemoteFiles(string syncpointId)
        {
            return _manager.GetFileEntriesAtSyncpoint(syncpointId);
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

        /// <summary>
        /// Gets a temporary local copy of a remote file.
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <returns>The path to the temporary location of the file.</returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        public string ReadAllTextRemote(string relativePath, string syncPointID)
        {
            var filesAtSyncPoint = _manager.GetFileEntriesAtSyncpoint(syncPointID);
            var fileEntry = filesAtSyncPoint.FirstOrDefault(f => f.SourcePath == relativePath);

            if (fileEntry == null) return string.Empty;

            var remotePathParts = fileEntry.SourcePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

            if(remotePathParts.Length == 2)
            {
                //file is a zip.
                var zipPath = Path.Combine(RemotePath, remotePathParts[1]);
                using var zip = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                var entryPath = relativePath.Replace("\\", "/");
                var zipEntry = archive.GetEntry(entryPath);

                if(zipEntry == null) return string.Empty;

                using var entryStream = zipEntry.Open();
                using var reader = new StreamReader(entryStream);
                var remoteTextContent = reader.ReadToEnd();
                return remoteTextContent;
            }
            else
            {
                //file is not a zip.
                var fullRemotePath = Path.Combine(RemotePath, relativePath);
                if (File.Exists(fullRemotePath))
                {
                    return File.ReadAllText(fullRemotePath);
                } else
                {
                    return String.Empty;
                }
                
            }
        }

        public void Push()
        {
            //fyi: forwarding empty values will cause the actual manualrunner to generate id's itself.
            Push(string.Empty, string.Empty, false);
        }

        public void Push(string newSyncPointID, string newDescription, bool isReferencePoint = false)
        {
            ManualRunner.Run(LocalPath, RemotePath, true, newSyncPointID, newDescription, isReferencePoint);
            _manager.RefreshSyncPoints();

            if (_remoteConfig == null || LatestSyncPoint == null) return;
            _remoteConfig.CurrentSyncPoint = LatestSyncPoint.SyncPointId;
            _remoteConfig.Save(LocalPath);
        }
    }
}
