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

            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                var destDir = dirPath.Replace(sourcePath, _destination);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
            }

            foreach (var srcFilePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                var destFilePath = Path.Combine(_destination, relativePath);
                SynchronizeFile(srcFilePath, destFilePath);
            }
        }

        /// <summary>
        /// Synchronizes a single file from the source to the destination, using the specified overwrite option.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="destFilePath">The full path to the destination file.</param>
        public void SynchronizeFile(string srcFilePath, string destFilePath)
        {
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
                    File.Copy(srcFilePath, destFilePath, true);
                }
                _logger.LogSyncAction(destFilePath, action);
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
