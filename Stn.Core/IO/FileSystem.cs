using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stn.Core.IO
{
    
    /// <summary>
    /// A class with file operations for copy etc, because the regular File.Copy just simply chocked certain
    /// network targets, causing machines to reboot etc.These methods have a couple of mechanisms embedded to
    /// deal with unstable network connections etc.
    /// </summary>
    internal class FileSystem
    {
        const int KILOBYTE = 1024;
        const int BUFFER_SIZE_LOW = KILOBYTE * 128;
        const int BUFFER_SIZE_HIGH = KILOBYTE * 512;
        const int FLUSH_THRESHOLD = KILOBYTE * KILOBYTE * 2;

        private static DriveInfo[]? drives = null;

        private static bool IsNetworkLocation(string path)
        {
            //first check if the path is on a local or network drive.
            if (path.StartsWith(@"\\")) return true;

            if (drives == null)
            {
                drives = DriveInfo.GetDrives();
            }

            var driveInfo = drives.FirstOrDefault(d => path.StartsWith(d.Name, StringComparison.OrdinalIgnoreCase));

            if (driveInfo != null)
            {
                if (driveInfo.DriveType == DriveType.Network || driveInfo.DriveType == DriveType.CDRom) return true;
            }

            return false;
        }

        private static int DetermineBufferSize(string path)
        {
            return IsNetworkLocation(path) ? BUFFER_SIZE_LOW : BUFFER_SIZE_HIGH;
        }

        public async static Task<bool> Copy(string source, string target)
        {
            bool copySuccess = true;
            FileStream? sourceStream = null;
            FileStream? targetStream = null;
            
            //we take the buffer size of the slowest of the two.
            var bufferSize = Math.Min(DetermineBufferSize(source), DetermineBufferSize(target));

            const int MAX_RETRIES = 5;
            const int WAIT_TIME_UNIT = 250;

            try
            {
                OpenFiles(source, target, out sourceStream, out targetStream);

                if (sourceStream != null && targetStream != null)
                {
                    var sourceSize = sourceStream.Length;
                    var numberOfChunks = (sourceSize / bufferSize) + (sourceSize % bufferSize != 0 ? 1 : 0);
                    byte[] buffer = new byte[bufferSize];

                    for (int currentChunk = 0; currentChunk < numberOfChunks; currentChunk++)
                    {
                        var offset = currentChunk * bufferSize;

                        //flush every at every flush threshold so we have a safe point to return to if we need to retry.
                        if (offset != 0 && (offset % FLUSH_THRESHOLD == 0))
                        {
                            targetStream.Flush(true);
                        }

                        var bytesRead = 0;
                        var readSucces = false;
                        var readTryCount = 1;

                        while (!readSucces && readTryCount <= MAX_RETRIES)
                        {
                            try
                            {
                                if (readTryCount > 1)
                                {
                                    //try to reopen the file on the retries. Just to rule out we are not try to write to a buffer
                                    //which has been disconnected.
                                    sourceStream.Dispose();
                                    sourceStream = File.Open(target, FileMode.Open, FileAccess.Read, FileShare.Read);
                                }

                                sourceStream.Seek(offset, SeekOrigin.Begin);
                                bytesRead = await sourceStream.ReadAsync(buffer, 0, bufferSize);
                                readSucces = true;
                            }
                            catch (IOException)
                            {
                                await Task.Delay(WAIT_TIME_UNIT * readTryCount);
                                readSucces = false;
                                readTryCount++;
                            }
                        }

                        var writeSucces = false;
                        var writeTryCount = 1;

                        while(!writeSucces && writeTryCount <= MAX_RETRIES)
                        {
                            try
                            {
                                if (writeTryCount > 1)
                                {
                                    //try to reopen the file on the retries. Just to rule out we are not try to write to a buffer
                                    //which has been disconnected.
                                    targetStream.Dispose();
                                    targetStream = File.Open(target, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                                }


                                targetStream.Seek(offset, SeekOrigin.Begin);
                                await targetStream.WriteAsync(buffer, 0, bytesRead);
                                
                                writeSucces = true;
                            } 
                            catch (IOException)
                            {
                                await Task.Delay(WAIT_TIME_UNIT * writeTryCount);

                                //we want to reposition our offset to the latest flush threshold point because would probably be the safest point
                                //this is really only usefull for files above 1MB. Below that it basically means a complete retry of the file copy.
                                offset = offset - (offset % FLUSH_THRESHOLD);
                                currentChunk = offset / bufferSize;

                                writeSucces = false;
                                writeTryCount++;
                            }
                        }

                        if(!readSucces || !writeSucces)
                        {
                            sourceStream.Dispose();
                            sourceStream = null;
                            targetStream.Dispose();
                            targetStream = null;
                            throw new IOException("Failed to transfer file.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //we assume the target is corrupted in case of an error.
                if (Path.Exists(target))
                {
                    File.Delete(target);
                }

                copySuccess = false;   
            } 
            finally
            {
                if (sourceStream != null)
                {
                    sourceStream.Dispose();
                }

                if (targetStream != null)
                {
                    targetStream.Flush(true);
                    targetStream.Dispose();
                }

                if(copySuccess)
                {
                    var modifiedTime = File.GetLastWriteTimeUtc(source);
                    File.SetLastWriteTimeUtc(target, modifiedTime);

                    var createdTime = File.GetCreationTimeUtc(source);
                    File.SetCreationTimeUtc(target, createdTime);
                }
            }

            return copySuccess;
        }

        private static void OpenFiles(string source, string target, out FileStream? sourceStream, out FileStream? targetStream)
        {
            if (Path.Exists(source))
            {
                sourceStream = File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            else
            {
                throw new FileNotFoundException("Source not found.");
            }

            targetStream = File.Open(target, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        public static void RemoveDirectory(string path, Action<int, int, string>? OnUpdateHandler, int index = -1, int fromTotal = -1)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);

            var currentCount = 0;
            var totalCount = files.Count() + directories.Count();

            foreach( var file in files)
            {
                currentCount++;
                File.Delete(file);

                var indexMessage = (index >= 0 && fromTotal > index) ? $"({index}/{fromTotal})" : String.Empty;
                OnUpdateHandler?.Invoke(currentCount, totalCount, $"{indexMessage} Removing {file}");
            }

            foreach(var directory in directories)
            {
                currentCount++;

                Directory.Delete(directory, false);

                var indexMessage = (index >= 0 && fromTotal > index) ? $"({index}/{fromTotal})" : String.Empty;
                OnUpdateHandler?.Invoke(currentCount, totalCount, $"{indexMessage} Removing {directory}");
            }

            Directory.Delete(path);
        }
    }
}
