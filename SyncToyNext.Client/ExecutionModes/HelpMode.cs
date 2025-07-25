﻿using System;

namespace SyncToyNext.Client.ExecutionModes
{
    internal class HelpMode
    {
        public static void RunHelp()
        {
            Console.WriteLine("Usage: stn [options]");
            Console.WriteLine("Options:");

            //print overview of logical option combinations in groupings as per the options etc in the main program entry
            Console.WriteLine();
            Console.WriteLine("General Options:");
            Console.WriteLine("  --help                 Show this help message.");
            Console.WriteLine("  --config <file>        Specify a custom configuration file path.");
            Console.WriteLine("  --strict               Enable strict mode for synchronization (Checksum validation for differences)");
            Console.WriteLine("  --recover              Force a full sync to recover from an interrupted state.");
            Console.WriteLine("  --service              Run the application as a Windows service (Windows only).");
            Console.WriteLine();

            Console.WriteLine("Running a specific profile manually:");
            Console.WriteLine("  --profile <name>       Run a specific profile in manual mode.");
            Console.WriteLine();

            Console.WriteLine("Manual Sync:");
            Console.WriteLine("  --from <path>          Specify the source path for manual sync.");
            Console.WriteLine("  --to <path>            Specify the destination path for manual sync.");
            Console.WriteLine("  --syncpoint            Indicate that this sync should create a syncpoint.");
            Console.WriteLine();

            Console.WriteLine("Repository help:");
            Console.WriteLine("----------------");
            Console.WriteLine();
            Console.WriteLine("Repositories are always setup in a local/remote pair. The local location is where you work. The remote");
            Console.WriteLine("is where the syncpoints live. Syncpoints are individual check-ins of code due to manual pushes or as part");
            Console.WriteLine("a merge. In other words the 'local folder' is the working directory, where the remote location contains");
            Console.WriteLine("the actual repo. These are kept seperate on purpose to ensure that an incident in the working folder does");
            Console.WriteLine("not affect the repo itself. The actions below are similar to other source control systems out there.");
            Console.WriteLine();

            Console.WriteLine("Initializing a local/remote pair repository in the current directory:");
            Console.WriteLine("  --init                 The flag to indicate that you are requesting an init");
            Console.WriteLine("  --remote               The remote to initialize/pair against. Local folder content");
            Console.WriteLine("                         will be cleared if remote has syncpoints. To avoid remote corruption.");
            Console.WriteLine();

            Console.WriteLine("Clone an existing remote into a new local/remote pair repository:");
            Console.WriteLine("  --clone                The remote to clone from.");
            Console.WriteLine("  --local                The (relative) foldername to clone into.");
            Console.WriteLine("  --remote               The new remote for the new local/remote pair.");
            Console.WriteLine("                         will be cleared if remote has syncpoints. To avoid remote corruption.");
            Console.WriteLine();

            Console.WriteLine("Pushing changes in folder (requires configured remote location):");
            Console.WriteLine("  --push                 Push changes to the remote path.");
            Console.WriteLine("  --id                   (Optional) ID of the to be generated syncpoint. Otherwise is autogenerated.");
            Console.WriteLine("  --desc                 (Optional) Description of the syncpoint. Could be changelog.");
            Console.WriteLine();

            Console.WriteLine("Restoring a sync point:");
            Console.WriteLine("  --restore <id>         Restore a sync point by ID.");
            Console.WriteLine("  --file <path>          (Optional relative path) Specify a specific file to restore.");
            Console.WriteLine();

            Console.WriteLine("Listing all sync points for the current location (requires configured remote location):");
            Console.WriteLine("  --list                 List all sync points.");
        }
    }
}
