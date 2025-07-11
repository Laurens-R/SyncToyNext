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
        public void FullSynchronization(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            if (!Directory.Exists(_destination))
                Directory.CreateDirectory(_destination);

            // Exclude 'synclogs' subfolder from sync (source and destination)
            var allDirs = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
                .Where(d => !d.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"synclogs", StringComparison.OrdinalIgnoreCase)
                    && !d.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
            foreach (var dirPath in allDirs)
            {
                var destDir = dirPath.Replace(sourcePath, _destination);
                if (!destDir.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"synclogs", StringComparison.OrdinalIgnoreCase)
                    && !destDir.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                }
            }

            var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !f.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase));
                    
            foreach (var srcFilePath in allFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                if (relativePath.StartsWith("synclogs" + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    continue;
                var destFilePath = Path.Combine(_destination, relativePath);
                if (destFilePath.Contains($"{System.IO.Path.DirectorySeparatorChar}synclogs{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || destFilePath.TrimEnd(System.IO.Path.DirectorySeparatorChar).EndsWith($"{System.IO.Path.DirectorySeparatorChar}synclogs", StringComparison.OrdinalIgnoreCase))
                    continue;
                SynchronizeFile(srcFilePath, destFilePath);
            }
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
                    var destDir = Path.GetDirectoryName(destFilePath);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir!);

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
