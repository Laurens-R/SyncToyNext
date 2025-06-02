using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Represents the root context for the sync application, managing configuration, watchers, and manual syncs.
    /// </summary>
    public class SyncContext : IDisposable
    {
        private readonly List<FileSyncWatcher> _watchers = new();
        private bool _isRunning = false;
        public SyncConfiguration Configuration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncContext"/> class and loads configuration.
        /// </summary>
        /// <param name="configPath">Optional path to the configuration file.</param>
        public SyncContext(string? configPath = null)
        {
            Configuration = SyncConfiguration.Load(configPath);
        }

        /// <summary>
        /// Starts all watchers for the configured sync profiles.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            foreach (var profile in Configuration.Profiles)
            {
                var watcher = new FileSyncWatcher(
                    profile.SourcePath,
                    profile.DestinationPath,
                    OverwriteOption.OnlyOverwriteIfNewer,
                    profile.DestinationIsZip
                );
                _watchers.Add(watcher);
            }
            _isRunning = true;
        }

        /// <summary>
        /// Stops all running watchers and releases resources.
        /// </summary>
        public void Shutdown()
        {
            foreach (var watcher in _watchers)
                watcher.Dispose();
            _watchers.Clear();
            _isRunning = false;
        }

        /// <summary>
        /// Manually triggers a full sync for a given profile by ID or Name.
        /// </summary>
        /// <param name="profileIdOrName">The ID or Name of the profile to sync.</param>
        /// <param name="overwriteOption">Optional overwrite option (default: OnlyOverwriteIfNewer).</param>
        /// <param name="toZip">If true, sync to a zip file; otherwise, to a directory.</param>
        public void ManualSyncProfile(string profileIdOrName, OverwriteOption overwriteOption = OverwriteOption.OnlyOverwriteIfNewer, bool? toZip = null)
        {
            var profile = Configuration.Profiles.FirstOrDefault(p => string.Equals(p.Id, profileIdOrName, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
                throw new ArgumentException($"No profile found with ID/Name '{profileIdOrName}'", nameof(profileIdOrName));
            // Use the profile's DestinationIsZip unless overridden
            bool useZip = toZip ?? profile.DestinationIsZip;
            FileSynchronizer.FullSynchronization(profile.SourcePath, profile.DestinationPath, overwriteOption, useZip);
        }

        /// <summary>
        /// Manually triggers a full sync for arbitrary source and destination locations.
        /// </summary>
        /// <param name="sourcePath">Source directory.</param>
        /// <param name="destination">Destination directory or zip file.</param>
        /// <param name="overwriteOption">Overwrite option.</param>
        /// <param name="toZip">If true, sync to a zip file; otherwise, to a directory.</param>
        public void ManualSync(string sourcePath, string destination, OverwriteOption overwriteOption = OverwriteOption.OnlyOverwriteIfNewer, bool toZip = false)
        {
            FileSynchronizer.FullSynchronization(sourcePath, destination, overwriteOption, toZip);
        }

        /// <summary>
        /// Disposes all watchers and resources.
        /// </summary>
        public void Dispose()
        {
            Shutdown();
        }
    }
}
