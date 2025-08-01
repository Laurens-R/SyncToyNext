using Stn.Core.IO;
using Stn.Core.Synchronization;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Schema;

namespace Stn.Core.Synchronizers
{

    public class SourceInformation
    {
        public string RelativeSourcePath { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public string ZipFile { get; set; } = string.Empty;
        public SyncPointEntry? SyncPointFileEntry { get; set; } = null;

        public bool IsExistingEntry  { 
            get
            {
                return !string.IsNullOrEmpty(ZipFile) && SyncPointFileEntry != null;
            }
        }
    }

    /// <summary>
    /// Provides file synchronization logic for writing files into a Zip archive.
    /// </summary>
    public class ZipFileSynchronizer : Synchronizer
    {
        private string _zipFilePath;
        private OverwriteOption _overwriteOption;
        private bool _strictMode;
        private FileStream? _archiveStream;
        private ZipArchive? _archive;
        private bool _isArchiveOpen = false;

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
                if (!_isArchiveOpen || _archive == null)
                {
                    throw new InvalidOperationException("Zip archive is not open. Call OpenTarget() before synchronizing files.");
                }

                var entryPath = relativePath.Replace("\\", "/");
                var entry = _archive.GetEntry(entryPath);
                bool entryExists = entry != null;
                bool shouldCopy = false;
                string action = "None";

                if (oldDestFilePath != null )
                {
                    // If an old destination file was provided, delete it first
                    var oldEntry = _archive.GetEntry(oldDestFilePath.Replace("\\", "/"));
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
                    var newEntry = _archive.CreateEntry(entryPath, CompressionLevel.SmallestSize);
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

            if(!_isArchiveOpen || _archive == null)
            {
                throw new InvalidOperationException("Zip archive is not open. Call OpenTarget() before synchronizing files.");
            }

            if(zipParentFolder == null)
            {
                throw new InvalidOperationException("Couldn't resolve parent folder of zip file.");
            }

            int progressCounter = 0;
            int totalFileCount = allSourceLocationFiles.Count();

            //To avoid most of the IO and GC overhead that comes with opening zip files, we 
            //are going to group entries together to belong to the same zip files.
            var sortedSyncPointFiles = allFilesPartOfSyncPoint.OrderBy(spFile =>
            {
                var zipFile = spFile.RelativeRemotePath.Split("@")[1];
                return zipFile;
            });

            if(UpdateProgressHandler != null)
            {
                UpdateProgressHandler(progressCounter, totalFileCount, "Preparing synchronization index...");
            }

            //now sort the source files locations to their according to the sorted sync point files.
            var preparedEntries = allSourceLocationFiles.AsParallel().Select(srcFile =>
            {
                var relevantSyncEntry = allFilesPartOfSyncPoint.FirstOrDefault(spFile => spFile.SourcePath.Equals(Path.GetRelativePath(sourceDirectory, srcFile), StringComparison.OrdinalIgnoreCase));

                var entry = new SourceInformation
                {
                    RelativeSourcePath = Path.GetRelativePath(sourceDirectory, srcFile),
                    SourceFile = srcFile,
                    ZipFile = relevantSyncEntry?.RelativeRemotePath.Split("@")[1] ?? string.Empty,
                    SyncPointFileEntry = relevantSyncEntry
                };

               return entry;
            }).OrderBy(entry => entry.ZipFile);

            FileStream? spArchiveStream = null;
            ZipArchive? spArchive = null;
            string currentZipFilePath = string.Empty;

            foreach (var entry in preparedEntries)
            {
                var relativeDestinationPath = $"{entry.RelativeSourcePath}@{newSyncPoint.SyncPointId}\\{Path.GetFileName(_zipFilePath)}";

                if(entry.SyncPointFileEntry != null && entry.SyncPointFileEntry.EntryType == SyncPointEntryType.Deleted)
                {
                    // If the entry was marked as deleted, we need to re-add it
                    newSyncPoint.AddEntry(entry.RelativeSourcePath, relativeDestinationPath);
                    SynchronizeFile(entry.SourceFile, entry.RelativeSourcePath);
                    continue;
                }

                if (entry.SyncPointFileEntry != null)
                {
                    //determine the path of the file in the zip archive.
                    var relativePathInZip = entry.RelativeSourcePath;
                    var syncPointZipFile = Path.Combine(zipParentFolder, entry.ZipFile);

                    if(entry.ZipFile != currentZipFilePath && !String.IsNullOrWhiteSpace(entry.ZipFile))
                    {
                        // If we are switching to a new zip file, close the previous one
                        spArchive?.Dispose();
                        spArchiveStream?.Dispose();
                        currentZipFilePath = entry.ZipFile;
                        spArchiveStream = new FileStream(syncPointZipFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        spArchive = new ZipArchive(spArchiveStream, ZipArchiveMode.Update, leaveOpen: false);
                    }

                    var entryPath = entry.RelativeSourcePath.Replace("\\", "/");
                    var zipEntry = spArchive != null ? spArchive.GetEntry(entryPath) : null;

                    if (zipEntry != null)
                    {
                        var sourceFileInfo = new FileInfo(entry.SourceFile);

                        var srcLastWrite = sourceFileInfo.LastWriteTimeUtc;

                        //this thing is acting strangely... maybe we need to support both scenarios
                        //with straight UtcTimeDate and the Kind thing.
                        var entryLastWrite = zipEntry.LastWriteTime.UtcDateTime; //DateTime.SpecifyKind(zipEntry.LastWriteTime.UtcDateTime, DateTimeKind.Utc);//zipEntry.LastWriteTime.UtcDateTime;
                        srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
                        var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

                        if (secondsDifference > 2 || zipEntry.Length != sourceFileInfo.Length) // ZIP format is only precise to 2 seconds
                        {
                            newSyncPoint.AddEntry(entry.RelativeSourcePath, relativeDestinationPath);
                            SynchronizeFile(entry.SourceFile, entry.RelativeSourcePath);
                            continue;
                        }
                    } else
                    {
                        throw new Exception($"Entry '{entryPath}' not found in zip file '{entry.ZipFile}' for sync point '{newSyncPoint.SyncPointId}'.");
                    }
                }
                else
                {
                    newSyncPoint.AddEntry(entry.RelativeSourcePath, relativeDestinationPath);
                    SynchronizeFile(entry.SourceFile, entry.RelativeSourcePath);
                }

                progressCounter++;

                if(UpdateProgressHandler != null)
                {
                    UpdateProgressHandler(progressCounter, totalFileCount, entry.RelativeSourcePath);
                }
            }

            spArchive?.Dispose();
            spArchiveStream?.Dispose();

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

        public override void OpenTarget()
        {
            _archiveStream = new FileStream(_zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Update, leaveOpen: false);
            _isArchiveOpen = true;
        }

        public override void CloseTarget()
        {
            _isArchiveOpen = false;
            _archive?.Dispose();
            _archiveStream?.Dispose();
        }
    }
}
