using SyncToyNext.Core;
using SyncToyNext.Core.SyncPoints;
using SyncToyNext.Core.UX;
using SyncToyNext.GuiClient.Models;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.GuiClient.Helpers
{
    internal class ClientHelpers
    {
        private static List<string> acceptedTextExtensions = new List<string>();

        static ClientHelpers()
        {
            InitializeKnownTextExtensions();
        }

        private static void InitializeKnownTextExtensions()
        {
            acceptedTextExtensions.Add(".txt");
            acceptedTextExtensions.Add(".html");
            acceptedTextExtensions.Add(".htm");
            acceptedTextExtensions.Add(".md");
            acceptedTextExtensions.Add(".json");
            acceptedTextExtensions.Add(".xml");
            acceptedTextExtensions.Add(".cs");
            acceptedTextExtensions.Add(".cpp");
            acceptedTextExtensions.Add(".c");
            acceptedTextExtensions.Add(".h");
            acceptedTextExtensions.Add(".hpp");
            acceptedTextExtensions.Add(".h++");
            acceptedTextExtensions.Add(".c++");
            acceptedTextExtensions.Add(".js");
            acceptedTextExtensions.Add(".css");
            acceptedTextExtensions.Add(".scss");
            acceptedTextExtensions.Add(".ts");
            acceptedTextExtensions.Add(".tsx");
            acceptedTextExtensions.Add(".py");
            acceptedTextExtensions.Add(".java");
            acceptedTextExtensions.Add(".php");
            acceptedTextExtensions.Add(".rb");
            acceptedTextExtensions.Add(".go");
            acceptedTextExtensions.Add(".sh");
            acceptedTextExtensions.Add(".bat");
            acceptedTextExtensions.Add(".ps1");
            acceptedTextExtensions.Add(".sql");
            acceptedTextExtensions.Add(".yaml");
            acceptedTextExtensions.Add(".yml");
            acceptedTextExtensions.Add(".log");
            acceptedTextExtensions.Add(".conf");
            acceptedTextExtensions.Add(".ini");
            acceptedTextExtensions.Add(".properties");
            acceptedTextExtensions.Add(".mdx");
            acceptedTextExtensions.Add(".txt");
            acceptedTextExtensions.Add(".csv");
            acceptedTextExtensions.Add(".tsv");
            acceptedTextExtensions.Add(".bas");
            acceptedTextExtensions.Add(".vb");
            acceptedTextExtensions.Add(".vbs");
            acceptedTextExtensions.Add(".lua");
            acceptedTextExtensions.Add(".swift");
            acceptedTextExtensions.Add(".kotlin");
            acceptedTextExtensions.Add(".dart");
            acceptedTextExtensions.Add(".r");
            acceptedTextExtensions.Add(".scala");
            acceptedTextExtensions.Add(".groovy");
            acceptedTextExtensions.Add(".clj");
            acceptedTextExtensions.Add(".clojure");
            acceptedTextExtensions.Add(".elixir");
            acceptedTextExtensions.Add(".erl");
            acceptedTextExtensions.Add(".ex");
            acceptedTextExtensions.Add(".exs");
            acceptedTextExtensions.Add(".asm");
            acceptedTextExtensions.Add(".asmx");
            acceptedTextExtensions.Add(".pl");
            acceptedTextExtensions.Add(".perl");
            acceptedTextExtensions.Add(".ps");
            acceptedTextExtensions.Add(".ps1xml");
            acceptedTextExtensions.Add(".sh");
            acceptedTextExtensions.Add(".rs");
        }

        public static void RestoreSyncPoint(SyncPoint? selectedSyncPoint)
        {
            if (selectedSyncPoint == null)
            {
                throw new InvalidOperationException("No valid syncpoint could be retrieved");
            }

            if (string.IsNullOrWhiteSpace(SessionContext.LocalFolderPath))
            {
                throw new InvalidOperationException("No valid local path was opened");
            }

            UserIO.Message($"Starting with restore of syncpoint {selectedSyncPoint.SyncPointId}");

            SyncPointRestorer.RestorePath = SessionContext.LocalFolderPath;
            SyncPointRestorer.Run(selectedSyncPoint.SyncPointId);
        }

        public static bool IsAcceptedTextExtension(string extension)
        {
            if(string.IsNullOrWhiteSpace(extension)) return false;
            if(!extension.StartsWith(".")) extension = "." + extension;

            return acceptedTextExtensions.Contains(extension.ToLowerInvariant());
        }

        public  static void ApplyRemoteConfigChanges(RemoteConfig existingRemoteConfig, RemoteDialogResult remoteConfigChanges)
        {
            if (string.IsNullOrWhiteSpace(SessionContext.LocalFolderPath)) throw new InvalidOperationException("Local folder should be known.");

            UserIO.Message("Changing configured remote location.");
            var currentRemotePath = SessionContext.RemoteFolderPath;
            SessionContext.RemoteFolderPath = remoteConfigChanges.RemotePath;

            if (remoteConfigChanges.IsCompressed)
            {
                SessionContext.RemoteFolderPath = Path.Combine(SessionContext.RemoteFolderPath, Path.GetFileName(SessionContext.LocalFolderPath) + ".zip");
            }

            existingRemoteConfig.RemotePath = SessionContext.RemoteFolderPath;
            existingRemoteConfig.Save(SessionContext.LocalFolderPath);
        }

        public static void RestoreMultipleEntriesFromSyncPoint(IEnumerable<SyncPointEntry> remoteSelectedItems, SyncPoint? currentSyncPoint, string restorePath, string? remotePath, string localPath)
        {
            SyncPointRestorer.RestorePath = restorePath;

            bool isZipped = Path.HasExtension(remotePath) && Path.GetExtension(remotePath) == ".zip";

            if (remoteSelectedItems.Count() > 0 && currentSyncPoint != null)
            {
                foreach (var entry in remoteSelectedItems)
                {
                    if (entry != null)
                    {
                        var entryParts = entry.RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                        var relativeEntryPath = entryParts[0];

                        if (!string.IsNullOrWhiteSpace(localPath))
                        {
                            SyncPointRestorer.Run(currentSyncPoint.SyncPointId, string.Empty, relativeEntryPath);
                        }
                    }
                }
            }
        }

        public static string RetrieveRemoteItem(SyncPointEntry selectedItem)
        {
            var pathParts = selectedItem.RelativeRemotePath.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            var relativePath = pathParts[0];
            if (pathParts.Length > 0)
            {
                var remoteFolderPath = Path.GetDirectoryName(SessionContext.RemoteFolderPath);

                if (remoteFolderPath == null) throw new InvalidOperationException("Remote folder path could not be retrieved");

                var zipPath = Path.Combine(remoteFolderPath, pathParts[1]);
                using var zip = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                var entryPath = relativePath.Replace("\\", "/");
                var zipEntry = archive.GetEntry(entryPath);

                var tempFolder = Path.GetTempPath();
                var tempPath = Path.Combine(tempFolder, Path.GetFileName(relativePath));

                if (zipEntry != null)
                {
                    zipEntry.ExtractToFile(tempPath, true);
                    return tempPath;
                }
            }
            else
            {
                if (SessionContext.RemoteFolderPath == null)
                {
                    throw new InvalidOperationException("Remote folder path is not set.");
                }

                var fullPath = Path.Combine(SessionContext.RemoteFolderPath, relativePath);
                return fullPath;
            }

            return string.Empty;
        }
    }
}
