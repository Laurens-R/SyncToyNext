using SyncToyNext.Client;
using SyncToyNext.Client.ExecutionModes;
using System;

// Print banner with version
var version = VersionUtil.GetVersion();
Console.WriteLine("===============================================================================");
Console.WriteLine($"   SyncToyNext - Cross-Platform Sync Tool");
Console.WriteLine($"   Version: {version}");
Console.WriteLine($"   (c) {DateTime.Now.Year} Laurens Ruijtenberg");
Console.WriteLine("===============================================================================\n");

var cmdArgs = new CommandLineArguments(args);

bool isService = cmdArgs.Has("service");
bool strictMode = cmdArgs.Has("strict");
bool forceFullSync = cmdArgs.Has("recover");

try
{
    if (cmdArgs.Has("profile"))
    {
        ProfileMode.RunSpecificProfileMode(cmdArgs, strictMode);
    }
    else if (cmdArgs.Has("from"))
    {
        ManualMode.RunManual(cmdArgs);
    }
    else if (cmdArgs.Has("restore"))
    {
        SyncPointMode.RunRestoreSyncPoint(cmdArgs);
    }
    else if (cmdArgs.Has("remote"))
    {
        SyncPointMode.ConfigureRemote(cmdArgs);
    }
    else if (cmdArgs.Has("push"))
    {
        SyncPointMode.RunPushCommand(cmdArgs);
    }
    else if (cmdArgs.Has("list"))
    {
        SyncPointMode.RunListSyncPoints();
    }
    else if (cmdArgs.Has("help"))
    {
        HelpMode.RunHelp();
    }
    else if(cmdArgs.Has("service"))
    {
        ServiceMode.RunService(cmdArgs);
    }
    else
    {
        ProfileMode.RunInTaskMode(cmdArgs, strictMode, forceFullSync);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}