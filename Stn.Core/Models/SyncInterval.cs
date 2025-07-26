namespace SyncToyNext.Core.Models
{
    /// <summary>
    /// Specifies how and when a sync profile should be synchronized.
    /// </summary>
    public enum SyncInterval
    {
        /// <summary>
        /// Synchronize in real time (on file changes).
        /// </summary>
        Realtime,
        /// <summary>
        /// Synchronize every hour.
        /// </summary>
        Hourly,
        /// <summary>
        /// Synchronize once per day.
        /// </summary>
        Daily,
        /// <summary>
        /// Synchronize only at shutdown.
        /// </summary>
        AtShutdown
    }
}
