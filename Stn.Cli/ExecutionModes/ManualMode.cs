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
