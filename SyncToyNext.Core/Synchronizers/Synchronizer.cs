using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.Synchronizers
{
    public abstract class Synchronizer
    {
        public Action<int, int, string>? UpdateProgressHandler { get; set; } = null;

        /// <summary>
        /// Synchronizes all files and subdirectories from the source path to the destination.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        public abstract void FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null);

        /// <summary>
        /// Synchronizes a single file from the source to the destination.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="relativeOrDestPath">The relative path (for zip) or destination file path (for folder).</param>
        public abstract void SynchronizeFile(string srcFilePath, string relativeOrDestPath, string? oldDestFilePath = null);

        protected void DetectRemovedFiles(string sourceDirectory, List<SyncPointEntry> syncPointFiles, IEnumerable<string> localFiles, SyncPoint syncPoint)
        {
            int fileCount = syncPointFiles.Count();
            int currentFile = 1;

            // Now we need to check for files that were deleted since the last sync point
            foreach (var entry in syncPointFiles)
            {
                if(UpdateProgressHandler != null) UpdateProgressHandler(currentFile, fileCount, $"Check {entry.SourcePath}");

                var relativeSourcePath = entry.SourcePath;
                var relativePath = entry.RelativeRemotePath;
                // If the file no longer exists in the source, mark it as deleted

                var sourceFileEntry = localFiles.FirstOrDefault(f => f.Equals(Path.Combine(sourceDirectory, relativeSourcePath), StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(sourceFileEntry))
                {
                    if (entry.EntryType != SyncPointEntryType.Deleted)
                    {
                        syncPoint.AddEntry(relativeSourcePath, relativePath, SyncPointEntryType.Deleted);
                        UserIO.Message($"File '{relativeSourcePath}' marked as deleted in sync point '{syncPoint.SyncPointId}'.");
                    }
                }

                currentFile++;
            }
        }
    }
}
