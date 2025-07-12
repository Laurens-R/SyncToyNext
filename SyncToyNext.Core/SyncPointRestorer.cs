using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{
    public class SyncPointRestorer
    {
        public static void Run(CommandLineArguments cmdArgs)
        {
            var syncPointID = cmdArgs.Get("restore-syncpoint");
            var fromPath = cmdArgs.Get("from");

            if (string.IsNullOrWhiteSpace(syncPointID) || string.IsNullOrWhiteSpace(fromPath))
            {
                throw new InvalidOperationException("No values provided for sync point and from- location");
            }

            if (!Path.Exists(fromPath))
            {
                throw new InvalidOperationException($"The provided path '{fromPath}' does not exist.");
            }

            if (!Path.Exists(Path.Combine(fromPath, "syncpointroot.json" ))) {
                throw new InvalidOperationException($"The provided path '{fromPath}' does not contain a valid sync point root file.");
            }

            SyncPointManager syncPointManager = new SyncPointManager(fromPath);

            var syncPoint = syncPointManager.GetSyncPoint(syncPointID);

            if(syncPoint == null)
            {
                throw new InvalidOperationException($"Sync point with ID '{syncPointID}' not found in the specified path.");
            }

            bool isZipped = syncPointManager.IsZipped;

            var allFiles = syncPointManager.GetFileEntriesAtSyncpoint(syncPointID);
            
            foreach(var file in allFiles)
            {
                var restorePath = file.SourcePath;

                if (isZipped)
                {
                    var zipFile = file.RelativeRemotePath.Split("@")[1];
                    var relativeEntry = file.RelativeRemotePath.Split("@")[0];

                    using var zip = new FileStream(zipFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                    using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                    var entryPath = relativeEntry.Replace("\\", "/");
                    var zipEntry = archive.GetEntry(entryPath);

                    var restoreDirectory = Path.GetDirectoryName(restorePath);

                    if (restoreDirectory != null && !Directory.Exists(restoreDirectory))
                    {
                        Directory.CreateDirectory(restoreDirectory);
                    }

                    if (zipEntry == null)
                    {
                        throw new InvalidOperationException($"The zip entry '{entryPath}' does not exist in the zip file '{zipFile}'.");
                    }

                    zipEntry.ExtractToFile(restorePath, true);
                }
                else
                {
                    restorePath = Path.Combine(fromPath, file.RelativeRemotePath);
                    var syncPointPath = Path.Combine(fromPath, syncPointID, file.RelativeRemotePath);

                    if (!File.Exists(syncPointPath))
                    {
                        throw new InvalidOperationException($"The remote sync point file '{syncPointPath}' does not exist.");
                    }

                    var restoreDirectory = Path.GetDirectoryName(restorePath);

                    if (restoreDirectory != null && !Directory.Exists(restoreDirectory))
                    {
                        Directory.CreateDirectory(restoreDirectory);
                    }

                    File.Copy(syncPointPath, restorePath, true);
                }
            }

        }
    }
}
