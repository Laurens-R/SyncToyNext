using SyncToyNext.Core.Models;
using SyncToyNext.Core.Synchronizers;
using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.Runners
{
    public class ManualRunner
    {
        public static void Run(string fromPath, string toPath, bool useSyncPoint = false, string syncpointId = "", string syncpointDescription = "")
        {

            if (string.IsNullOrWhiteSpace(fromPath)
            || string.IsNullOrWhiteSpace(toPath))
                {
                UserIO.Error("Error: --from and --to flags are required for manual sync.");
                    Environment.Exit(1);
                }

            if (!Path.Exists(fromPath)
                && (!Path.Exists(toPath)
                    || !Directory.Exists(toPath)
                        && Path.HasExtension(toPath)
                        && Path.GetExtension(toPath).ToLowerInvariant() == "zip"
                    )
            )
            {
                UserIO.Error("Error: Both source and destination paths must be directories for manual sync. Or the destination must be a zip file");
                Environment.Exit(1);
            }

            bool toZip = false;
            string targetName = Path.GetFileName(toPath);
            string? parentPath = Path.GetDirectoryName(toPath);

            if (Path.HasExtension(toPath) && Path.GetExtension(toPath).ToLowerInvariant() == ".zip")
            {
                toZip = true;
            }

            SyncPointManager? syncPointManager = null;
            SyncPoint? syncPoint = null;

            if (useSyncPoint)
            {
                syncPointManager = new SyncPointManager(toPath, fromPath);
                syncPoint = syncPointManager.AddSyncPoint(fromPath, syncpointId, syncpointDescription);
            }

            if(toZip)
            {
                var zipFileSynchronizer = new ZipFileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, false);
                zipFileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
            } else
            {
                var fileSynchronizer = new FileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, false);
                fileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
            }
        }
    }
}
