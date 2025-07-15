using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{
    public class ManualRun
    {
        public static void Run(CommandLineArguments cmdArgs)
        {
            string? fromPath = null;
            string? toPath = null;

            if (cmdArgs.Has("from"))
            {
                fromPath = cmdArgs.Get("from");
            }

            if (cmdArgs.Has("to"))
            {
                toPath = cmdArgs.Get("to");
            }
            else
            {
                Console.Error.WriteLine("Error: --to flag is required for manual sync.");
                Environment.Exit(1);
            }

            bool useSyncPoint = cmdArgs.Has("syncpoint");

            if (String.IsNullOrWhiteSpace(fromPath)
            || string.IsNullOrWhiteSpace(toPath))
                {
                    Console.Error.WriteLine("Error: --from and --to flags are required for manual sync.");
                    Environment.Exit(1);
                }

            if (!Path.Exists(fromPath)
                && (!Path.Exists(toPath)
                    || (!Directory.Exists(toPath)
                        && Path.HasExtension(toPath)
                        && Path.GetExtension(toPath).ToLowerInvariant() == "zip")
                    )
            )
            {
                Console.Error.WriteLine("Error: Both source and destination paths must be directories for manual sync. Or the destination must be a zip file");
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

                var syncPointID = cmdArgs.Get("id") ?? string.Empty;
                var description = cmdArgs.Get("desc") ?? string.Empty;

                syncPoint = syncPointManager.AddSyncPoint(fromPath, syncPointID, description);
            }

            if(toZip)
            {
                var zipFileSynchronizer = new ZipFileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, new Logger(fromPath), false);
                zipFileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
            } else
            {
                var fileSynchronizer = new FileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, new Logger(fromPath), false);
                fileSynchronizer.FullSynchronization(fromPath, syncPoint, syncPointManager);
            }
        }
    }
}
