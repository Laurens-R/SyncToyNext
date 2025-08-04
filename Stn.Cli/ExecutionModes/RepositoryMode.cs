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
using Stn.Core;
using Stn.Core.SyncPoints;
using Stn.Core.UX;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Stn.Cli.ExecutionModes
{
    internal class RepositoryMode
    {

        public static async Task Init(CommandLineArguments cmdArgs)
        {
            try
            {
                var remotePath = cmdArgs.Get("remote");
                var localFolder = Environment.CurrentDirectory;
                var compressed = cmdArgs.Has("compressed");

                if (String.IsNullOrEmpty(remotePath) || !Path.Exists(remotePath)) throw new InvalidOperationException("Provided remote path not valid");

                await Repository.Initialize(localFolder, remotePath, compressed);
            }
            catch (Exception ex)
            {
                UserIO.Error(ex.Message);
            }
        }

        public static async Task Clone(CommandLineArguments cmdArgs)
        {
            try
            {
                var clonePath = cmdArgs.Get("clone");
                var newRemotePath = cmdArgs.Get("remote");
                var relativeLocalFolderName = cmdArgs.Get("local");
                var isCompressed = cmdArgs.Has("compressed");

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

                await Repository.CloneFromOtherRemote(localDirectory, newRemotePath, clonePath, isCompressed);
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

        public static async Task RunPushCommand(CommandLineArguments cmdArgs)
        {
            try
            {
                var repo = new Repository(Environment.CurrentDirectory);

                var syncPointID = cmdArgs.Get("id") ?? string.Empty;
                var syncPointDesc = cmdArgs.Get("desc") ?? string.Empty;

                await repo.Push(syncPointID, syncPointDesc);
            }
            catch (Exception)
            {
                Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before pushing.");
            }
        }

        public static async Task RunRestoreSyncPoint(CommandLineArguments cmdArgs)
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
                await repo.RestoreSingleFile(synpointId, singleFilename);
            }
        }
    }
}
