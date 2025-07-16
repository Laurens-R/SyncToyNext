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
                    .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}.stn{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}.stn", StringComparison.OrdinalIgnoreCase));

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
                    UserIO.Message($"File '{relativeSourcePath}' marked as deleted in sync point '{syncPoint.SyncPointId}'.");
                }
            }
        }

        public static bool AreFirst4KDifferent(string filePath1, string filePath2)
        {
            using (var stream1 = File.OpenRead(filePath1))
            using (var stream2 = File.OpenRead(filePath2))
            {
                return AreFirst4KDifferent(stream1, stream2);
            }
        }

        public static bool AreFirst4KDifferent(Stream stream1, Stream stream2)
        {
            const int bufferSize = 4096; // 4KB
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];
            int bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
            int bytesRead2 = stream2.Read(buffer2, 0, bufferSize);
            if (bytesRead1 != bytesRead2)
                return true; // Different sizes
            for (int i = 0; i < bytesRead1; i++)
            {
                if (buffer1[i] != buffer2[i])
                    return true; // Found a difference
            }
            return false;    
        }

        public static string ComputeSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash);
        }

        public  static string ComputeSHA256(Stream stream)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash);
        }
    }
}
