using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{
    public class SyncPointRestorer
    {
        public static void Run(CommandLineArguments cmdArgs)
        {
            var syncPointID = cmdArgs.Get("restore");
            var fromPath = string.Empty;

            bool hasManualFrom = cmdArgs.Has("from");
            if (hasManualFrom)
            {
                fromPath = cmdArgs.Get("from");
            }
            else
            {
                //we need to get the from path from the remote config file
                var currentDirectory = Environment.CurrentDirectory;
                var config = RemoteConfig.Load(currentDirectory);
                fromPath = config.RemotePath;
            }

            bool pathIsZipped = Path.HasExtension(fromPath) && Path.GetExtension(fromPath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
            string? fromDirectory = string.Empty;
            if (pathIsZipped)
            {
                fromDirectory = Path.GetDirectoryName(fromPath);
                if (string.IsNullOrWhiteSpace(fromDirectory))
                {
                    throw new InvalidOperationException("The provided path is a zip file, but no directory could be determined from it.");
                }
            }
            else
            {
                fromDirectory = fromPath;
            }

            if (string.IsNullOrWhiteSpace(fromDirectory) || string.IsNullOrWhiteSpace(fromPath))
            {
                throw new InvalidOperationException("The provided path is invalid or empty.");
            }

            if (!Path.Exists(fromDirectory) && !Path.Exists(fromPath))
            {
                throw new InvalidOperationException($"The provided path '{fromDirectory}' does not exist.");
            }

            if (!Path.Exists(Path.Combine(fromDirectory, "syncpointroot.json")))
            {
                throw new InvalidOperationException($"The provided path '{fromDirectory}' does not contain a valid sync point root file.");
            }

            SyncPointManager syncPointManager = new SyncPointManager(fromPath);

            SyncPoint? syncPoint = null;

            if (String.IsNullOrWhiteSpace(syncPointID) || syncPointID == "latest")
            {
                // if no sync point ID is provided, we will try to restore the latest sync point
                if (syncPointManager.SyncPoints.Count == 0)
                {
                    throw new InvalidOperationException("There are no syncpoints in the remote location");
                }

                syncPoint = syncPointManager.SyncPoints.First();
                syncPointID = syncPoint.SyncPointId;
            }
            else
            {
                syncPoint = syncPointManager.GetSyncPoint(syncPointID);
            }

            if (syncPoint == null)
            {
                throw new InvalidOperationException($"Sync point with ID '{syncPointID}' not found in the specified path.");
            }

            bool isZipped = syncPointManager.IsZipped;

            var allSyncPointFiles = syncPointManager.GetFileEntriesAtSyncpoint(syncPointID);
            var allFilesInRestoreLocation = Directory.GetFiles(syncPointManager.SyncPointRoot.SourceLocation, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase)
                        && !f.EndsWith("stn.remote.json", StringComparison.OrdinalIgnoreCase));


            if (cmdArgs.Has("file"))
            {
                SingleFileRestore(cmdArgs, syncPointID, fromDirectory, isZipped, allSyncPointFiles);
            }
            else
            {
                FullSyncPointRestore(syncPointID, fromPath, fromDirectory, isZipped, allSyncPointFiles, allFilesInRestoreLocation);
            }
        }

        private static bool SingleFileRestore(CommandLineArguments cmdArgs, string syncPointID, string fromDirectory, bool isZipped, List<SyncPointEntry> allSyncPointFiles)
        {
            var requestedFile = cmdArgs.Get("file");
            if (string.IsNullOrWhiteSpace(requestedFile))
            {
                throw new ArgumentException("The 'file' argument cannot be null or empty when restoring a specific file.");
            }

            //assume the provided file is relative to the sync point root and so create a full path to it
            var fullTargetPath = Path.Combine(Environment.CurrentDirectory, requestedFile);

            var fileToRestore = allSyncPointFiles.FirstOrDefault(f => f.SourcePath.Equals(fullTargetPath, StringComparison.OrdinalIgnoreCase));

            if (fileToRestore == null)
            {
                throw new InvalidOperationException($"The file '{requestedFile}' does not exist in the sync point '{syncPointID}'.");
            }

            if (fileToRestore.EntryType == SyncPointEntryType.Deleted)
            {
                //for single file restores, we don't delete the file and skip the operation. Users probably don't have the intent
                //to delete the file, but rather restore it to the state it was in at some sync point.
                Console.WriteLine($"The file '{requestedFile}' was marked as deleted in the sync point '{syncPointID}'. Nothing to restore.");
                return false;
            }

            if (isZipped)
            {
                var zipFile = fileToRestore.RelativeRemotePath.Split("@")[1];
                var fullZipPath = Path.Combine(fromDirectory, zipFile);
                var relativeEntry = fileToRestore.RelativeRemotePath.Split("@")[0];

                using var zip = new FileStream(fullZipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
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

                zipEntry.ExtractToFile(fullTargetPath, true);
                Console.WriteLine($"Restored single file '{requestedFile}' from sync point '{syncPointID}' to '{fullTargetPath}' from zip.");
            }
            else
            {
                var fullSyncPointPath = Path.Combine(fromDirectory, fileToRestore.RelativeRemotePath);

                if (!File.Exists(fullSyncPointPath))
                {
                    throw new InvalidOperationException($"The sync point file '{fullSyncPointPath}' does not exist.");
                }

                File.Copy(fullSyncPointPath, fullTargetPath, true);

                Console.WriteLine($"Restored single file '{requestedFile}' from sync point '{syncPointID}' to '{fullTargetPath}'.");
            }

            return true;
        }

        private static void FullSyncPointRestore(string syncPointID, string fromPath, string fromDirectory, bool isZipped, List<SyncPointEntry> allSyncPointFiles, IEnumerable<string> allFilesInRestoreLocation)
        {
            foreach (var file in allSyncPointFiles)
            {
                var restorePath = file.SourcePath;

                if (isZipped)
                {
                    var zipFile = file.RelativeRemotePath.Split("@")[1];
                    var fullZipPath = Path.Combine(fromDirectory, zipFile);
                    var relativeEntry = file.RelativeRemotePath.Split("@")[0];

                    using var zip = new FileStream(fullZipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                    using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                    var entryPath = relativeEntry.Replace("\\", "/");
                    var zipEntry = archive.GetEntry(entryPath);

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
                            Console.WriteLine($"File marked as deleted in sync point, removing: {restorePath}");
                            File.Delete(restorePath);
                            continue;
                        }
                    }

                    if (File.Exists(restorePath))
                    {
                        var existingFileInfo = new FileInfo(restorePath);

                        var srcLastWrite = File.GetLastWriteTimeUtc(restorePath);
                        var entryLastWrite = zipEntry.LastWriteTime.UtcDateTime;
                        srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

                        bool sameSize = existingFileInfo.Length == zipEntry.Length;
                        if (secondsDifference < 2 && sameSize)
                        {
                            continue;
                        }
                    }

                    Console.WriteLine($"Restoring file from zip: {restorePath}");
                    zipEntry.ExtractToFile(restorePath, true);
                }
                else
                {
                    restorePath = Path.Combine(fromPath, file.RelativeRemotePath);
                    var syncPointPath = Path.Combine(fromPath, syncPointID, file.RelativeRemotePath);

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

                    Console.WriteLine($"Restoring file: {restorePath}");
                    File.Copy(syncPointPath, restorePath, true);
                }
            }

            Console.WriteLine("Cleaning up files not part of the sync point...");

            // after restoring files to the proper version, we also need to remove any files that were not part of the sync point
            foreach (var file in allFilesInRestoreLocation)
            {
                var foundFile = allSyncPointFiles.FirstOrDefault(f => f.SourcePath == file);
                if (foundFile == null && !Path.GetFileName(file).Equals("stn.remote.json", StringComparison.OrdinalIgnoreCase))
                {
                    // this file was not part of the sync point, so we can remove it
                    try
                    {
                        Console.WriteLine($"Removing file not part of sync point: {file}");
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error removing file '{file}': {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"Restoration of sync point '{syncPointID}' completed successfully.");
        }
    }
}
