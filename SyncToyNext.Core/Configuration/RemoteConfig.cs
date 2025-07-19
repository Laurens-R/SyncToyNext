using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{

    // <summary>
    /// JSON source generator context for trim-safe serialization.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    )]
    [JsonSerializable(typeof(RemoteConfig))]
    internal partial class RemoteConfigJsonContext : JsonSerializerContext
    {
    }

    public class RemoteConfig
    {
        public string RemotePath { get; set; } = string.Empty;
        public string CurrentSyncPoint { get; set; } = string.Empty;

        public RemoteConfig() { }

        public RemoteConfig(string remotePath, string sourcePath)
        {
            var syncPointManager = new SyncPointManager(remotePath);
            RemotePath = remotePath;
        }

        public void Save(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException($"The directory '{Path.GetDirectoryName(rootDirectory)}' does not exist. Or a file was provided.");
            }

            var stnInternalsFolder = Path.Combine(rootDirectory, ".stn");
            if (!Directory.Exists(stnInternalsFolder))
            {
                Directory.CreateDirectory(stnInternalsFolder);
            }

            var json = JsonSerializer.Serialize(this, RemoteConfigJsonContext.Default.RemoteConfig);
            File.WriteAllText(Path.Combine(rootDirectory, ".stn/stn.remote.json"), json, Encoding.UTF8);
        }

        public static RemoteConfig? Load(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                return null;
            }

            var stnInternalsFolder = Path.Combine(rootDirectory, ".stn");
            if (!Directory.Exists(stnInternalsFolder))
            {
                Directory.CreateDirectory(stnInternalsFolder);
            }

            var configFile = Path.Combine(stnInternalsFolder, "stn.remote.json");

            if (!File.Exists(configFile))
            {
                return null;
            }

            var json = File.ReadAllText(configFile, Encoding.UTF8);
            return JsonSerializer.Deserialize(json, RemoteConfigJsonContext.Default.RemoteConfig) ?? new RemoteConfig(string.Empty, string.Empty);
        }

        public static bool RemoteConfigExists(string rootDirectory)
        {
            if (!Directory.Exists(rootDirectory))
            {
                return false;
            }

            var stnInternalsFolder = Path.Combine(rootDirectory, ".stn");
            if (!Directory.Exists(stnInternalsFolder))
            {
                return false;
            }

            var configFile = Path.Combine(stnInternalsFolder, "stn.remote.json");
            return File.Exists(configFile);
        }
    }
}
