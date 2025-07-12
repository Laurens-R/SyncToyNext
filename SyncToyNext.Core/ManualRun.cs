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
        public static void Run(string? fromPath, string? toPath)
        {
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

            if(Path.HasExtension(toPath) && Path.GetExtension(toPath).ToLowerInvariant() == ".zip")
            {
                toZip = true;
            }

            if(toZip)
            {
                var zipFileSynchronizer = new ZipFileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, new Logger(fromPath), false);
                zipFileSynchronizer.FullSynchronization(fromPath);
            } else
            {
                var fileSynchronizer = new FileSynchronizer(toPath, OverwriteOption.OnlyOverwriteIfNewer, new Logger(fromPath), false);
                fileSynchronizer.FullSynchronization(fromPath);
            }
        }
    }
}
