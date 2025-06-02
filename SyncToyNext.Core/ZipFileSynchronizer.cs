using System;
using System.IO;
using System.IO.Compression;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Provides file synchronization logic for writing files into a Zip archive.
    /// </summary>
    public class ZipFileSynchronizer
    {
        private readonly string _zipFilePath;
        private readonly OverwriteOption _overwriteOption;
        private readonly Logger _logger;

        public ZipFileSynchronizer(string zipFilePath, OverwriteOption overwriteOption, Logger logger)
        {
            _zipFilePath = zipFilePath;
            _overwriteOption = overwriteOption;
            _logger = logger;
        }

        /// <summary>
        /// Synchronizes a single file into the zip archive.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="relativePath">The relative path inside the zip archive.</param>
        public void SynchronizeFile(string srcFilePath, string relativePath)
        {
            try
            {
                using var zip = new FileStream(_zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Update, leaveOpen: false);
                var entry = archive.GetEntry(relativePath.Replace('\\', '/'));
                bool fileExists = entry != null;
                bool shouldCopy = false;
                string action = "None";

                switch (_overwriteOption)
                {
                    case OverwriteOption.NeverOverwrite:
                        shouldCopy = !fileExists;
                        action = shouldCopy ? "Create" : "DoNothing";
                        break;
                    case OverwriteOption.OnlyOverwriteIfNewer:
                        if (!fileExists)
                        {
                            shouldCopy = true;
                            action = "Create";
                        }
                        else if (entry != null)
                        {
                            var srcLastWrite = File.GetLastWriteTimeUtc(srcFilePath);
                            var entryLastWrite = entry.LastWriteTime.UtcDateTime;
                            shouldCopy = srcLastWrite > entryLastWrite;
                            action = shouldCopy ? "Update" : "DoNothing";
                        }
                        break;
                    case OverwriteOption.AlwaysOverwrite:
                        shouldCopy = true;
                        action = fileExists ? "Update" : "Create";
                        break;
                }

                if (shouldCopy)
                {
                    entry?.Delete();
                    var newEntry = archive.CreateEntry(relativePath.Replace('\\', '/'), CompressionLevel.Optimal);
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
    }
}
