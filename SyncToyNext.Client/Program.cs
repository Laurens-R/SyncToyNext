using SyncToyNext.Client.ExecutionModes;
using SyncToyNext.Client.Helpers;
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
    if (cmdArgs.EnsureValidCombination("profile") 
        && cmdArgs.RequiredPresent("profile"))
    {
        ProfileMode.RunSpecificProfileMode(cmdArgs, strictMode);
    }
    else if (cmdArgs.EnsureValidCombination("from", "to", "syncpoint")
        && cmdArgs.RequiredPresent("from", "to"))
    {
        ManualMode.RunManual(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("restore", "from", "file")
        && cmdArgs.RequiredPresent("restore"))
    {
        SyncPointMode.RunRestoreSyncPoint(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("remote")
        && cmdArgs.RequiredPresent("remote"))
    {
        SyncPointMode.ConfigureRemote(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("push", "id", "desc")
        && cmdArgs.RequiredPresent("push"))
    {
        SyncPointMode.RunPushCommand(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("list")
        && cmdArgs.RequiredPresent("list"))
    {
        SyncPointMode.RunListSyncPoints();
    }
    else if (cmdArgs.EnsureValidCombination("help") 
        && cmdArgs.RequiredPresent("help"))
    {
        HelpMode.RunHelp();
    }
    else if(cmdArgs.EnsureValidCombination("service", "config")
        && cmdArgs.RequiredPresent("service"))
    {
        ServiceMode.RunService(cmdArgs);
    }
    else if(!cmdArgs.Any() || cmdArgs.EnsureValidCombination("config", "strict", "recover"))
    {
        ProfileMode.RunInTaskMode(cmdArgs, strictMode, forceFullSync);
    } else
    {
        Console.WriteLine("No valid combination of arguments provided.");
        Console.WriteLine();
        HelpMode.RunHelp();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}