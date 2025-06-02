namespace SyncToyNext.Core
{
    /// <summary>
    /// Specifies how file overwrites are handled during synchronization.
    /// </summary>
    public enum OverwriteOption
    {
        /// <summary>
        /// Never overwrite files in the destination. Only copy files that do not exist.
        /// </summary>
        NeverOverwrite,
        /// <summary>
        /// Only overwrite files in the destination if the source file is newer.
        /// </summary>
        OnlyOverwriteIfNewer,
        /// <summary>
        /// Always overwrite files in the destination, regardless of modification date.
        /// </summary>
        AlwaysOverwrite
    }
}
