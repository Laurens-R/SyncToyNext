using ICSharpCode.SharpZipLib.Core;
using Stn.Core.IO;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZipLib = ICSharpCode.SharpZipLib.Zip;

namespace Stn.Core.SyncPoints
{
    public class SyncPointRestorer
    {
        public static string RestorePath = Environment.CurrentDirectory;
        public static string RemotePath = string.Empty;
        public static string RemoteDirectory = string.Empty;
        public static Action<int, int, string>? UpdateProgressHandler = null;

        public static void EstablishRestorePath()
        {
            var currentPath = RestorePath;
            RemoteConfig? config = RemoteConfig.Load(currentPath);

            while (config == null)
            {
                //get parent directory and check if it contains a remote config file
                var parentPath = Directory.GetParent(currentPath);
                if (parentPath == null)
                {
                    UserIO.Message($"Could not find remote configuration in path chain. Assuming {Environment.CurrentDirectory}");
                    RestorePath = Environment.CurrentDirectory;
                    return;
                }

                currentPath = parentPath.FullName;
                config = RemoteConfig.Load(currentPath);
            }

            RestorePath = currentPath;
        }

        public static void Run(string syncpointId, string remotePath = "", string singleFile = "")
        {
            bool hasManualRemotePath = !string.IsNullOrWhiteSpace(remotePath);

            EstablishRestorePath();

            if (hasManualRemotePath)
            {
                RemotePath = remotePath;

                if(string.IsNullOrEmpty(RemotePath))
                {
                    throw new ArgumentException("The 'from' argument must provide a path value.");
                }
            }
            else
            {
                //we need to get the from path from the remote config file
                var config = RemoteConfig.Load(RestorePath);
                if (config == null)
                {
                    throw new InvalidOperationException("No remote configuration found in the current directory or its parents.");
                }
                RemotePath = config.RemotePath;
            }

            RemoteDirectory = RemotePath;
            
            if (string.IsNullOrWhiteSpace(RemoteDirectory) || string.IsNullOrWhiteSpace(RemotePath))
            {
                throw new InvalidOperationException("The provided path is invalid or empty.");
            }

            if (!Path.Exists(RemoteDirectory) && !Path.Exists(RemotePath))
            {
                throw new InvalidOperationException($"The provided path '{RemoteDirectory}' does not exist.");
            }

            if (!Path.Exists(Path.Combine(RemoteDirectory, "syncpointroot.json")))
            {
                throw new InvalidOperationException($"The provided path '{RemoteDirectory}' does not contain a valid sync point root file.");
            }

            SyncPointManager syncPointManager = new SyncPointManager(RemotePath);

            //just making sure the sync points are refreshed before we start restoring
            if (!syncPointManager.RefreshSyncPoints())
            {
                throw new InvalidOperationException("Failed to refresh sync points. Please check the remote path and try again.");
            }

            SyncPoint? syncPoint = null;

            if (string.IsNullOrWhiteSpace(syncpointId) || syncpointId == "latest")
            {
                // if no sync point ID is provided, we will try to restore the latest sync point
                if (syncPointManager.SyncPoints.Count == 0)
                {
                    throw new InvalidOperationException("There are no syncpoints in the remote location");
                }

                syncPoint = syncPointManager.SyncPoints.First();
                syncpointId = syncPoint.SyncPointId;
            }
            else
            {
                syncPoint = syncPointManager.GetSyncPoint(syncpointId);
            }

            if (syncPoint == null)
            {
                throw new InvalidOperationException($"Sync point with ID '{syncpointId}' not found in the specified path.");
            }

            bool isZipped = syncPointManager.IsZipped;

            var allSyncPointFiles = syncPointManager.GetFileEntriesAtSyncpoint(syncpointId);
            var allFilesInRestoreLocation = Directory.GetFiles(RestorePath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.stn{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(Path.DirectorySeparatorChar).EndsWith($"{Path.DirectorySeparatorChar}.stn", StringComparison.OrdinalIgnoreCase));

            var ignoreFile = new IgnoreFile();
            ignoreFile.TryLoadIgnoreFile(RestorePath);

            allFilesInRestoreLocation = allFilesInRestoreLocation.Where(x => ignoreFile.IsEntryIgnored(x));

            if (!string.IsNullOrWhiteSpace(singleFile))
            {
                SingleFileRestore(singleFile, syncpointId, isZipped, allSyncPointFiles);
            }
            else
            {
                FullSyncPointRestore(syncpointId, isZipped, allSyncPointFiles, allFilesInRestoreLocation);
            }
        }

        private static bool SingleFileRestore(string requestedFile, string syncPointID,  bool isZipped, List<SyncPointEntry> allSyncPointFiles)
        {
            if (string.IsNullOrWhiteSpace(requestedFile))
            {
                throw new ArgumentException("The 'file' argument cannot be null or empty when restoring a specific file.");
            }

            //assume the provided file is relative to the sync point root and so create a full path to it
            var fullTargetPath = Path.Combine(RestorePath, requestedFile);

            var fileToRestore = allSyncPointFiles.FirstOrDefault(f => f.SourcePath.Equals(requestedFile, StringComparison.OrdinalIgnoreCase));

            if (fileToRestore == null)
            {
                throw new InvalidOperationException($"The file '{requestedFile}' does not exist in the sync point '{syncPointID}'.");
            }

            if (fileToRestore.EntryType == SyncPointEntryType.Deleted)
            {
                //for single file restores, we don't delete the file and skip the operation. Users probably don't have the intent
                //to delete the file, but rather restore it to the state it was in at some sync point.
                UserIO.Message($"The file '{requestedFile}' was marked as deleted in the sync point '{syncPointID}'. Nothing to restore.");
                return false;
            }

            if (isZipped)
            {
                var zipFile = fileToRestore.RelativeRemotePath.Split("@")[1];
                var fullZipPath = Path.Combine(RemoteDirectory, zipFile);
                var relativeEntry = fileToRestore.RelativeRemotePath.Split("@")[0];

                using var zip = new FileStream(fullZipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                using var archive = new ZipLib.ZipFile(zip, false);
                var entryPath = relativeEntry.Replace("\\", "/");
                var zipEntry = archive.GetEntry(entryPath);

                if (zipEntry == null)
                {
                    throw new InvalidOperationException($"The zip entry '{entryPath}' does not exist in the zip file '{zipFile}'.");
                }

                var restoreDirectory = Path.GetDirectoryName(fullTargetPath);

                if (restoreDirectory != null && !Directory.Exists(restoreDirectory))
                {
                    Directory.CreateDirectory(restoreDirectory);
                }

                FileHelpers.WriteZipEntryToDisk(fullTargetPath, archive, zipEntry);        

                UserIO.Message($"Restored single file '{requestedFile}' from sync point '{syncPointID}' to '{fullTargetPath}' from zip.");
            }
            else
            {
                var fullSyncPointPath = Path.Combine(RemoteDirectory, syncPointID, fileToRestore.RelativeRemotePath);

                if (!File.Exists(fullSyncPointPath))
                {
                    throw new InvalidOperationException($"The sync point file '{fullSyncPointPath}' does not exist.");
                }

                File.Copy(fullSyncPointPath, fullTargetPath, true);

                UserIO.Message($"Restored single file '{requestedFile}' from sync point '{syncPointID}' to '{fullTargetPath}'.");
            }

            return true;
        }

        private static void FullSyncPointRestore(string syncPointID, bool isZipped, List<SyncPointEntry> allSyncPointFiles, IEnumerable<string> allFilesInRestoreLocation)
        {
           var sortedByZipFile = allSyncPointFiles.AsParallel().OrderBy(f => { 
               var parts = f.RelativeRemotePath.Split("@", StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 1 ? parts[1] : parts[0];
           });
                
            var currentZipFile = string.Empty;
            FileStream? currentZipStream = null;
            ZipLib.ZipFile? currentZipArchive = null;

            int totalFiles = allSyncPointFiles.Count;
            int currentFileIndex = 0;

            foreach (var file in allSyncPointFiles)
            {
                currentFileIndex++;
                var restorePath = Path.Combine(RestorePath, file.SourcePath);

                Repository.UpdateProgressHandler?.Invoke(currentFileIndex, totalFiles, $"Restoring {file.SourcePath}");

                if (isZipped)
                {
                    var zipFile = file.RelativeRemotePath.Split("@")[1];
                    var fullZipPath = Path.Combine(RemoteDirectory, zipFile);
                    var relativeEntry = file.RelativeRemotePath.Split("@")[0];

                    if(currentZipFile != zipFile)
                    {
                        //we need to open a new zip file
                        currentZipFile = zipFile;

                        currentZipArchive?.Close();

                        currentZipStream = new FileStream(fullZipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                        currentZipArchive = new ZipLib.ZipFile(currentZipStream, false);
                    }
                    
                    if(currentZipArchive != null) {
                        var entryPath = relativeEntry.Replace("\\", "/");
                        var zipEntry = currentZipArchive.GetEntry(entryPath);

                        var restoreDirectory = Path.GetDirectoryName(restorePath);

                        if (restoreDirectory != null && !Directory.Exists(restoreDirectory))
                        {
                            Directory.CreateDirectory(restoreDirectory);
                        }

                        if (zipEntry == null)
                        {
                            throw new InvalidOperationException($"The zip entry '{entryPath}' does not exist in the zip file '{zipFile}'.");
                        }

                        if (file.EntryType == SyncPointEntryType.Deleted)
                        {
                            if (File.Exists(restorePath))
                            {
                                File.Delete(restorePath);
                                continue;
                            }
                        }

                        if (File.Exists(restorePath))
                        {
                            var existingFileInfo = new FileInfo(restorePath);

                            var srcLastWrite = File.GetLastWriteTimeUtc(restorePath);
                            var entryLastWrite = zipEntry.DateTime;
                            srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
                            entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
                            var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

                            bool sameSize = existingFileInfo.Length == zipEntry.Size;
                            if (secondsDifference < 2 && sameSize)
                            {
                                continue;
                            }
                        }

                        FileHelpers.WriteZipEntryToDisk(restorePath, currentZipArchive, zipEntry);
                    }
                }
                else
                {
                    var syncPointPath = Path.Combine(RemotePath, file.SyncpointID, file.RelativeRemotePath);

                    if (!File.Exists(syncPointPath))
                    {
                        throw new InvalidOperationException($"The remote sync point file '{syncPointPath}' does not exist.");
                    }

                    var restoreDirectory = Path.GetDirectoryName(restorePath);

                    if (restoreDirectory != null && !Directory.Exists(restoreDirectory))
                    {
                        Directory.CreateDirectory(restoreDirectory);
                    }

                    if (File.Exists(restorePath))
                    {
                        //compare dates between the existing file and the sync point file
                        var existingFileInfo = new FileInfo(restorePath);
                        var syncPointFileInfo = new FileInfo(syncPointPath);
                        bool sameSize = existingFileInfo.Length == syncPointFileInfo.Length;

                        if (existingFileInfo.LastAccessTimeUtc == syncPointFileInfo.LastWriteTimeUtc && sameSize)
                        {
                            continue;
                        }
                    }

                    File.Copy(syncPointPath, restorePath, true);
                }
            }

            currentZipArchive?.Close();

            UserIO.Message("Cleaning up files not part of the sync point...");

            int progressCounter = 0;
            totalFiles = allFilesInRestoreLocation.Count();

            // after restoring files to the proper version, we also need to remove any files that were not part of the sync point
            foreach (var file in allFilesInRestoreLocation)
            {
                progressCounter++;
                Repository.UpdateProgressHandler?.Invoke(progressCounter, totalFiles, "Doing final restore checks...");

                var relativeRestorePath = Path.GetRelativePath(RestorePath, file);
                var foundFile = allSyncPointFiles.AsParallel().Any(f => f.SourcePath == relativeRestorePath);
                if (!foundFile)
                {
                    // this file was not part of the sync point, so we can remove it
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        UserIO.Message($"Error removing file '{file}': {ex.Message}");
                    }
                }
            }

            UserIO.Message($"Restoration of sync point '{syncPointID}' completed successfully.");
        }
    }
}
