using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.IO
{
    public class IgnoreHelper
    {
        private static List<string> IgnoreFilters { get; set; } = new List<string>();

        public static void TryLoadIgnoreFile(string path)
        {
            IgnoreFilters.Clear();
            var ignoreFilePath = Path.Combine(path, ".stnignore");
            if (File.Exists(ignoreFilePath))
            {
                var ignoreContent = File.ReadAllText(ignoreFilePath);
                IgnoreFilters.AddRange(ignoreContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public static bool IsFileIgnored(string relativePath)
        {
            if (IgnoreFilters == null)
                return false;

            // Normalize path to use forward slashes
            var relPath = relativePath.Replace('\\', '/');

            foreach (var pattern in IgnoreFilters)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                    continue;
                var trimmedPattern = pattern.Trim();
                if (trimmedPattern.StartsWith("#")) // comment
                    continue;
                if (FileIgnoredIsMatch(relPath, trimmedPattern))
                    return true;
            }
            return false;
        }

        // Basic .gitignore-style pattern matcher
        private static bool FileIgnoredIsMatch(string relPath, string pattern)
        {
            // Normalize pattern
            pattern = pattern.Replace('\\', '/');

            // Handle root-relative pattern
            bool rootRelative = pattern.StartsWith("/");
            if (rootRelative)
                pattern = pattern.TrimStart('/');

            // Handle directory pattern (ending with /)
            bool dirPattern = pattern.EndsWith("/");
            if (dirPattern)
                pattern = pattern.TrimEnd('/');

            // Handle **
            var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace(@"\*\*", ".*") // ** matches any path segment(s)
                .Replace(@"\*", "[^/]*") // * matches any non-slash chars
                .Replace(@"\?", ".");    // ? matches any single char

            if (rootRelative)
                regexPattern = "^" + regexPattern;
            else
                regexPattern = "(^|/)" + regexPattern;

            regexPattern += dirPattern ? "/" : "$";

            return System.Text.RegularExpressions.Regex.IsMatch(relPath, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
