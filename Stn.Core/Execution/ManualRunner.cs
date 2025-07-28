using Stn.Core.Synchronization;
using Stn.Core.Synchronizers;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.Execution
{
    public class ManualRunner
    {
        public static Action<int, int, string>? UpdateProgressHandler = null;

        public static void Run(string fromPath, string toPath, bool useSyncPoint = false, string syncpointId = "", string syncpointDescription = "", bool isReferencePoint = false)
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

            if(toZip && useSyncPoint)
            {
                UserIO.Error("Cannot manually specify a zip file when using syncpoints/repository mode. This is determined at repo creation. Defining a zip target is only allowed for manual 1:1 syncs between file locations.");
                Environment.Exit(1);
            }

            if (useSyncPoint && !toZip)
            {
                var syncPointManager = new SyncPointManager(toPath);
                var syncPoint = syncPointManager.AddSyncPoint(fromPath, syncpointId, syncpointDescription, isReferencePoint);

                if (syncPointManager.IsZipped && syncPointManager.SyncPointRoot != null)
                {
                    var zipFilePath = Path.Combine(toPath, syncPoint.SyncPointId, syncPointManager.SyncPointRoot.ZipFilename);
                    var zipFileSynchronizer = new ZipFileSynchronizer(zipFilePath, OverwriteOption.OnlyOverwriteIfNewer, false);
                    zipFileSynchronizer.UpdateProgressHandler = UpdateProgressHandler;
                    zipFileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
                }
                else
                {
                    var fileSynchronizer = new FileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, false);
                    fileSynchronizer.UpdateProgressHandler = UpdateProgressHandler;
                    fileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
                }

                return;
            }

            if(toZip)
            {
                var zipFileSynchronizer = new ZipFileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, false);
                zipFileSynchronizer.UpdateProgressHandler = UpdateProgressHandler;
                zipFileSynchronizer.FullSynchronization(fromPath);
            } else
            {
                var fileSynchronizer = new FileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, false);
                fileSynchronizer.UpdateProgressHandler = UpdateProgressHandler;
                fileSynchronizer.FullSynchronization(fromPath);
            }
        }
    }
}
