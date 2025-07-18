using System;

namespace SyncToyNext.Client.ExecutionModes
{
    internal class ServiceMode
    {
        public static void RunService(CommandLineArguments cmdArgs)
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

    }
}
