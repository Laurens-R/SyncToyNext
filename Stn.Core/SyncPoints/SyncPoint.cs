using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Stn.Core
{
    // <summary>
    /// JSON source generator context for trim-safe serialization.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    )]
    [JsonSerializable(typeof(SyncPoint))]
    [JsonSerializable(typeof(SyncPointEntry))]
    internal partial class SyncPointJsonContext : JsonSerializerContext
    {
    }

    public class SyncPoint
    {
        public string SyncPointId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool ReferencePoint { get; set; } = false;
        public DateTime LastSyncTime { get; set; }
        public List<SyncPointEntry> Entries { get; set; } = new List<SyncPointEntry>();

        public static string GenerateSyncpointID()
        {
            return $"{DateTime.UtcNow:yyyyMMddHHmmss}UTC";
        }

        public static SyncPoint Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"SyncPoint file not found: {filePath}");
            
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize(json, SyncPointJsonContext.Default.SyncPoint) ?? new SyncPoint();
        }

        public void Save(string filePath)
        {
            var json = JsonSerializer.Serialize(this, SyncPointJsonContext.Default.SyncPoint);
            File.WriteAllText(filePath, json);
        }

        public void AddEntry(string sourcePath, string relativeDestinationPath, SyncPointEntryType type = SyncPointEntryType.AddOrChanged)
        {
            var entry = new SyncPointEntry
            {
                SourcePath = sourcePath,
                RelativeRemotePath = relativeDestinationPath,
                EntryType = type
            };
            Entries.Add(entry);
        }

        public override string ToString()
        {
            var descriptionToRender = String.IsNullOrWhiteSpace(Description) ? "(no description provided)" : Description;
            var localTime = LastSyncTime.ToLocalTime();
            var localTimeStr = localTime.ToShortTimeString();
            var localDateStr = localTime.ToShortDateString();
            return $"{localDateStr} at {localTimeStr} - {descriptionToRender}";
        }
    }
}
