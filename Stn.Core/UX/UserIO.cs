using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.UX
{
    public class UserIO
    {
        private static List<string> messages = new List<string>();

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

        public static string MessageLog
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var message in messages)
                {
                    builder.AppendLine(message);
                }

                return builder.ToString();
            }
        }

        public static Action<string>? OnMessageReceivedHandler { get; set; }
        public static Action<string, string?>? OnErrorReceivedHandler { get; set; }

        public static void ClearLog()
        {
            messages.Clear();
        }

        public static void Message(string message)
        {
            var formattedMessage = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message}";
            messages.Add(formattedMessage);

            if (InConsoleMode)
            {
                Console.WriteLine(formattedMessage);
            }

            if (OnMessageReceivedHandler != null)
            {
                OnMessageReceivedHandler(message);
            }

        }

        public static void Error(string message)
        {
            var formattedMessage = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message}";
            messages.Add(formattedMessage);

            if (InConsoleMode)
            {
                Console.Error.WriteLine(formattedMessage);
            } 

            OnErrorReceivedHandler?.Invoke(message, null);
        }

        public static void Error(string message, Exception ex)
        {
            var formattedMessage = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {message} - {ex.Message}";
            messages.Add(formattedMessage);

            if (InConsoleMode)
            {
                Console.Error.WriteLine(formattedMessage);
            }

            OnErrorReceivedHandler?.Invoke(message, ex.Message);
        }
    }
}
