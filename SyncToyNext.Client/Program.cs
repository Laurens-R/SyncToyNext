using SyncToyNext.Core;
using SyncToyNext.Client; // For VersionUtil
using System.Reflection;
using System;
using System.Threading;

#if WINDOWS
using System.Runtime.InteropServices;
using SyncToyNext.Client;
using System.ServiceProcess;
#endif

// Print banner with version
PrintBanner();

var cmdArgs = new CommandLineArguments(args);

bool isService = cmdArgs.Has("service");
bool strictMode = cmdArgs.Has("strict");
bool forceFullSync = cmdArgs.Has("recover");


#if WINDOWS
string? configPath = cmdArgs.Get("config");
if (isService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows service mode
    var service = new SyncToyNextService(configPath);
    System.ServiceProcess.ServiceBase.Run(service);
    return;
}

// Service shutdown logic should happen here, before any sync logic to ensure
// that the service is not running while we perform sync operations.
// This prevents conflicts with the service's own sync operations.
ServiceController? synctoyService = null;
bool wasServiceRunning = false;
if (!isService)
{
    try
    {
        synctoyService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals("SyncToyNext", StringComparison.OrdinalIgnoreCase));
        if (synctoyService != null && synctoyService.Status == ServiceControllerStatus.Running)
        {
            wasServiceRunning = true;
            Console.WriteLine("Stopping SyncToyNext service...");
            synctoyService.Stop();
            synctoyService.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            Console.WriteLine("SyncToyNext service stopped.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: Could not check/stop SyncToyNext service: {ex.Message}");
        Console.WriteLine("Exiting to prevent conflicting sync actions.");
        Environment.Exit(1);
    }
    // If the service is still running after the attempt, exit
    if (synctoyService != null && synctoyService.Status == ServiceControllerStatus.Running)
    {
        Console.WriteLine("Error: SyncToyNext service is still running and could not be stopped. Exiting to prevent conflicts.");
        Environment.Exit(1);
    }
}
#endif

MainProgramEntry(cmdArgs, strictMode, forceFullSync);

// If running as a service, restart the service if it was running before
// This to ensure that the service doesn't conflict with the console app
#if WINDOWS
if (!isService && wasServiceRunning && synctoyService != null)
{
    try
    {
        Console.WriteLine("Restarting SyncToyNext service...");
        synctoyService.Start();
        synctoyService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
        Console.WriteLine("SyncToyNext service restarted.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not restart SyncToyNext service: {ex.Message}");
    }
}
#endif

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

    string? configPath = cmdArgs.Get("config");
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
    string? configPath = cmdArgs.Get("config");

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
    ManualRun.Run(cmdArgs);
}

static void RunRestoreSyncPoint(CommandLineArguments cmdArgs)
{
    SyncPointRestorer.Run(cmdArgs);
}

static void MainProgramEntry(CommandLineArguments cmdArgs, bool strictMode, bool forceFullSync)
{
    if (cmdArgs.Has("profile"))
    {
        RunSpecificProfileMode(cmdArgs, strictMode);
    }
    else if (cmdArgs.Has("from"))
    {
        RunManual(cmdArgs);
    } 
    else if(cmdArgs.Has("restore-syncpoint"))
    {

    }
    else
    {
        RunInTaskMode(cmdArgs, strictMode, forceFullSync);
    }
}