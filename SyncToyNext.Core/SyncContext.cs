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
        private readonly bool _strictMode;
        public SyncConfiguration Configuration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncContext"/> class and loads configuration.
        /// </summary>
        /// <param name="configPath">Optional path to the configuration file.</param>
        /// <param name="strictMode">Optional flag to enable strict mode.</param>
        public SyncContext(string? configPath = null, bool strictMode = false)
        {
            Configuration = SyncConfiguration.Load(configPath);
            _strictMode = strictMode;
            // Check for clean shutdown marker
            if (!SyncConfiguration.WasCleanShutdown())
            {
                // Remove marker if present (corrupt/old)
                SyncConfiguration.RemoveCleanShutdownMarker();
                // Perform a full sync for all profiles
                foreach (var profile in Configuration.Profiles)
                {
                    var logger = new Logger(profile.SourcePath);
                    if (profile.DestinationIsZip)
                    {
                        var zipSync = new ZipFileSynchronizer(profile.DestinationPath, OverwriteOption.OnlyOverwriteIfNewer, logger, strictMode);
                        zipSync.FullSynchronization(profile.SourcePath);
                    }
                    else
                    {
                        var fileSync = new FileSynchronizer(profile.DestinationPath, OverwriteOption.OnlyOverwriteIfNewer, logger, strictMode);
                        fileSync.FullSynchronization(profile.SourcePath);
                    }
                }
            }
            // Remove marker so next run will require a clean shutdown again
            SyncConfiguration.RemoveCleanShutdownMarker();
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
                    profile.DestinationIsZip,
                    profile.SyncInterval,
                    _strictMode
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
            SyncConfiguration.WriteCleanShutdownMarker();
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
            var logger = new Logger(profile.SourcePath);
            if (useZip)
            {
                var zipSync = new ZipFileSynchronizer(profile.DestinationPath, overwriteOption, logger);
                zipSync.FullSynchronization(profile.SourcePath);
            }
            else
            {
                var fileSync = new FileSynchronizer(profile.DestinationPath, overwriteOption, logger);
                fileSync.FullSynchronization(profile.SourcePath);
            }
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
