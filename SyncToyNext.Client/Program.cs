using SyncToyNext.Core;
using System.Threading;
#if WINDOWS
using System.Runtime.InteropServices;
using SyncToyNext.Client;
#endif

var cmdArgs = new SyncToyNext.Client.CommandLineArguments(args);

bool isService = cmdArgs.Has("service");

#if WINDOWS
string? configPath = cmdArgs.Get("config");
if (isService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    // Windows service mode
    var service = new SyncToyNextService(configPath);
    System.ServiceProcess.ServiceBase.Run(service);
    return;
}
#endif

string? configPath = cmdArgs.Get("config");

var syncContext = configPath != null ? new SyncContext(configPath) : new SyncContext();
syncContext.Start();

Console.WriteLine("SyncToyNext is running. Press 'q' to quit.");

// Use a ManualResetEventSlim to block until quit
using var quitEvent = new ManualResetEventSlim(false);

// Start a background thread to monitor for 'q' key
var keyThread = new Thread(() =>
{
    while (true)
    {
        // Console.In.Peek returns -1 if no input is available
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

Console.WriteLine("Shutting down...");
syncContext.Shutdown();
