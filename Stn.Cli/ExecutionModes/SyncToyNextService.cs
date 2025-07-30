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

using System;
using System.ServiceProcess;
using System.Threading;
using Stn.Cli.Helpers;

namespace Stn.Cli.ExecutionModes
{
    /// <summary>
    /// Windows Service wrapper for Stn.
    /// </summary>
    public class SyncToyNextService : ServiceBase
    {
        private Core.Execution.ExecutionContext? _syncContext;
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
            _syncContext = _configPath != null ? new Core.Execution.ExecutionContext(_configPath) : new Core.Execution.ExecutionContext();
            _syncContext.Start();
        }

        protected override void OnStop()
        {
            _syncContext?.Shutdown();
        }
    }
}
