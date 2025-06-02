using System;
using System.ServiceProcess;
using SyncToyNext.Core;

namespace SyncToyNext.Client
{
    /// <summary>
    /// Windows Service wrapper for SyncToyNext.
    /// </summary>
    public class SyncToyNextService : ServiceBase
    {
        private SyncContext? _syncContext;
        private string? _configPath;

        public SyncToyNextService(string? configPath = null)
        {
            _configPath = configPath;
        }

        protected override void OnStart(string[] args)
        {
            // Try to get config from args if not set
            if (_configPath == null && args != null && args.Length > 0)
            {
                var cmdArgs = new SyncToyNext.Client.CommandLineArguments(args);
                _configPath = cmdArgs.Get("config");
            }
            _syncContext = _configPath != null ? new SyncContext(_configPath) : new SyncContext();
            _syncContext.Start();
        }

        protected override void OnStop()
        {
            _syncContext?.Shutdown();
        }
    }
}
