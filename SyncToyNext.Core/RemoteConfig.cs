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

        public RemoteConfig(string remotePath, string sourcePath)
        {
            var syncPointManager = new SyncPointManager(remotePath, sourcePath);
        }

        public void Save(string filePath)
        {
            if(!Directory.Exists(filePath))             {
                throw new DirectoryNotFoundException($"The directory '{Path.GetDirectoryName(filePath)}' does not exist. Or a file was provided.");
            }

            var json = JsonSerializer.Serialize(this, RemoteConfigJsonContext.Default.RemoteConfig);
            File.WriteAllText(Path.Combine(filePath, "stn.remote.json"), json, Encoding.UTF8);
        }

        public static RemoteConfig Load(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                throw new FileNotFoundException($"The directory '{filePath}' does not exist.");
            }

            var configFile = Path.Combine(filePath, "stn.remote.json");
            
            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException($"The configuration file '{configFile}' does not exist.");
            }

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<RemoteConfig>(json, RemoteConfigJsonContext.Default.RemoteConfig)!;
        }
    }
}
