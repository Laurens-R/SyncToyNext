using SyncToyNext.Core;
using SyncToyNext.Core.SyncPoints;
using SyncToyNext.Core.UX;
using System;
using System.IO;

namespace SyncToyNext.Client.ExecutionModes
{
    internal class SyncPointMode
    {
        public static void ConfigureRemote(CommandLineArguments cmdArgs)
        {
            //get the current working directory
            var workingDirectory = Environment.CurrentDirectory;

            var remotePath = cmdArgs.Get("remote") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                Console.Error.WriteLine("Error: --remote flag requires a remote path.");
                Environment.Exit(1);
            }

            var remoteConfig = new RemoteConfig(remotePath, workingDirectory);
            remoteConfig.Save(workingDirectory);

            Console.WriteLine($"Succesfully configured remote at {remotePath}");
        }

        public static void RunListSyncPoints()
        {
            try
            {
                var currentDirectory = Environment.CurrentDirectory;
                var remoteConfig = RemoteConfig.Load(currentDirectory);

                if (remoteConfig == null)
                {
                    Console.Error.WriteLine("Error: Remote configuration not found. Please configure the remote first using --remote <path>.");
                    Environment.Exit(1);
                }

                var remotePath = remoteConfig.RemotePath;
                var syncPointManager = new SyncPointManager(remotePath, currentDirectory);

                var syncPoints = syncPointManager.SyncPoints;

                const int IDColumnWidth = 20;
                const int DescriptionColumnWidth = 50;

                CliHelpers.Print("ID", IDColumnWidth);
                CliHelpers.Print("Description", DescriptionColumnWidth);
                Console.WriteLine();
                CliHelpers.Print("--", IDColumnWidth);
                CliHelpers.Print("-----------", DescriptionColumnWidth);
                Console.WriteLine();

                const string NodeDescription = "(none provided)";

                foreach (var syncpoint in syncPoints)
                {
                    var description = !String.IsNullOrWhiteSpace(syncpoint.Description) ? syncpoint.Description : NodeDescription;

                    CliHelpers.Print(syncpoint.SyncPointId, IDColumnWidth);
                    CliHelpers.Print(description, DescriptionColumnWidth);
                    Console.WriteLine();
                }

            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before listing syncpoints.");
            }
        }

        public static void RunPushCommand(CommandLineArguments cmdArgs)
        {
            try
            {
                var currentDirectory = Environment.CurrentDirectory;
                var remoteConfig = RemoteConfig.Load(currentDirectory);
                var remotePath = string.Empty;

                while (remoteConfig == null)
                {
                    var parentDirectoryInfo = Directory.GetParent(currentDirectory);
                    if (parentDirectoryInfo == null)
                    {
                        currentDirectory = Environment.CurrentDirectory;
                        break;
                    }

                    currentDirectory = parentDirectoryInfo.FullName;
                    remoteConfig = RemoteConfig.Load(currentDirectory);
                }

                if (remoteConfig == null)
                {
                    Console.Error.WriteLine("Error: Remote configuration not found. Please configure the remote first using --remote <path>.");
                    Environment.Exit(1);
                }

                cmdArgs.Set("from", currentDirectory);
                cmdArgs.Set("to", remoteConfig.RemotePath);
                cmdArgs.Set("syncpoint", string.Empty);
                ManualMode.RunManual(cmdArgs);

            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before pushing.");
            }
        }

        public static void RunRestoreSyncPoint(CommandLineArguments cmdArgs)
        {
            var synpointId = cmdArgs.Get("restore");
            var singleFilename = cmdArgs.Get("file") ?? string.Empty;
            var remotePath = cmdArgs.Get("from") ?? string.Empty;

            if (synpointId == null)
            {
                UserIO.Error("Error: --restore flag requires a sync point ID.");
                return;
            }


            SyncPointRestorer.Run(synpointId, remotePath, singleFilename);
        }
    }
}
