using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{
    public abstract class Synchronizer
    {
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

        protected IEnumerable<string> GetFilesInPath(string sourcePath)
        {
            return Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(f).Equals("stn.remote.json", StringComparison.OrdinalIgnoreCase));

        }

        protected void DetectRemovedFiles(string sourceDirectory, List<SyncPointEntry> syncPointFiles, IEnumerable<string> localFiles, SyncPoint syncPoint)
        {
            // Now we need to check for files that were deleted since the last sync point
            foreach (var entry in syncPointFiles)
            {
                var relativeSourcePath = entry.SourcePath;
                var relativePath = entry.RelativeRemotePath;
                // If the file no longer exists in the source, mark it as deleted

                var sourceFileEntry = localFiles.FirstOrDefault(f => f.Equals(Path.Combine(sourceDirectory, relativeSourcePath), StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(sourceFileEntry))
                {
                    syncPoint.AddEntry(relativeSourcePath, relativePath, SyncPointEntryType.Deleted);
                    Console.WriteLine($"File '{relativeSourcePath}' marked as deleted in sync point '{syncPoint.SyncPointId}'.");
                }
            }
        }

        public static string ComputeSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        public  static string ComputeSHA256(Stream stream)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
