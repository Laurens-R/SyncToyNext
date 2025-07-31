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


using Stn.Cli.Helpers;
using Stn.Core.Execution;
using Stn.Core.UX;
using System;

namespace Stn.Cli.ExecutionModes
{
    internal class ManualMode
    {
        /// <summary>
        /// Runs a manual synchronization between two specified paths.
        /// </summary>
        public static void RunManual(CommandLineArguments cmdArgs)
        {
            var fromPath = cmdArgs.Get("from");
            var toPath = cmdArgs.Get("to");
            var useSyncPoint = cmdArgs.Has("syncpoint");
            var syncpointId = cmdArgs.Get("id") ?? string.Empty;
            var syncpointDescription = cmdArgs.Get("desc") ?? string.Empty;

            if (String.IsNullOrEmpty(fromPath) || String.IsNullOrEmpty(toPath))
            {
                UserIO.Error("Error: --from and --to flags must both be specified.");
                return;
            }

            ManualRunner.Run(fromPath, toPath, useSyncPoint, syncpointId, syncpointDescription);
        }
    }
}
