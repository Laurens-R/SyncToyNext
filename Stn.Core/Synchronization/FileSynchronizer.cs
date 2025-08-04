using Stn.Core.IO;
using Stn.Core.Synchronization;
using Stn.Core.SyncPoints;
using Stn.Core.UX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stn.Core.Synchronizers
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
        public override async Task FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            if (!Directory.Exists(_destination))
                Directory.CreateDirectory(_destination);

            var allFilesInSourcePath = FileSystemHelpers.GetFilesInPath(sourcePath);

            if (syncPoint != null && syncPointManager != null)
            {
                await ProcessSyncPoint(sourcePath, syncPoint, syncPointManager, allFilesInSourcePath);
            }
            else
            {
                await ProcessStraightSync(sourcePath, allFilesInSourcePath);
            }
        }

        private async Task ProcessStraightSync(string sourcePath, IEnumerable<string> allFilesInSourcePath)
        {
            int progressCounter = 0;
            int totalFileCount = allFilesInSourcePath.Count();

            foreach (var srcFilePath in allFilesInSourcePath)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                var destFilePath = Path.Combine(_destination, relativePath);

                var succes = await SynchronizeFile(srcFilePath, destFilePath);
                if (!succes) throw new IOException($"Could not transfer file from {srcFilePath} to {destFilePath}.");

                progressCounter++;

                if (UpdateProgressHandler != null)
                {
                    UpdateProgressHandler(progressCounter, totalFileCount, srcFilePath);
                }
            }
        }

        private async Task ProcessSyncPoint(string sourceDirectory, SyncPoint newSyncPoint, SyncPointManager syncPointManager, IEnumerable<string> allSourceLocationFiles)
        {
            //First get the state of all the directories.
            var directories = FileSystemHelpers.GetDirectoriesInPath(sourceDirectory);

            foreach (var directory in directories)
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, directory);
                newSyncPoint.AddEntry(relativePath, relativePath, SyncPointEntryType.Directory);
            }

            //Now process all files.
            var allFilesPartOfSyncPoint = syncPointManager.GetEntriesAtSyncPoint(newSyncPoint.SyncPointId);

            int progressCounter = 0;
            int totalFileCount = allSourceLocationFiles.Count();

            foreach (var srcFilePath in allSourceLocationFiles)
            {
                var relativeSourcePath = Path.GetRelativePath(sourceDirectory, srcFilePath);
                var existingEntry = allFilesPartOfSyncPoint.FirstOrDefault(e => e.SourcePath.Equals(relativeSourcePath, StringComparison.OrdinalIgnoreCase));

                // If the sync point entry exists, check if the file is newer than the existing sync point
                if (existingEntry != null)
                {
                    var syncPointPath = Path.Combine(_destination, existingEntry.SyncpointID, existingEntry.RelativeRemotePath);

                    var sourceFileInfo = new FileInfo(srcFilePath);
                    var targetFileInfo = new FileInfo(syncPointPath);

                    var sourceFileDateTime = sourceFileInfo.LastWriteTimeUtc;
                    var targetFileDateTime = targetFileInfo.LastWriteTimeUtc;
                    var destFilePath = Path.Combine(Path.Combine(_destination, newSyncPoint.SyncPointId), relativeSourcePath);
                    var destEntryPath = Path.Combine(relativeSourcePath);

                    // If the sync point entry is deleted, we need to add it back
                    if (existingEntry.EntryType == SyncPointEntryType.Deleted)
                    {
                        newSyncPoint.AddEntry(relativeSourcePath, destEntryPath, SyncPointEntryType.File);
                        var succes = await SynchronizeFile(srcFilePath, destFilePath);
                        if (!succes) 
                            throw new IOException($"Could not transfer file from {srcFilePath} to {destFilePath}.");
                        continue;
                    }

                    bool fileExists = File.Exists(syncPointPath);

                    //check if the sync point is old compared to the source file.
                    if (fileExists && sourceFileDateTime > targetFileDateTime)
                    {                       
                        if (sourceFileInfo.Length != targetFileInfo.Length)
                        {
                            // replicate file into the new sync point location
                            newSyncPoint.AddEntry(relativeSourcePath, destEntryPath, SyncPointEntryType.File);
                            bool succes = await SynchronizeFile(srcFilePath, destFilePath);
                            if (!succes) 
                                throw new IOException($"Could not transfer file from {srcFilePath} to {destFilePath}.");
                            continue;
                        }
                    }
                }
                else
                {
                    // replicate file into the new sync point location
                    var destFilePath = Path.Combine(Path.Combine(_destination, newSyncPoint.SyncPointId), relativeSourcePath);
                    newSyncPoint.AddEntry(relativeSourcePath, relativeSourcePath);
                    bool succes = await SynchronizeFile(srcFilePath, destFilePath);
                    if (!succes) 
                        throw new IOException($"Could not transfer file from {srcFilePath} to {destFilePath}.");
                }

                progressCounter++;

                UpdateProgressHandler?.Invoke(progressCounter, totalFileCount, srcFilePath);
            }

            var updatedFileListOfSyncpoint = syncPointManager.GetEntriesAtSyncPoint(newSyncPoint.SyncPointId);

            // Now we need to check for files that were deleted since the last sync point
            DetectRemovedEntries(sourceDirectory, updatedFileListOfSyncpoint, newSyncPoint);
            
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
        public override async Task<bool> SynchronizeFile(string srcFilePath, string destFilePath, string? oldDestFilePath = null)
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
                shouldCopy = FileSystemHelpers.IsFileDifferent(srcFilePath, destFilePath);

                if (shouldCopy)
                {
                    action = "Update";
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

                    bool succes = await FileSystem.Copy(srcFilePath, destFilePath);

                    if(!succes)
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override void OpenTarget()
        {
            //do nothing because this is a file system synchronizer.
        }

        public override void CloseTarget()
        {
            //DO nothing because this is a filesystem synchronizer.
        }
    }
}
