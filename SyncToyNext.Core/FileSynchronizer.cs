namespace SyncToyNext.Core
{
    /// <summary>
    /// Provides file and directory synchronization between two paths with configurable overwrite behavior.
    /// </summary>
    public static class FileSynchronizer
    {
        /// <summary>
        /// Synchronizes all files and subdirectories from the source path to the destination path or zip file.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        /// <param name="destination">The root directory or zip file to copy files to. The structure will be recreated here.</param>
        /// <param name="overwriteOption">Specifies how to handle existing files in the destination directory or zip file.</param>
        /// <param name="toZip">If true, synchronize into a zip file; otherwise, to a directory.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the source directory does not exist.</exception>
        /// <remarks>
        /// - Recursively traverses all files and subfolders in the source location.
        /// - Rebuilds the same folder/file structure in the target location.
        /// - Overwrite behavior is controlled by the <paramref name="overwriteOption"/> parameter.
        /// </remarks>
        public static void FullSynchronization(string sourcePath, string destination, OverwriteOption overwriteOption, bool toZip = false)
        {
            var logger = new Logger(sourcePath);
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            if (toZip)
            {
                var zipSync = new ZipFileSynchronizer(destination, overwriteOption, logger);
                foreach (var srcFilePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                    zipSync.SynchronizeFile(srcFilePath, relativePath);
                }
            }
            else
            {
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);

                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var destDir = dirPath.Replace(sourcePath, destination);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                }

                foreach (var srcFilePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                    var destFilePath = Path.Combine(destination, relativePath);
                    SynchronizeFile(srcFilePath, destFilePath, overwriteOption, logger);
                }
            }
        }

        /// <summary>
        /// Synchronizes a single file from the source to the destination, using the specified overwrite option.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="destFilePath">The full path to the destination file.</param>
        /// <param name="overwriteOption">Specifies how to handle existing files in the destination directory.</param>
        /// <param name="logger">Optional logger instance to log sync actions and errors.</param>
        public static void SynchronizeFile(string srcFilePath, string destFilePath, OverwriteOption overwriteOption, Logger? logger = null)
        {
            bool fileExists = File.Exists(destFilePath);
            bool shouldCopy = false;
            string action = "None";

            switch (overwriteOption)
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
                    else
                    {
                        var srcLastWrite = File.GetLastWriteTimeUtc(srcFilePath);
                        var destLastWrite = File.GetLastWriteTimeUtc(destFilePath);
                        shouldCopy = srcLastWrite > destLastWrite;
                        action = shouldCopy ? "Update" : "DoNothing";
                    }
                    break;
                case OverwriteOption.AlwaysOverwrite:
                    shouldCopy = true;
                    action = fileExists ? "Update" : "Create";
                    break;
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
                logger?.LogSyncAction(destFilePath, action);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to sync file '{srcFilePath}' to '{destFilePath}'", ex);
            }
        }
    }
}
