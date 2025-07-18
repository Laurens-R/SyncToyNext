using System;

namespace SyncToyNext.Client
{
    internal class CliHelpers
    {
        public static void Print(string message, int columnWidth = 50)
        {
            if (message.Length > columnWidth)
            {
                message = message.Substring(0, columnWidth - 3) + "...";
            }
            Console.Write($"{message}");
            for (int i = message.Length; i < columnWidth; i++)
            {
                Console.Write(" ");
            }
        }
    }
}
