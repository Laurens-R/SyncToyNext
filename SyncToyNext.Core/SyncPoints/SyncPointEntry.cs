using System;
using System.Text.Json.Serialization;

namespace SyncToyNext.Core
{
    public class SyncPointEntry
    {
        public string SourcePath { get; set; } = string.Empty;
        public string RelativeRemotePath { get; set; } = string.Empty;
        
        [JsonConverter(typeof(JsonStringEnumConverter<SyncPointEntryType>))]
        public SyncPointEntryType EntryType { get; set; } = SyncPointEntryType.AddOrChanged;

        public override string ToString()
        {
            return RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }
}
