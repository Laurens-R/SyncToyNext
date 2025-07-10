using System;
using System.IO;

namespace SyncToyNext.Core
{
    /// <summary>
    /// Provides logging functionality for sync operations. Logs to a file in the source directory and to the console.
    /// </summary>
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();

        public Logger(string sourcePath)
        {
            var logDir = Path.Combine(sourcePath, "synclogs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"SyncToyNext_{DateTime.UtcNow:yyyyMMdd}.log");
        }

        public void Log(string message)
        {
            var logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            Console.WriteLine(logLine);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var errorLine = $"[ERROR] {message}" + (ex != null ? $" Exception: {ex.Message}" : "");
            Log(errorLine);
        }

        public void LogSyncAction(string filePath, string action, string? details = null)
        {
            var msg = $"{action}: {filePath}" + (details != null ? $" ({details})" : "");
            Log(msg);
        }
    }
}
