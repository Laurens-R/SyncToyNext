/*
    STN - A file synchronization and source control solution
    Copyright (C) 2025  Laurens Ruijtenberg

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/


using Stn.Cli.ExecutionModes;
using Stn.Cli.Helpers;
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
    if(cmdArgs.EnsureValidCombination("init", "remote", "compressed")
        && cmdArgs.RequiredPresent("init", "remote")) {
        RepositoryMode.Init(cmdArgs);
    }
    if (cmdArgs.EnsureValidCombination("clone", "local", "remote", "compressed")
        && cmdArgs.RequiredPresent("clone", "local", "remote"))
    {
        RepositoryMode.Clone(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("from", "to", "syncpoint")
        && cmdArgs.RequiredPresent("from", "to"))
    {
        ManualMode.RunManual(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("restore", "from", "file")
        && cmdArgs.RequiredPresent("restore"))
    {
        RepositoryMode.RunRestoreSyncPoint(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("push", "id", "desc")
        && cmdArgs.RequiredPresent("push"))
    {
        RepositoryMode.RunPushCommand(cmdArgs);
    }
    else if (cmdArgs.EnsureValidCombination("list")
        && cmdArgs.RequiredPresent("list"))
    {
        RepositoryMode.RunListSyncPoints();
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