/*
    STN - A file synchronization and source control solution
    Copyright (C) 2025  Laurens Ruijtenberg

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Reflection;

namespace Stn.Cli.Helpers
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
