using System.Reflection;

namespace SyncToyNext.Client.Helpers
{
    public static class VersionUtil
    {
        public static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var version = assembly.GetName().Version?.ToString();
            // Prefer informational version, but strip off any git hash or metadata for friendliness
            if (!string.IsNullOrEmpty(infoVersion))
            {
                var plusIdx = infoVersion.IndexOf('+');
                if (plusIdx > 0)
                    return infoVersion.Substring(0, plusIdx) + " (build " + infoVersion.Substring(plusIdx + 1).Replace("build.", "") + ")";
                return infoVersion;
            }
            return fileVersion ?? version ?? "unknown";
        }
    }
}
