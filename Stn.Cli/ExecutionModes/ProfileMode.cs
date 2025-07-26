using Stn.Cli.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stn.Cli.ExecutionModes
{
    internal class ProfileMode
    {
        /// <summary>
        /// Runs the application in profile mode, synchronizing a specific profile.
        /// </summary>
        public static void RunSpecificProfileMode(CommandLineArguments cmdArgs, bool strictMode)
        {
            var profileName = cmdArgs.Get("profile") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(profileName))
            {
                Console.Error.WriteLine("Error: --profile flag requires a profile name.");
                Environment.Exit(1);
            }

            var configPath = cmdArgs.Get("config");
            var syncContext = configPath != null ? new Stn.Core.Execution.ExecutionContext(configPath, strictMode, false) : new Stn.Core.Execution.ExecutionContext(null, strictMode, false);
            syncContext.Start();
            syncContext.ManualSyncProfile(profileName);
            syncContext.Shutdown();
        }


        /// <summary>
        /// Runs the application in task mode, continuously synchronizing profiles.
        /// </summary>
        public static void RunInTaskMode(CommandLineArguments cmdArgs, bool strictMode, bool forceFullSync)
        {
            var configPath = cmdArgs.Get("config");

            if (forceFullSync)
            {
                Stn.Core.SyncConfiguration.RemoveCleanShutdownMarker();
            }

            var syncContext = configPath != null ? new Stn.Core.Execution.ExecutionContext(configPath, strictMode) : new Stn.Core.Execution.ExecutionContext(null, strictMode);
            syncContext.Start();

            Console.WriteLine("SyncToyNext is running. Press 'q' to quit.");
            var quitEvent = new ManualResetEventSlim(false);

            // Start a background thread to monitor for 'q' key
            var keyThread = new Thread(() =>
            {
                while (!quitEvent.IsSet)
                {
                    if (Console.In.Peek() != -1)
                    {
                        var keyChar = (char)Console.In.Read();
                        if (char.ToLowerInvariant(keyChar) == 'q')
                        {
                            quitEvent.Set();
                            break;
                        }
                    }
                    Thread.Sleep(100); // Reduce CPU usage
                }
            });

            keyThread.IsBackground = true;
            keyThread.Start();

            // Wait for quit signal
            quitEvent.Wait();

            // Ensure the keyThread exits if not already
            if (keyThread.IsAlive) keyThread.Join(500);

            Console.WriteLine("Shutting down...");
            syncContext.Shutdown();
        }
    }
}
