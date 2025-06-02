using System;
using System.Collections.Generic;

namespace SyncToyNext.Client
{
    /// <summary>
    /// Parses and stores command line arguments as key-value pairs, supporting both flags and string parameters.
    /// </summary>
    public class CommandLineArguments
    {
        private readonly Dictionary<string, string?> _args = new(StringComparer.OrdinalIgnoreCase);

        public CommandLineArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--"))
                {
                    var key = arg.Substring(2);
                    string? value = null;
                    // Support --key value or --key=value
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        value = args[i + 1];
                        i++;
                    }
                    else if (key.Contains('='))
                    {
                        var split = key.Split('=', 2);
                        key = split[0];
                        value = split[1];
                    }
                    _args[key] = value;
                }
            }
        }

        /// <summary>
        /// Gets the value for a given key, or null if not present.
        /// </summary>
        public string? Get(string key) => _args.TryGetValue(key, out var value) ? value : null;

        /// <summary>
        /// Checks if a flag/parameter is present.
        /// </summary>
        public bool Has(string key) => _args.ContainsKey(key);
    }
}
