using SyncToyNext.Client.Helpers;
using SyncToyNext.Core;
using SyncToyNext.Core.SyncPoints;
using SyncToyNext.Core.UX;
using System;
using System.IO;

namespace SyncToyNext.Client.ExecutionModes
{
    internal class RepositoryMode
    {

        public static void Init(CommandLineArguments cmdArgs)
        {
            try
            {
                var remotePath = cmdArgs.Get("remote");
                var localFolder = Environment.CurrentDirectory;
                var compressed = cmdArgs.Has("compressed");

                if (String.IsNullOrEmpty(remotePath) || !Path.Exists(remotePath)) throw new InvalidOperationException("Provided remote path not valid");

                Repository.Initialize(localFolder, remotePath, compressed);
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        public static void Clone(CommandLineArguments cmdArgs)
        {
            try
            {
                var clonePath = cmdArgs.Get("clone");
                var newRemotePath = cmdArgs.Get("remote");
                var relativeLocalFolderName = cmdArgs.Get("local");

                if (String.IsNullOrWhiteSpace(clonePath) || !Path.Exists(clonePath)) throw new InvalidOperationException("Provided clone source path not valid");
                if (String.IsNullOrWhiteSpace(newRemotePath) || !Path.Exists(newRemotePath)) throw new InvalidOperationException("Provided remote path not valid");
                if (String.IsNullOrWhiteSpace(relativeLocalFolderName)) throw new InvalidOperationException("Provided remote path not valid");

                //we keep this directory creation logic seperate in the console tool, because this depends
                //on the desired UX experience from the perspective of the tool that is being used.
                //(console vs gui).
                var localDirectory = Path.Combine(Environment.CurrentDirectory, relativeLocalFolderName);
                if(!Path.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                Repository.CloneFromOtherRemote(localDirectory, newRemotePath, clonePath);
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        public static void RunListSyncPoints()
        {
            try
            {
                var repo = new Repository(Environment.CurrentDirectory);
                var syncPoints = repo.SyncPoints;

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
                var repo = new Repository(Environment.CurrentDirectory);

                var syncPointID = cmdArgs.Get("id") ?? string.Empty;
                var syncPointDesc = cmdArgs.Get("desc") ?? string.Empty;

                repo.Push(syncPointID, syncPointDesc);
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

            if (synpointId == null)
            {
                UserIO.Error("Error: --restore flag requires a sync point ID.");
                return;
            }

            var repo = new Repository(Environment.CurrentDirectory);

            if(!String.IsNullOrEmpty(singleFilename))
            {
                repo.RestoreSingleFile(synpointId, singleFilename);
            }
        }
    }
}
