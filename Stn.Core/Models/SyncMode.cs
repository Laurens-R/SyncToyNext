namespace Stn.Core.Models
{
    /// <summary>
    /// Specifies how and when a sync profile should be synchronized.
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// Performs an incremental synchronization, copying only changed files.
        /// </summary>
        Incremental,
        /// <summary>
        /// Performs a full synchronization.
        /// </summary>
        FullSync
    }
}
