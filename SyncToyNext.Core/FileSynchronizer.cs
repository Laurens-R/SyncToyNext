using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Provides file and directory synchronization between two paths with configurable overwrite behavior.
    /// </summary>
    public class FileSynchronizer : ISynchronizer
    {
        private readonly string _destination;
        private readonly OverwriteOption _overwriteOption;
        private readonly Logger _logger;
        private readonly bool _strictMode;

        public FileSynchronizer(string destination, OverwriteOption overwriteOption, Logger logger, bool strictMode = false)
        {
            _destination = destination;
            _overwriteOption = overwriteOption;
            _logger = logger;
            _strictMode = strictMode;
        }

        /// <summary>
        /// Synchronizes all files and subdirectories from the source path to the destination directory.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        public void FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            if (!Directory.Exists(_destination))
                Directory.CreateDirectory(_destination);

            var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(f).Equals("stn.remote.json", StringComparison.OrdinalIgnoreCase));

            if (syncPoint != null && syncPointManager != null)
            {
                ProcessSyncPoint(sourcePath, syncPoint, syncPointManager, allFiles);
            }
            else
            {
                ProcessStraightSync(sourcePath, allFiles);
            }
        }

        private void ProcessStraightSync(string sourcePath, IEnumerable<string> allFiles)
        {
            foreach (var srcFilePath in allFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);

                var destFilePath = Path.Combine(_destination, relativePath);

                SynchronizeFile(srcFilePath, destFilePath);
            }
        }

        private void ProcessSyncPoint(string sourcePath, SyncPoint syncPoint, SyncPointManager syncPointManager, IEnumerable<string> allFiles)
        {
            var allFilesPartOfSyncPoint = syncPointManager.GetFileEntriesAtSyncpoint(syncPoint.SyncPointId);

            foreach (var srcFilePath in allFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                var existingEntry = allFilesPartOfSyncPoint.FirstOrDefault(e => e.SourcePath.Equals(srcFilePath, StringComparison.OrdinalIgnoreCase));

                // If the sync point entry exists, check if the file is newer than the existing sync point
                if (existingEntry != null)
                {
                    var syncPointPath = Path.Combine(_destination, existingEntry.RelativeRemotePath);

                    var sourceFileDateTime = File.GetLastWriteTimeUtc(srcFilePath);
                    var targetFileDateTime = File.GetLastWriteTimeUtc(syncPointPath);

                    //check if the sync point is old compared to the source file.
                    if (File.Exists(syncPointPath) && (sourceFileDateTime > targetFileDateTime))
                    {
                        // replicate file into the new sync point location
                        var destFilePath = Path.Combine(Path.Combine(_destination, syncPoint.SyncPointId), relativePath);
                        var destEntryPath = Path.Combine(syncPoint.SyncPointId, relativePath);
                        syncPoint.AddEntry(srcFilePath, destEntryPath);
                        SynchronizeFile(srcFilePath, destFilePath);
                        continue;
                    }
                }
                else
                {
                    // replicate file into the new sync point location
                    var destFilePath = Path.Combine(Path.Combine(_destination, syncPoint.SyncPointId), relativePath);
                    var destEntryPath = Path.Combine(syncPoint.SyncPointId, relativePath);
                    syncPoint.AddEntry(srcFilePath, destEntryPath);
                    SynchronizeFile(srcFilePath, destFilePath);
                }
            }

            //now save the sync point
            syncPoint.Save(Path.Combine(_destination, syncPoint.SyncPointId, syncPoint.SyncPointId + ".syncpoint.json"));
        }

        /// <summary>
        /// Synchronizes a single file from the source to the destination, using the specified overwrite option.
        /// If oldDestFilePath is provided, deletes the old file (for renames).
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="destFilePath">The full path to the destination file.</param>
        /// <param name="oldDestFilePath">The old destination file path to delete (optional, for renames).</param>
        public void SynchronizeFile(string srcFilePath, string destFilePath, string? oldDestFilePath = null)
        {
            if (!string.IsNullOrEmpty(oldDestFilePath) && File.Exists(oldDestFilePath))
            {
                try { File.Delete(oldDestFilePath); } catch { /* ignore */ }
            }

            bool fileExists = File.Exists(destFilePath);
            bool shouldCopy = false;
            string action = "None";

            if (_overwriteOption == OverwriteOption.AlwaysOverwrite)
            {
                shouldCopy = true;
                action = fileExists ? "Overwrite" : "Create";
            }
            else if (!fileExists)
            {
                shouldCopy = true;
                action = "Create";
            }
            else
            {
                var srcLastWrite = File.GetLastWriteTimeUtc(srcFilePath);
                var destLastWrite = File.GetLastWriteTimeUtc(destFilePath);
                if (srcLastWrite > destLastWrite)
                {
                    shouldCopy = true;
                    action = "Update";
                }
                else
                {
                    var srcSize = new FileInfo(srcFilePath).Length;
                    var destSize = new FileInfo(destFilePath).Length;
                    if (srcSize != destSize)
                    {
                        shouldCopy = true;
                        action = "RepairSizeMismatch";
                    }
                    else if (_strictMode)
                    {
                        var srcHash = ComputeSHA256(srcFilePath);
                        var destHash = ComputeSHA256(destFilePath);
                        if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldCopy = true;
                            action = "RepairChecksumMismatch";
                        }
                    }
                }
            }

            try
            {
                if (shouldCopy)
                {
                    //first ensure the target directory exists
                    var destDir = Path.GetDirectoryName(destFilePath);
                    if (destDir != null && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    const int maxRetries = 10;
                    const int delayMs = 1000;
                    int attempt = 0;

                    while (true)
                    {
                        try
                        {
                            File.Copy(srcFilePath, destFilePath, true);
                            _logger.LogSyncAction(destFilePath, action);
                            break;
                        }
                        catch (IOException)
                        {
                            _logger.LogError($"File locked when trying to sync from '{srcFilePath}' to '{destFilePath}'. Retrying {attempt + 1}/{maxRetries}...");
                            attempt++;
                            if (attempt >= maxRetries) throw new IOException($"Failed to copy file after {maxRetries} attempts.");
                            Thread.Sleep(delayMs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to sync file '{srcFilePath}' to '{destFilePath}'", ex);
            }
        }

        private static string ComputeSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
