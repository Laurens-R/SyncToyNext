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
        public SyncContext(string? configPath = null, bool strictMode = false, bool enterWatchMode = true)
        {
            // If no config file is provided and none is found, show a helpful error and exit
            string resolvedConfigPath = configPath ?? SyncConfiguration.GetDefaultConfigPath();
            if (!System.IO.File.Exists(resolvedConfigPath))
            {
                UserIO.Error("Error: No configuration file was provided and none was found at the default location.");
                UserIO.Error($"Expected config file at: {resolvedConfigPath}");
                UserIO.Error("Please provide a config file using the --config <file> argument, or create a config file at the default location.");
                Environment.Exit(1);
            }

            Configuration = SyncConfiguration.Load(configPath);
            _strictMode = strictMode;

            if (enterWatchMode)
            {
                // Validate all profiles before proceeding
                var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Check for clean shutdown marker. Everything below this is to ensure a stable state before starting watchers.
                if (!SyncConfiguration.WasCleanShutdown())
                {
                    // Remove marker if present (corrupt/old)
                    SyncConfiguration.RemoveCleanShutdownMarker();
                    // Perform a full sync for all profiles
                    foreach (var profile in Configuration.Profiles)
                    {
                        if (ValidateProfile(idSet, profile))
                        {
                            InitializeProfile(strictMode, profile);
                        }
                    }
                }
                // Remove marker so next run will require a clean shutdown again
                SyncConfiguration.RemoveCleanShutdownMarker();
            }
        }

        private static void InitializeProfile(bool strictMode, SyncProfile profile)
        {
            var overwriteOption = profile.OverwriteOption;
            if (profile.DestinationIsZip)
            {
                var zipSync = new ZipFileSynchronizer(profile.DestinationPath, overwriteOption, strictMode);
                zipSync.FullSynchronization(profile.SourcePath);
            }
            else
            {
                var fileSync = new FileSynchronizer(profile.DestinationPath, overwriteOption, strictMode);
                fileSync.FullSynchronization(profile.SourcePath);
            }
        }

        private static bool ValidateProfile(HashSet<string> idSet, SyncProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                UserIO.Error("Error: Each profile must have a non-empty 'Id'. Please correct your configuration.");
                return false;
            }
            if (!idSet.Add(profile.Id))
            {
                UserIO.Error($"Error: Duplicate profile Id detected: '{profile.Id}'. Each profile Id must be unique.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(profile.SourcePath))
            {
                UserIO.Error($"Error: Profile '{profile.Id}' is missing a 'SourcePath'. Please correct your configuration.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(profile.DestinationPath))
            {
                UserIO.Error($"Error: Profile '{profile.Id}' is missing a 'DestinationPath'. Please correct your configuration.");
                return false;
            }
            if (!System.IO.Directory.Exists(profile.SourcePath))
            {
                UserIO.Error($"Error: SourcePath '{profile.SourcePath}' for profile '{profile.Id}' does not exist on the filesystem.");
                return false;
            }
            // For DestinationPath, check directory or parent directory if destination is zip
            if (profile.DestinationIsZip)
            {
                var destDir = System.IO.Path.GetDirectoryName(profile.DestinationPath);
                if (string.IsNullOrWhiteSpace(destDir) || !System.IO.Directory.Exists(destDir))
                {
                    UserIO.Error($"Error: The parent directory for DestinationPath '{profile.DestinationPath}' (profile '{profile.Id}') does not exist.");
                    return false;
                }
            }
            else
            {
                if (!System.IO.Directory.Exists(profile.DestinationPath))
                {
                    UserIO.Error($"Error: DestinationPath '{profile.DestinationPath}' for profile '{profile.Id}' does not exist on the filesystem.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Starts all watchers for the configured sync profiles.
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;
            foreach (var profile in Configuration.Profiles)
            {
                var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (ValidateProfile(idSet, profile))
                {
                    var watcher = new FileSyncWatcher(
                        profile.SourcePath,
                        profile.DestinationPath,
                        profile.OverwriteOption,
                        profile.DestinationIsZip,
                        profile.SyncInterval,
                        profile.Mode,
                        _strictMode
                    );
                    _watchers.Add(watcher);
                }
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
        public void ManualSyncProfile(string profileIdOrName)
        {
            var profile = Configuration.Profiles.FirstOrDefault(p => string.Equals(p.Id, profileIdOrName, StringComparison.OrdinalIgnoreCase));
            if (profile == null)
                throw new ArgumentException($"No profile found with ID/Name '{profileIdOrName}'", nameof(profileIdOrName));
            // Use the profile's DestinationIsZip unless overridden
            bool useZip = profile.DestinationIsZip;

            if (useZip)
            {
                var zipSync = new ZipFileSynchronizer(profile.DestinationPath, profile.OverwriteOption);
                zipSync.FullSynchronization(profile.SourcePath);
            }
            else
            {
                var fileSync = new FileSynchronizer(profile.DestinationPath, profile.OverwriteOption);
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
