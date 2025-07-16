using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core
{
    public class UserIO
    {
        protected static bool InConsoleMode
        {
            get
            {
                // we can check if we are in a console mode by trying to access the console width.
                // If this throws an exception, we are not in a console mode.
                // Not sure if this is the best way, but it works for now.
                try
                {
                    var consoleWidth = Console.WindowWidth;
                    return true;
                } catch
                {
                    return false;
                }
            }
        }

        public static Action<string>? OnMessageReceivedHandler { get; set; }
        public static Action<string, string?>? OnErrorReceivedHandler { get; set; }

        public static void Message(string message)
        {
            if (InConsoleMode)
            {
                Console.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message}");
            }

            if (OnMessageReceivedHandler != null)
            {
                OnMessageReceivedHandler(message);
            }

        }

        public static void Error(string message)
        {
            if (InConsoleMode)
            {
                Console.Error.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message}");
            }

            OnErrorReceivedHandler?.Invoke(message, null);
        }

        public static void Error(string message, Exception ex)
        {
            if (InConsoleMode)
            {
                Console.Error.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message} - {ex.Message}");
            }

            OnErrorReceivedHandler?.Invoke(message, ex.Message);
        }
    }
}
