namespace SyncToyNext.Core
{
    /// <summary>
    /// Interface for synchronizer classes (file-to-folder, file-to-zip, etc.).
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// Synchronizes all files and subdirectories from the source path to the destination.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        void FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null);

        /// <summary>
        /// Synchronizes a single file from the source to the destination.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="relativeOrDestPath">The relative path (for zip) or destination file path (for folder).</param>
        void SynchronizeFile(string srcFilePath, string relativeOrDestPath, string? oldDestFilePath = null);
    }
}
