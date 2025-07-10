using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Provides file synchronization logic for writing files into a Zip archive.
    /// </summary>
    public class ZipFileSynchronizer : ISynchronizer
    {
        private readonly string _zipFilePath;
        private readonly OverwriteOption _overwriteOption;
        private readonly Logger _logger;
        private readonly bool _strictMode;

        public ZipFileSynchronizer(string zipFilePath, OverwriteOption overwriteOption, Logger logger, bool strictMode = false)
        {
            _zipFilePath = zipFilePath;
            _overwriteOption = overwriteOption;
            _logger = logger;
            _strictMode = strictMode;
        }

        /// <summary>
        /// Synchronizes a single file into the zip archive.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="relativePath">The relative path inside the zip archive.</param>
        public void SynchronizeFile(string srcFilePath, string relativePath, string? oldDestFilePath = null)
        {
        
            try
            {
                using var zip = new FileStream(_zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Update, leaveOpen: false);
                var entryPath = relativePath.Replace("\\", "/");
                var entry = archive.GetEntry(entryPath);
                bool entryExists = entry != null;
                bool shouldCopy = false;
                string action = "None";

                if (oldDestFilePath != null )
                {
                    // If an old destination file was provided, delete it first
                    var oldEntry = archive.GetEntry(oldDestFilePath.Replace("\\", "/"));
                    oldEntry?.Delete();
                }

                if (_overwriteOption == OverwriteOption.AlwaysOverwrite)
                {
                    shouldCopy = true;
                    action = entryExists ? "Overwrite" : "Create";
                }
                else if (!entryExists)
                {
                    shouldCopy = true;
                    action = "Create";
                }
                else if (entry != null)
                {
                    var srcLastWrite = File.GetLastWriteTimeUtc(srcFilePath);
                    var entryLastWrite = entry.LastWriteTime.UtcDateTime;
                    if (srcLastWrite > entryLastWrite)
                    {
                        shouldCopy = true;
                        action = "Update";
                    }
                    else
                    {
                        long srcSize = new FileInfo(srcFilePath).Length;
                        long entrySize = entry.Length;
                        if (srcSize != entrySize)
                        {
                            shouldCopy = true;
                            action = "RepairSizeMismatch";
                        }
                        else if (_strictMode)
                        {
                            var srcHash = ComputeSHA256(srcFilePath);
                            string destHash;
                            using (var entryStream = entry.Open())
                            {
                                destHash = ComputeSHA256(entryStream);
                            }
                            if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                            {
                                shouldCopy = true;
                                action = "RepairChecksumMismatch";
                            }
                        }
                    }
                }

                if (shouldCopy)
                {
                    entry?.Delete();
                    var newEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                    using var entryStream = newEntry.Open();
                    using var fileStream = File.OpenRead(srcFilePath);
                    fileStream.CopyTo(entryStream);
                    newEntry.LastWriteTime = File.GetLastWriteTimeUtc(srcFilePath);
                }
                _logger.LogSyncAction(relativePath, action, "zip");
            }
            catch (IOException ioEx)
            {
                _logger.LogError($"IO error syncing '{srcFilePath}' to zip '{_zipFilePath}'", ioEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogError($"Access denied syncing '{srcFilePath}' to zip '{_zipFilePath}'", uaEx);
            }
            catch (InvalidDataException dataEx)
            {
                _logger.LogError($"Invalid data in zip '{_zipFilePath}'", dataEx);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error syncing '{srcFilePath}' to zip '{_zipFilePath}'", ex);
            }
        }

        /// <summary>
        /// Synchronizes all files and subdirectories from the source path into the zip archive.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        public void FullSynchronization(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            // Exclude 'synclogs' subfolder from sync
            var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Where(f => {
                    var rel = Path.GetRelativePath(sourcePath, f);
                    return !rel.StartsWith("synclogs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && !rel.StartsWith("synclogs/", StringComparison.OrdinalIgnoreCase);
                });
            foreach (var srcFilePath in allFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                if (relativePath.StartsWith("synclogs" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || relativePath.StartsWith("synclogs/", StringComparison.OrdinalIgnoreCase))
                    continue;
                SynchronizeFile(srcFilePath, relativePath);
            }
        }
        private static string ComputeSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
        private static string ComputeSHA256(Stream stream)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}
