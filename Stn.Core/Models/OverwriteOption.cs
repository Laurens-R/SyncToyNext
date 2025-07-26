namespace SyncToyNext.Core.Models
{
    /// <summary>
    /// Specifies how file overwrites are handled during synchronization.
    /// </summary>
    public enum OverwriteOption
    {
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
