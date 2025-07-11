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
            // If no config file is provided and none is found, show a helpful error and exit
            string resolvedConfigPath = configPath ?? SyncConfiguration.GetDefaultConfigPath();
            if (!System.IO.File.Exists(resolvedConfigPath))
            {
                Console.WriteLine("Error: No configuration file was provided and none was found at the default location.");
                Console.WriteLine($"Expected config file at: {resolvedConfigPath}");
                Console.WriteLine("Please provide a config file using the --config <file> argument, or create a config file at the default location.");
                Environment.Exit(1);
            }
            Configuration = SyncConfiguration.Load(configPath);
            _strictMode = strictMode; 

            // Validate all profiles before proceeding
            var idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var profile in Configuration.Profiles)
            {
                ValidateProfile(idSet, profile);
            }

            // Check for clean shutdown marker
            if (!SyncConfiguration.WasCleanShutdown())
            {
                // Remove marker if present (corrupt/old)
                SyncConfiguration.RemoveCleanShutdownMarker();
                // Perform a full sync for all profiles
                foreach (var profile in Configuration.Profiles)
                {
                    InitializeProfile(strictMode, profile);
                }
            }
            // Remove marker so next run will require a clean shutdown again
            SyncConfiguration.RemoveCleanShutdownMarker();
        }

        private static void InitializeProfile(bool strictMode, SyncProfile profile)
        {
            var logger = new Logger(profile.SourcePath);
            var overwriteOption = profile.OverwriteOption;
            if (profile.DestinationIsZip)
            {
                var zipSync = new ZipFileSynchronizer(profile.DestinationPath, overwriteOption, logger, strictMode);
                zipSync.FullSynchronization(profile.SourcePath);
            }
            else
            {
                var fileSync = new FileSynchronizer(profile.DestinationPath, overwriteOption, logger, strictMode);
                fileSync.FullSynchronization(profile.SourcePath);
            }
        }

        private static void ValidateProfile(HashSet<string> idSet, SyncProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                Console.WriteLine("Error: Each profile must have a non-empty 'Id'. Please correct your configuration.");
                Environment.Exit(1);
            }
            if (!idSet.Add(profile.Id))
            {
                Console.WriteLine($"Error: Duplicate profile Id detected: '{profile.Id}'. Each profile Id must be unique.");
                Environment.Exit(1);
            }
            if (string.IsNullOrWhiteSpace(profile.SourcePath))
            {
                Console.WriteLine($"Error: Profile '{profile.Id}' is missing a 'SourcePath'. Please correct your configuration.");
                Environment.Exit(1);
            }
            if (string.IsNullOrWhiteSpace(profile.DestinationPath))
            {
                Console.WriteLine($"Error: Profile '{profile.Id}' is missing a 'DestinationPath'. Please correct your configuration.");
                Environment.Exit(1);
            }
            if (!System.IO.Directory.Exists(profile.SourcePath))
            {
                Console.WriteLine($"Error: SourcePath '{profile.SourcePath}' for profile '{profile.Id}' does not exist on the filesystem.");
                Environment.Exit(1);
            }
            // For DestinationPath, check directory or parent directory if destination is zip
            if (profile.DestinationIsZip)
            {
                var destDir = System.IO.Path.GetDirectoryName(profile.DestinationPath);
                if (string.IsNullOrWhiteSpace(destDir) || !System.IO.Directory.Exists(destDir))
                {
                    Console.WriteLine($"Error: The parent directory for DestinationPath '{profile.DestinationPath}' (profile '{profile.Id}') does not exist.");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (!System.IO.Directory.Exists(profile.DestinationPath))
                {
                    Console.WriteLine($"Error: DestinationPath '{profile.DestinationPath}' for profile '{profile.Id}' does not exist on the filesystem.");
                    Environment.Exit(1);
                }
            }
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
                    profile.OverwriteOption,
                    profile.DestinationIsZip,
                    profile.SyncInterval,
                    profile.Mode,
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
