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
            var fromPath = cmdArgs.Get("from");

            if (string.IsNullOrWhiteSpace(syncPointID) || string.IsNullOrWhiteSpace(fromPath))
            {
                throw new InvalidOperationException("No values provided for sync point and from- location");
            }

            if (!Path.Exists(fromPath))
            {
                throw new InvalidOperationException($"The provided path '{fromPath}' does not exist.");
            }

            if (!Path.Exists(Path.Combine(fromPath, "syncpointroot.json" ))) {
                throw new InvalidOperationException($"The provided path '{fromPath}' does not contain a valid sync point root file.");
            }

            SyncPointManager syncPointManager = new SyncPointManager(fromPath);

            SyncPoint? syncPoint = null;

            if (syncPointID == "latest")
            {
                if(syncPointManager.SyncPoints.Count == 0)
                {
                    throw new InvalidOperationException("There are no syncpoints in the remote location");
                }

                syncPoint = syncPointManager.SyncPoints.First();
            }
            else
            {
                syncPoint = syncPointManager.GetSyncPoint(syncPointID);
            }

            if(syncPoint == null)
            {
                throw new InvalidOperationException($"Sync point with ID '{syncPointID}' not found in the specified path.");
            }

            bool isZipped = syncPointManager.IsZipped;

            var allSyncPointFiles = syncPointManager.GetFileEntriesAtSyncpoint(syncPointID);
            var allFilesInRestoreLocation = Directory.GetFiles(syncPointManager.SyncPointRoot.SourceLocation, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase));

            foreach (var file in allSyncPointFiles)
            {
                var restorePath = file.SourcePath;

                if (isZipped)
                {
                    var zipFile = file.RelativeRemotePath.Split("@")[1];
                    var relativeEntry = file.RelativeRemotePath.Split("@")[0];

                    using var zip = new FileStream(zipFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
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

                    var existingFileInfo = new FileInfo(restorePath);

                    var srcLastWrite = File.GetLastWriteTimeUtc(restorePath);
                    var entryLastWrite = DateTime.SpecifyKind(zipEntry.LastWriteTime.DateTime, DateTimeKind.Utc);
                    srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
                    entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
                    var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

                    if(File.Exists(restorePath))
                    {
                        bool sameSize = existingFileInfo.Length == zipEntry.Length;
                        if(secondsDifference > 2 && sameSize)
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

                    if(File.Exists(restorePath))
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

            // after restoring files to the proper version, we also need to remove any files that were not part of the sync point
            foreach (var file in allFilesInRestoreLocation)
            {
                var foundFile = allSyncPointFiles.FirstOrDefault(f => f.SourcePath == file);
                if (foundFile == null)
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
        }
    }
}
