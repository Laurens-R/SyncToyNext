using System;
using System.ServiceProcess;
using System.Threading;
using SyncToyNext.Client.Helpers;
using SyncToyNext.Core;

namespace SyncToyNext.Client.ExecutionModes
{
    /// <summary>
    /// Windows Service wrapper for SyncToyNext.
    /// </summary>
    public class SyncToyNextService : ServiceBase
    {
        private Core.ExecutionContext? _syncContext;
        private string? _configPath;

        public SyncToyNextService(string? configPath = null)
        {
            //for now it doesn't matter if this is set to null or not, because
            //the sync context will try to load the config from the default location
            //if this is set to null.
            _configPath = configPath;
        }

        protected override void OnStart(string[] args)
        {
            // Try to get config from args if not set
            if (_configPath == null && args != null && args.Length > 0)
            {
                var cmdArgs = new CommandLineArguments(args);
                _configPath = cmdArgs.Get("config");
            }
            _syncContext = _configPath != null ? new Core.ExecutionContext(_configPath) : new Core.ExecutionContext();
            _syncContext.Start();
        }

        protected override void OnStop()
        {
            _syncContext?.Shutdown();
        }
    }
}
