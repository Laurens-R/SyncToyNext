using System;
using System.Text.Json.Serialization;

namespace SyncToyNext.Core
{
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

    public class SyncPointRoot
    {
        public Guid RootID { get; set; } = Guid.NewGuid();
        public string SourceLocation { get; set; } = string.Empty;
        public bool Zipped { get; set; } = false;
    }
}
