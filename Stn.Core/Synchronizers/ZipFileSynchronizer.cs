using Stn.Core.Helpers;
using Stn.Core.Models;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Schema;

namespace Stn.Core.Synchronizers
{
    /// <summary>
    /// Provides file synchronization logic for writing files into a Zip archive.
    /// </summary>
    public class ZipFileSynchronizer : Synchronizer
    {
        private string _zipFilePath;
        private OverwriteOption _overwriteOption;
        private bool _strictMode;

        public ZipFileSynchronizer(string zipFilePath, OverwriteOption overwriteOption, bool strictMode = false)
        {
            _zipFilePath = zipFilePath;
            _overwriteOption = overwriteOption;
            _strictMode = strictMode;
        }

        /// <summary>
        /// Synchronizes a single file into the zip archive.
        /// </summary>
        /// <param name="srcFilePath">The full path to the source file.</param>
        /// <param name="relativePath">The relative path inside the zip archive.</param>
        public override void SynchronizeFile(string srcFilePath, string relativePath, string? oldDestFilePath = null)
        {
            try
            {
                using var zip = new FileStream(_zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                using var archive = new ZipArchive(zip, ZipArchiveMode.Update, leaveOpen: false);
                var entryPath = relativePath.Replace("\\", "/");
                var entry = archive.GetEntry(entryPath);
                bool entryExists = entry != null;
                bool shouldCopy = false;
                string action = "None";

                if (oldDestFilePath != null )
                {
                    // If an old destination file was provided, delete it first
                    var oldEntry = archive.GetEntry(oldDestFilePath.Replace("\\", "/"));
                    oldEntry?.Delete();
                }

                if (_overwriteOption == OverwriteOption.AlwaysOverwrite)
                {
                    shouldCopy = true;
                    action = entryExists ? "Overwrite" : "Create";
                }
                else if (!entryExists)
                {
                    shouldCopy = true;
                    action = "Create";
                }
                else if (entry != null)
                {
                    shouldCopy = FileHelpers.IsFileDifferent(srcFilePath, entry);

                    if (shouldCopy)
                    {
                        action = "Update";
                    }
                }

                if (shouldCopy)
                {
                    entry?.Delete();
                    var newEntry = archive.CreateEntry(entryPath, CompressionLevel.SmallestSize);
                    newEntry.LastWriteTime = File.GetLastWriteTime(srcFilePath); //get the local time, because it seems to internally convert to the right UTC.
                    using var entryStream = newEntry.Open();
                    using var fileStream = File.OpenRead(srcFilePath);
                    fileStream.CopyTo(entryStream);
                    UserIO.Message($"{action} (zip): {relativePath}");
                }
                
            }
            catch (IOException ioEx)
            {
                UserIO.Error($"IO error syncing '{srcFilePath}' to zip '{_zipFilePath}'", ioEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                UserIO.Error($"Access denied syncing '{srcFilePath}' to zip '{_zipFilePath}'", uaEx);
            }
            catch (InvalidDataException dataEx)
            {
                UserIO.Error($"Invalid data in zip '{_zipFilePath}'", dataEx);
            }
            catch (Exception ex)
            {
                UserIO.Error($"Unexpected error syncing '{srcFilePath}' to zip '{_zipFilePath}'", ex);
            }
        }

        /// <summary>
        /// Synchronizes all files and subdirectories from the source path into the zip archive.
        /// </summary>
        /// <param name="sourcePath">The root directory to copy files from.</param>
        public override void FullSynchronization(string sourcePath, SyncPoint? syncPoint = null, SyncPointManager? syncPointManager = null)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");

            var allFilesInSourcePath = FileHelpers.GetFilesInPath(sourcePath);

            if (syncPoint != null && syncPointManager != null)
            {
                ProcessSyncPoint(sourcePath, syncPoint, syncPointManager, allFilesInSourcePath);
            }
            else
            {
                ProcessStraightSync(sourcePath, allFilesInSourcePath);
            }
        }

        private void ProcessSyncPoint(string sourceDirectory, SyncPoint newSyncPoint, SyncPointManager syncPointManager, IEnumerable<string> allSourceLocationFiles)
        {
            var allFilesPartOfSyncPoint = syncPointManager.GetFileEntriesAtSyncpoint(newSyncPoint.SyncPointId);

            //update zip path according to sync point
            var zipParentFolder = syncPointManager.RemotePath;

            if(zipParentFolder == null)
            {
                throw new InvalidOperationException("Couldn't resolve parent folder of zip file.");
            }

            int progressCounter = 0;
            int totalFileCount = allSourceLocationFiles.Count();

            foreach (var srcFilePath in allSourceLocationFiles)
            {
                var relativeSourcePath = Path.GetRelativePath(sourceDirectory, srcFilePath);

                var existingEntry = allFilesPartOfSyncPoint.FirstOrDefault(e => e.SourcePath.Equals(relativeSourcePath, StringComparison.OrdinalIgnoreCase));

                var relativeDestinationPath = $"{relativeSourcePath}@{newSyncPoint.SyncPointId}\\{Path.GetFileName(_zipFilePath)}";

                if (existingEntry != null)
                {
                    //determine the path of the file in the zip archive.
                    var spRelativeZipFile = existingEntry.RelativeRemotePath.Split("@")[1];
                    var relativePathInZip = existingEntry.RelativeRemotePath.Split("@")[0];
                    var spZipFile = Path.Combine(zipParentFolder, spRelativeZipFile);

                    using var zip = new FileStream(spZipFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                    using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: false);
                    var entryPath = relativePathInZip.Replace("\\", "/");
                    var zipEntry = archive.GetEntry(entryPath);

                    if(existingEntry.EntryType == SyncPointEntryType.Deleted)
                    {
                        // If the entry was marked as deleted, we need to re-add it
                        newSyncPoint.AddEntry(relativeSourcePath, relativeDestinationPath);
                        SynchronizeFile(srcFilePath, relativeSourcePath);
                        continue;
                    }

                    if (zipEntry != null)
                    {
                        var sourceFileInfo = new FileInfo(srcFilePath);

                        var srcLastWrite = sourceFileInfo.LastWriteTimeUtc;

                        //this thing is acting strangely... maybe we need to support both scenarios
                        //with straight UtcTimeDate and the Kind thing.
                        var entryLastWrite = zipEntry.LastWriteTime.UtcDateTime; //DateTime.SpecifyKind(zipEntry.LastWriteTime.UtcDateTime, DateTimeKind.Utc);//zipEntry.LastWriteTime.UtcDateTime;

                        srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

                        if (secondsDifference > 2 || zipEntry.Length != sourceFileInfo.Length) // ZIP format is only precise to 2 seconds
                        {
                            newSyncPoint.AddEntry(relativeSourcePath, relativeDestinationPath);
                            SynchronizeFile(srcFilePath, relativeSourcePath);
                            continue;
                        }
                    } else
                    {
                        throw new Exception($"Entry '{entryPath}' not found in zip file '{spRelativeZipFile}' for sync point '{newSyncPoint.SyncPointId}'.");
                    }
                }
                else
                {
                    newSyncPoint.AddEntry(relativeSourcePath, relativeDestinationPath);
                    SynchronizeFile(srcFilePath, relativeSourcePath);
                }

                progressCounter++;

                if(UpdateProgressHandler != null)
                {
                    UpdateProgressHandler(progressCounter, totalFileCount, srcFilePath);
                }
            }

            var updatedFileListOfSyncpoint = syncPointManager.GetFileEntriesAtSyncpoint(newSyncPoint.SyncPointId);

            // Now we need to check for files that were deleted since the last sync point
            DetectRemovedFiles(sourceDirectory, updatedFileListOfSyncpoint, allSourceLocationFiles, newSyncPoint);

            newSyncPoint.Save(Path.Combine(zipParentFolder, newSyncPoint.SyncPointId, $"{newSyncPoint.SyncPointId}.syncpoint.json"));
        }

        private void ProcessStraightSync(string sourcePath, IEnumerable<string> allFiles)
        {
            int progressCounter = 0;
            int totalFileCount = allFiles.Count();

            foreach (var srcFilePath in allFiles)
            {
                var relativePath = Path.GetRelativePath(sourcePath, srcFilePath);
                SynchronizeFile(srcFilePath, relativePath);

                progressCounter++;

                if (UpdateProgressHandler != null)
                {
                    UpdateProgressHandler(progressCounter, totalFileCount, srcFilePath);
                }
            }
        }
    }
}
