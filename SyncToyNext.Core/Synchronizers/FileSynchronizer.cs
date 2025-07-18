using SyncToyNext.Core.Models;
using SyncToyNext.Core.UX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SyncToyNext.Core.Synchronizers
{
    /// <summary>
    /// Provides file and directory synchronization between two paths with configurable overwrite behavior.
    /// </summary>
    public class FileSynchronizer : Synchronizer
    {
        private readonly string _destination;
        private readonly OverwriteOption _overwriteOption;
        private readonly bool _strictMode;

        public FileSynchronizer(string destination, OverwriteOption overwriteOption, bool strictMode = false)
        {
            _destination = destination;
            _overwriteOption = overwriteOption;
            _strictMode = strictMode;
        }

        /// <summary>
        /// Synchronizes all files and subdirectories from the source path to the destination directory.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        public override void FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            if (!Directory.Exists(_destination))
                Directory.CreateDirectory(_destination);

            var allFilesInSourcePath = GetFilesInPath(sourcePath);

            if (syncPoint != null && syncPointManager != null)
            {
                ProcessSyncPoint(sourcePath, syncPoint, syncPointManager, allFilesInSourcePath);
            }
            else
            {
                ProcessStraightSync(sourcePath, allFilesInSourcePath);
            }
        }

        private void ProcessStraightSync(string sourcePath, IEnumerable<string> allFilesInSourcePath)
        {
            foreach (var srcFilePath in allFilesInSourcePath)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                var destFilePath = Path.Combine(_destination, relativePath);

                SynchronizeFile(srcFilePath, destFilePath);
            }
        }

        private void ProcessSyncPoint(string sourceDirectory, SyncPoint newSyncPoint, SyncPointManager syncPointManager, IEnumerable<string> allSourceLocationFiles)
        {
            var allFilesPartOfSyncPoint = syncPointManager.GetFileEntriesAtSyncpoint(newSyncPoint.SyncPointId);

            foreach (var srcFilePath in allSourceLocationFiles)
            {
                var relativeSourcePath = Path.GetRelativePath(sourceDirectory, srcFilePath);
                var existingEntry = allFilesPartOfSyncPoint.FirstOrDefault(e => e.SourcePath.Equals(relativeSourcePath, StringComparison.OrdinalIgnoreCase));

                // If the sync point entry exists, check if the file is newer than the existing sync point
                if (existingEntry != null)
                {
                    var syncPointPath = Path.Combine(_destination, existingEntry.RelativeRemotePath);

                    var sourceFileInfo = new FileInfo(srcFilePath);
                    var targetFileInfo = new FileInfo(syncPointPath);

                    var sourceFileDateTime = sourceFileInfo.LastWriteTimeUtc;
                    var targetFileDateTime = targetFileInfo.LastWriteTimeUtc;
                    var destFilePath = Path.Combine(Path.Combine(_destination, newSyncPoint.SyncPointId), relativeSourcePath);
                    var destEntryPath = Path.Combine(newSyncPoint.SyncPointId, relativeSourcePath);

                    // If the sync point entry is deleted, we need to add it back
                    if (existingEntry.EntryType == SyncPointEntryType.Deleted)
                    {
                        newSyncPoint.AddEntry(relativeSourcePath, destEntryPath, SyncPointEntryType.AddOrChanged);
                        SynchronizeFile(srcFilePath, destFilePath);
                        continue;
                    }

                    //check if the sync point is old compared to the source file.
                    if (File.Exists(syncPointPath) && sourceFileDateTime > targetFileDateTime)
                    {                       
                        if (sourceFileInfo.Length != targetFileInfo.Length)
                        {
                            // replicate file into the new sync point location
                            newSyncPoint.AddEntry(relativeSourcePath, destEntryPath, SyncPointEntryType.AddOrChanged);
                            SynchronizeFile(srcFilePath, destFilePath);
                            continue;
                        }
                    }
                }
                else
                {
                    // replicate file into the new sync point location
                    var destFilePath = Path.Combine(Path.Combine(_destination, newSyncPoint.SyncPointId), relativeSourcePath);
                    var destEntryPath = Path.Combine(newSyncPoint.SyncPointId, relativeSourcePath);
                    newSyncPoint.AddEntry(relativeSourcePath, destEntryPath);
                    SynchronizeFile(srcFilePath, destFilePath);
                }
            }

            var updatedFileListOfSyncpoint = syncPointManager.GetFileEntriesAtSyncpoint(newSyncPoint.SyncPointId);

            // Now we need to check for files that were deleted since the last sync point
            DetectRemovedFiles(sourceDirectory, updatedFileListOfSyncpoint, allSourceLocationFiles, newSyncPoint);
            
            //now save the sync point
            newSyncPoint.Save(Path.Combine(_destination, newSyncPoint.SyncPointId, newSyncPoint.SyncPointId + ".syncpoint.json"));
        }

        /// <summary>
        /// Synchronizes a single file from the source to the destination, using the specified overwrite option.
        /// If oldDestFilePath is provided, deletes the old file (for renames).
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="destFilePath">The full path to the destination file.</param>
        /// <param name="oldDestFilePath">The old destination file path to delete (optional, for renames).</param>
        public override void SynchronizeFile(string srcFilePath, string destFilePath, string? oldDestFilePath = null)
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
                        action = "Update";
                    }
                    else 
                    {
                        bool areFirst4KDifferent = AreFirst4KDifferent(srcFilePath, destFilePath);
                        if (areFirst4KDifferent)
                        {
                            shouldCopy = true;
                            action = "Update";
                        } else
                        {
                            var srcHash = ComputeSHA256(srcFilePath);
                            var destHash = ComputeSHA256(destFilePath);
                            if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                            {
                                shouldCopy = true;
                                action = "Update";
                            }
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
                            UserIO.Message($"{action} - {destFilePath}");
                            break;
                        }
                        catch (IOException)
                        {
                            UserIO.Error($"File locked when trying to sync from '{srcFilePath}' to '{destFilePath}'. Retrying {attempt + 1}/{maxRetries}...");
                            attempt++;
                            if (attempt >= maxRetries) throw new IOException($"Failed to copy file after {maxRetries} attempts.");
                            Thread.Sleep(delayMs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserIO.Error($"Failed to sync file '{srcFilePath}' to '{destFilePath}'", ex);
            }
        }
    }
}
