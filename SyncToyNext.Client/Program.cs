using SyncToyNext.Core;
using SyncToyNext.Client; // For VersionUtil
using System.Reflection;
using System;
using System.Threading;
using System.IO;

// Print banner with version
PrintBanner();

var cmdArgs = new CommandLineArguments(args);

bool isService = cmdArgs.Has("service");
bool strictMode = cmdArgs.Has("strict");
bool forceFullSync = cmdArgs.Has("recover");

MainProgramEntry(cmdArgs, strictMode, forceFullSync);

void PrintBanner()
{
    var version = VersionUtil.GetVersion();
    Console.WriteLine("===============================================================================");
    Console.WriteLine($"   SyncToyNext - Cross-Platform Sync Tool");
    Console.WriteLine($"   Version: {version}");
    Console.WriteLine($"   (c) {DateTime.Now.Year} Laurens Ruijtenberg");
    Console.WriteLine("===============================================================================\n");
}

/// <summary>
/// Runs the application in profile mode, synchronizing a specific profile.
/// </summary>
static void RunSpecificProfileMode(CommandLineArguments cmdArgs, bool strictMode)
{
    var profileName = cmdArgs.Get("profile") ?? string.Empty;
    if (string.IsNullOrWhiteSpace(profileName))
    {
        Console.Error.WriteLine("Error: --profile flag requires a profile name.");
        Environment.Exit(1);
    }

    var configPath = cmdArgs.Get("config");
    var syncContext = configPath != null ? new SyncContext(configPath, strictMode, false) : new SyncContext(null, strictMode, false);
    syncContext.Start();
    syncContext.ManualSyncProfile(profileName);
    syncContext.Shutdown();
}

/// <summary>
/// Runs the application in task mode, continuously synchronizing profiles.
/// </summary>
static void RunInTaskMode(CommandLineArguments cmdArgs, bool strictMode, bool forceFullSync)
{
    var configPath = cmdArgs.Get("config");

    if (forceFullSync)
    {
        SyncToyNext.Core.SyncConfiguration.RemoveCleanShutdownMarker();
    }

    var syncContext = configPath != null ? new SyncContext(configPath, strictMode) : new SyncContext(null, strictMode);
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

/// <summary>
/// Runs a manual synchronization between two specified paths.
/// </summary>
static void RunManual(CommandLineArguments cmdArgs)
{
    var fromPath = cmdArgs.Get("from");
    var toPath = cmdArgs.Get("to");
    var useSyncPoint = cmdArgs.Has("syncpoint");
    var syncpointId = cmdArgs.Get("id") ?? string.Empty;
    var syncpointDescription = cmdArgs.Get("desc") ?? string.Empty;

    if(String.IsNullOrEmpty(fromPath) || String.IsNullOrEmpty(toPath))
    {
        UserIO.Error("Error: --from and --to flags must both be specified.");
        return;
    }

    ManualRun.Run(fromPath, toPath, useSyncPoint, syncpointId, syncpointDescription);
}

static void RunRestoreSyncPoint(CommandLineArguments cmdArgs)
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

static void ConfigureRemote(CommandLineArguments cmdArgs)
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

static void RunPushCommand(CommandLineArguments cmdArgs)
{
    try
    {
        var currentDirectory = Environment.CurrentDirectory;
        var remoteConfig = RemoteConfig.Load(currentDirectory);
        var remotePath = string.Empty;

        while(remoteConfig == null)
        {
            var parentDirectoryInfo = Directory.GetParent(currentDirectory);
            if(parentDirectoryInfo ==  null)
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
        RunManual(cmdArgs);

    }
    catch (Exception)
    {
        Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before pushing.");
    }
}

static void Print(string message, int columnWidth = 50)
{
    if (message.Length > columnWidth)
    {
        message = message.Substring(0, columnWidth - 3) + "...";
    }
    Console.Write($"{message}");
    for(int i = message.Length; i < columnWidth; i++)
    {
        Console.Write(" ");
    }
}

static void RunListSyncPoints()
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

        Print("ID", IDColumnWidth);
        Print("Description", DescriptionColumnWidth);
        Console.WriteLine();
        Print("--", IDColumnWidth);
        Print("-----------", DescriptionColumnWidth);
        Console.WriteLine();

        const string NodeDescription = "(none provided)";

        foreach (var syncpoint in syncPoints)
        {
            var description = !String.IsNullOrWhiteSpace(syncpoint.Description) ? syncpoint.Description : NodeDescription;

            Print(syncpoint.SyncPointId, IDColumnWidth);
            Print(description, DescriptionColumnWidth);
            Console.WriteLine();
        }

    }
    catch (Exception)
    {
        Console.Error.WriteLine($"Error loading remote config. Make sure the remote has been configured before listing syncpoints.");
    }
}

static void RunService(CommandLineArguments cmdArgs)
{
    if (OperatingSystem.IsWindows())
    {
        var service = new SyncToyNextService(cmdArgs.Get("config"));
        System.ServiceProcess.ServiceBase.Run(service);
    }
    else
    {
        Console.Error.WriteLine("Error: Service mode is only supported on Windows.");
        Environment.Exit(1);
    }
}

static void RunHelp()
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
    
    Console.WriteLine("Pushing changes in folder (requires configured remote location):");
    Console.WriteLine("  --push                 Push changes to the remote path.");
    Console.WriteLine("  --id                   (Optional) ID of the to be generated syncpoint. Otherwise is autogenerated.");
    Console.WriteLine("  --desc                 (Optional) Description of the syncpoint. Could be changelog.");
    
    Console.WriteLine("Restoring a sync point:");
    Console.WriteLine("  --restore <id>         Restore a sync point by ID.");
    Console.WriteLine("  --from <path>          (Optional if no remote configured) Specify the source path for restoring a sync point.");
    Console.WriteLine("  --file <path>          (Optional relative path) Specify a specific file to restore.");
    Console.WriteLine();
    
    Console.WriteLine("Configure syncpoint remote for current directory:");
    Console.WriteLine("  --remote <path>        Configure a remote path for syncpoint operations.");
    Console.WriteLine();
    Console.WriteLine("Listing all sync points for the current location (requires configured remote location):");
    Console.WriteLine("  --list                 List all sync points.");
}

static void MainProgramEntry(CommandLineArguments cmdArgs, bool strictMode, bool forceFullSync)
{
    try
    {
        if (cmdArgs.Has("profile"))
        {
            RunSpecificProfileMode(cmdArgs, strictMode);
        }
        else if (cmdArgs.Has("from"))
        {
            RunManual(cmdArgs);
        }
        else if (cmdArgs.Has("restore"))
        {
            RunRestoreSyncPoint(cmdArgs);
        }
        else if (cmdArgs.Has("remote"))
        {
            ConfigureRemote(cmdArgs);
        }
        else if (cmdArgs.Has("push"))
        {
            RunPushCommand(cmdArgs);
        }
        else if (cmdArgs.Has("list"))
        {
            RunListSyncPoints();
        }
        else if (cmdArgs.Has("help"))
        {
            RunHelp();
        }
        else if(cmdArgs.Has("service"))
        {
            RunService(cmdArgs);
        }
        else
        {
            RunInTaskMode(cmdArgs, strictMode, forceFullSync);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.Exit(1);
    }
}

