using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Manages loading and saving of synchronization configuration, which is a collection of SyncProfiles.
    /// </summary>
    public class SyncConfiguration
    {
        /// <summary>
        /// The list of synchronization profiles.
        /// </summary>
        public List<SyncProfile> Profiles { get; set; } = new List<SyncProfile>();

        /// <summary>
        /// Gets the default configuration file path (in the application directory).
        /// </summary>
        public static string GetDefaultConfigPath()
        {
            var exeDir = AppContext.BaseDirectory;
            return Path.Combine(exeDir, "SyncToyNext.config.json");
        }

        /// <summary>
        /// Loads the configuration from the specified file, or from the default location if not specified.
        /// </summary>
        public static SyncConfiguration Load(string? path = null)
        {
            path ??= GetDefaultConfigPath();
            if (!File.Exists(path))
                return new SyncConfiguration();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SyncConfiguration>(json) ?? new SyncConfiguration();
        }

        /// <summary>
        /// Saves the configuration to the specified file, or to the default location if not specified.
        /// </summary>
        public void Save(string? path = null)
        {
            path ??= GetDefaultConfigPath();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
