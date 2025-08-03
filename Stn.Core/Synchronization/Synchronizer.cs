using Stn.Core.SyncPoints;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.Synchronizers
{
    public abstract class Synchronizer
    {
        public Action<int, int, string>? UpdateProgressHandler { get; set; } = null;

        public abstract void OpenTarget();
        public abstract void CloseTarget();

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
        public abstract bool SynchronizeFile(string srcFilePath, string relativeOrDestPath, string? oldDestFilePath = null);

        protected void DetectRemovedEntries(string sourceDirectory, List<SyncPointEntry> syncPointEntries, SyncPoint syncPoint)
        {
            int fileCount = syncPointEntries.Count();
            int currentFile = 1;

            // Now we need to check for files that were deleted since the last sync point
            foreach (var entry in syncPointEntries)
            {
                if(UpdateProgressHandler != null) UpdateProgressHandler(currentFile, fileCount, $"Check {entry.SourcePath}");

                var relativeSourcePath = entry.SourcePath;
                var relativePath = entry.RelativeRemotePath;

                // If the file no longer exists in the source, mark it as deleted
                var fileExistsInSource = Path.Exists(Path.Combine(sourceDirectory, relativeSourcePath));

                if (!fileExistsInSource)
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
