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

using System;
using System.Collections.Generic;

namespace Stn.Cli.Helpers
{
    /// <summary>
    /// Parses and stores command line arguments as key-value pairs, supporting both flags and string parameters.
    /// </summary>
    public class CommandLineArguments
    {
        private readonly Dictionary<string, string?> _args = new(StringComparer.OrdinalIgnoreCase);

        public CommandLineArguments(string[] args)
        {
            OriginalArgs = args;

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

        public string[] OriginalArgs { get; private set; }

        /// <summary>
        /// Gets the value for a given key, or null if not present.
        /// </summary>
        public string? Get(string key) => _args.TryGetValue(key, out var value) ? value : null;

        /// <summary>
        /// Checks if a flag/parameter is present.
        /// </summary>
        public bool Has(string key) => _args.ContainsKey(key);

        public void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            }
            _args[key] = value;
        }

        /// <summary>
        /// Ensures that all present arguments are among the allowed set. Returns true if valid, false otherwise.
        /// </summary>
        /// <param name="allowedKeys">The set of allowed argument keys.</param>
        /// <returns>True if all present arguments are in the allowed set, false otherwise.</returns>
        public bool EnsureValidCombination(params string[] allowedKeys)
        {
            if (allowedKeys == null || allowedKeys.Length == 0)
                return _args.Count == 0; // If no allowed keys, only valid if no args present

            var allowedSet = new HashSet<string>(allowedKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var key in _args.Keys)
            {
                if (!allowedSet.Contains(key))
                    return false;
            }
            return true;
        }

        public bool RequiredPresent(params string[] requiredKeys)
        {
            if (requiredKeys == null || requiredKeys.Length == 0)
                return _args.Count == 0; // If no allowed keys, only valid if no args present

            if(requiredKeys.Length > _args.Count) return false;

            foreach(var requiredKey in requiredKeys)
            {
                if (!_args.Keys.Contains(requiredKey)) return false;
            }

            return true;
        }

        public bool Any()
        {
            return _args.Count > 0;
        }
    }
}
