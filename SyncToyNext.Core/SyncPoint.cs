using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
    [JsonSerializable(typeof(SyncPoint))]
    [JsonSerializable(typeof(SyncPointEntry))]
    internal partial class SyncPointJsonContext : JsonSerializerContext
    {
    }

    // <summary>
    /// JSON source generator context for trim-safe serialization.
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    )]
    [JsonSerializable(typeof(SyncPointRoot))]
    internal partial class SyncPointRootJsonContext : JsonSerializerContext
    {
    }

    public enum SyncPointEntryType
    {
        AddOrChanged,
        Deleted
    }

    public class SyncPointEntry
    {
        public string SourcePath { get; set; } = string.Empty;
        public string RelativeRemotePath { get; set; } = string.Empty;
        
        [JsonConverter(typeof(JsonStringEnumConverter<SyncPointEntryType>))]
        public SyncPointEntryType EntryType { get; set; } = SyncPointEntryType.AddOrChanged;
    }

    public class SyncPoint
    {
        public string SyncPointId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastSyncTime { get; set; }
        public List<SyncPointEntry> Entries { get; set; } = new List<SyncPointEntry>();

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
    }

    public class SyncPointRoot
    {
        public string SourceLocation { get; set; } = string.Empty;
        public bool Zipped { get; set; } = false;
    }

    public class SyncPointManager
    {
        private List<SyncPoint> _syncPoints;
        private string _path;
        private string _sourcePath;
        private bool _isZipped = false;
        private SyncPointRoot? _syncPointRoot;

        public bool IsZipped => _isZipped;

        public SyncPointRoot SyncPointRoot => _syncPointRoot ?? throw new InvalidOperationException("SyncPointRoot is not initialized.");

        public SyncPointManager(string path, string sourcePath = "")
        {
            if(Path.HasExtension(path) && Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _isZipped = true;
                path = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid zip file path.");
            }

            _path = path;
            _sourcePath = sourcePath;
            _syncPoints = new List<SyncPoint>();
            LoadSyncPointRoot();
            LoadSyncPoints();
            
        }

        public IReadOnlyList<SyncPoint> SyncPoints => _syncPoints.AsReadOnly();

        private void LoadSyncPointRoot()
        {
            var rootFilePath = Path.Combine(_path, "syncpointroot.json");
            if (File.Exists(rootFilePath))
            {
                var json = File.ReadAllText(rootFilePath);
                _syncPointRoot =  JsonSerializer.Deserialize(json, SyncPointRootJsonContext.Default.SyncPointRoot) ?? new SyncPointRoot();
                _sourcePath = _syncPointRoot.SourceLocation;

                if (_syncPointRoot.Zipped != _isZipped)
                {
                    throw new InvalidOperationException($"Cannot mix between zipped and non-zipped destination targets when working with syncpoints.");
                }
                if (string.IsNullOrWhiteSpace(_syncPointRoot.SourceLocation) || _syncPointRoot.SourceLocation != _sourcePath)
                {
                    throw new InvalidOperationException($"SyncPointRoot source location mismatch. Expected: {_sourcePath}, Found: {_syncPointRoot.SourceLocation}");
                }
            }
            else
            {
                _syncPointRoot = new SyncPointRoot();
                _syncPointRoot.SourceLocation = _sourcePath; // Set default source location to the current path
                _syncPointRoot.Zipped = _isZipped;
                var json = JsonSerializer.Serialize(_syncPointRoot, SyncPointRootJsonContext.Default.SyncPointRoot);
                File.WriteAllText(rootFilePath, json);
            }
        }

        private void LoadSyncPoints()
        {
            var childDirectories = Directory.GetDirectories(_path);

            foreach (var directory in childDirectories)
            {
                Directory.GetFiles(directory, "*.syncpoint.json").ToList().ForEach(file =>
                {
                    try
                    {
                        var syncPoint = SyncPoint.Load(file);
                        _syncPoints.Add(syncPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error loading sync point from {file}: {ex.Message}");
                    }
                });
            }

            // Sort sync points by LastSyncTime, descending
            _syncPoints.Sort((sp1, sp2) => sp2.LastSyncTime.CompareTo(sp1.LastSyncTime));
        }

        public SyncPoint AddSyncPoint(string sourcePath, string syncPointID = "", string description = "")
        {
            //first check if the syncPointID is already used
            if (_syncPoints.Any(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"SyncPoint ID '{syncPointID}' already exists.");
            }

            DateTime now = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(syncPointID))
            {
                syncPointID = $"{now:yyyyMMddHHmmss}UTC";

                if (_syncPoints.Any(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Generated SyncPoint ID '{syncPointID}' already exists.");
                }
            }

            Directory.CreateDirectory(Path.Combine(_path, syncPointID));

            var newSyncPoint = new SyncPoint
            {
                SyncPointId = syncPointID,
                Description = description,
                LastSyncTime = now,
                Entries = new List<SyncPointEntry>()
            };

            _syncPoints.Add(newSyncPoint);
            _syncPoints.Sort((sp1, sp2) => sp2.LastSyncTime.CompareTo(sp1.LastSyncTime));

            newSyncPoint.Save(Path.Combine(_path, syncPointID, $"{syncPointID}.syncpoint.json"));

            return newSyncPoint;
        }

        /// <summary>
        /// Get a specific sync point by its ID.
        /// </summary>
        /// <param name="syncPointID"></param>
        /// <returns></returns>
        public SyncPoint? GetSyncPoint(string syncPointID)
        {
            return _syncPoints.FirstOrDefault(sp => sp.SyncPointId.Equals(syncPointID, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all the file entries at a specific sync point. Not only the entries of the sync point itself, but also all previous sync points that were created before the given sync point.
        /// This way we get a complete state of the files at the time of the sync point.
        /// </summary>
        /// <param name="syncPointID">The syncpoint ID to check</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<SyncPointEntry> GetFileEntriesAtSyncpoint(string syncPointID)
        {
            var requestedSyncPoint = GetSyncPoint(syncPointID);
            
            if (requestedSyncPoint == null)
                throw new InvalidOperationException($"SyncPoint with ID '{syncPointID}' not found.");

            var allRelevantSyncPoints = _syncPoints.Where(sp => sp.LastSyncTime < requestedSyncPoint.LastSyncTime)
                                                .OrderByDescending(sp => sp.LastSyncTime)
                                                .ToList();
            
            allRelevantSyncPoints.Insert(0, requestedSyncPoint);

            var result = new List<SyncPointEntry>();

            // Use a HashSet to track included restore targets
            HashSet<string> includedRestoreTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var syncPoint in allRelevantSyncPoints)
            {
                foreach (var entry in syncPoint.Entries)
                {
                    // If the entry is already included in the result, as we sorted the sync points by LastSyncTime descending,
                    // we know that this entry represents the most recent state of the file.
                    if (!includedRestoreTargets.Contains(entry.SourcePath))
                    {
                        //if the entry does not exist in the current sync point, we add it
                        result.Add(new SyncPointEntry
                        {
                            SourcePath = entry.SourcePath,
                            RelativeRemotePath = Path.Combine(entry.RelativeRemotePath),
                            EntryType = entry.EntryType
                        });
                        includedRestoreTargets.Add(entry.SourcePath);
                    }
                }
            }

            return result;
        }
    }
}
