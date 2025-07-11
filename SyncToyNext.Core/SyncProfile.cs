using System;
using System.Collections.Generic;
using SyncToyNext.Core;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Represents a synchronization profile with a unique ID/Name, source path, and destination path.
    /// </summary>
    public class SyncProfile
    {
        /// <summary>
        /// Unique identifier or name for the profile.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The source directory path for synchronization.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// The destination directory path for synchronization.
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the destination is a zip file.
        /// </summary>
        public bool DestinationIsZip { get; set; } = false;

        /// <summary>
        /// Specifies the synchronization mode (incremental, full sync, etc.).
        /// /// </summary>
        public SyncMode Mode { get; set; } = SyncMode.Incremental;

        /// <summary>
        /// Specifies when this profile should be synchronized (realtime, hourly, daily, at shutdown).
        /// </summary>
        public SyncInterval SyncInterval { get; set; } = SyncToyNext.Core.SyncInterval.Realtime;

        /// <summary>
        /// Specifies how file overwrites are handled for this profile. Defaults to OnlyOverwriteIfNewer.
        /// </summary>
        public OverwriteOption OverwriteOption { get; set; } = OverwriteOption.OnlyOverwriteIfNewer;
    }
}
