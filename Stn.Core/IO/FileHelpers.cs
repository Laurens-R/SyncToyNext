using Stn.Core.SyncPoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ZipLib = ICSharpCode.SharpZipLib.Zip;

namespace Stn.Core.IO
{
    public class FileHelpers
    {
        private static List<string> acceptedTextExtensions = new List<string>();
        private static List<string> knownCompressedExtensions = new List<string>();

        static FileHelpers()
        {
            InitializeKnownTextExtensions();
            InitializeKnownCompressedExtension();
        }

        private static void InitializeKnownCompressedExtension()
        {
            knownCompressedExtensions.Add(".zip");
            knownCompressedExtensions.Add(".rar");
            knownCompressedExtensions.Add(".arj");
            knownCompressedExtensions.Add(".7z");
            knownCompressedExtensions.Add(".png");
            knownCompressedExtensions.Add(".jpg");
            knownCompressedExtensions.Add(".jpeg");
            knownCompressedExtensions.Add(".mp3");
            knownCompressedExtensions.Add(".mp4");
            knownCompressedExtensions.Add(".mp5");
            knownCompressedExtensions.Add(".mkv");
            knownCompressedExtensions.Add(".avi");
            knownCompressedExtensions.Add(".mov");
            knownCompressedExtensions.Add(".prores");
            knownCompressedExtensions.Add(".mkv");
            knownCompressedExtensions.Add(".tga");
            knownCompressedExtensions.Add(".mb");
            knownCompressedExtensions.Add(".tx");
            knownCompressedExtensions.Add(".tex");
            knownCompressedExtensions.Add(".spp");
            knownCompressedExtensions.Add(".dds");
            knownCompressedExtensions.Add(".ktx");
            knownCompressedExtensions.Add(".ktx2");
            knownCompressedExtensions.Add(".pvr");
            knownCompressedExtensions.Add(".crn");
            knownCompressedExtensions.Add(".astc");
            knownCompressedExtensions.Add(".pak");
            knownCompressedExtensions.Add(".assetbundle");
            knownCompressedExtensions.Add(".bundle");
            knownCompressedExtensions.Add(".zst");
            knownCompressedExtensions.Add(".kraken");
            knownCompressedExtensions.Add(".mermaid");
            knownCompressedExtensions.Add(".leviathan");
            knownCompressedExtensions.Add(".arc");
            knownCompressedExtensions.Add(".ogg");
            knownCompressedExtensions.Add(".flac");
            knownCompressedExtensions.Add(".aac");
            knownCompressedExtensions.Add(".webm");
            knownCompressedExtensions.Add(".bik");
            knownCompressedExtensions.Add(".usm");
            knownCompressedExtensions.Add(".vdb");
            knownCompressedExtensions.Add(".zvdb");
            knownCompressedExtensions.Add(".spp");
            knownCompressedExtensions.Add(".sbs");
            knownCompressedExtensions.Add(".sbsar");
            knownCompressedExtensions.Add(".ssa");
            knownCompressedExtensions.Add(".alch");
            knownCompressedExtensions.Add(".mdl");
            knownCompressedExtensions.Add(".psd");
            knownCompressedExtensions.Add(".tiff");
            knownCompressedExtensions.Add(".hdr");
            knownCompressedExtensions.Add(".fbx");
            knownCompressedExtensions.Add(".blend");
            knownCompressedExtensions.Add(".max");
            knownCompressedExtensions.Add(".usd");
            knownCompressedExtensions.Add(".gltf");
            knownCompressedExtensions.Add(".exr");
            knownCompressedExtensions.Add(".ztl");
            knownCompressedExtensions.Add(".zpr");
            knownCompressedExtensions.Add(".hip");
            knownCompressedExtensions.Add(".bgeo");
            knownCompressedExtensions.Add(".geo");
            knownCompressedExtensions.Add(".pic");
            knownCompressedExtensions.Add(".rat");
            knownCompressedExtensions.Add(".i3d");
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

        public static bool IsAcceptedTextExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (!extension.StartsWith(".")) extension = "." + extension;

            return acceptedTextExtensions.Contains(extension.ToLowerInvariant());
        }

        public static bool IsCompressedExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (!extension.StartsWith(".")) extension = "." + extension;

            return knownCompressedExtensions.Contains(extension.ToLowerInvariant());
        }

        public static IEnumerable<string> GetFilesInPath(string sourcePath, IgnoreFile? ignoreFile = null)
        {
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.stn{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(Path.DirectorySeparatorChar).EndsWith($"{Path.DirectorySeparatorChar}.stn", StringComparison.OrdinalIgnoreCase));

            ignoreFile = ignoreFile == null ? new IgnoreFile() : ignoreFile;
            ignoreFile.TryLoadIgnoreFile(sourcePath);
            return files.Where(file => !ignoreFile.IsEntryIgnored(file));
        }

        public static bool IsFileDifferent(string srcFilePath, string destFilePath)
        {
            var srcLastWrite = File.GetLastWriteTimeUtc(srcFilePath);
            var destLastWrite = File.GetLastWriteTimeUtc(destFilePath);
            if (srcLastWrite > destLastWrite)
            {
                return true;
            }
            else
            {
                var srcSize = new FileInfo(srcFilePath).Length;
                var destSize = new FileInfo(destFilePath).Length;
                if (srcSize != destSize)
                {
                    return true;
                }
                else
                {
                    bool areFirst4KDifferent = AreFirst4KDifferent(srcFilePath, destFilePath);
                    if (areFirst4KDifferent)
                    {
                        return true;
                    }
                    else
                    {
                        var srcHash = ComputeSHA256(srcFilePath);
                        var destHash = ComputeSHA256(destFilePath);
                        if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsFileDifferent(string localFilePath, ZipArchiveEntry? remoteZipEntry)
        {
            if (remoteZipEntry == null) throw new InvalidOperationException("Zip file entry should not be null");

            var srcLastWrite = File.GetLastWriteTimeUtc(localFilePath);
            // ZIP entries store time as UTC, but DateTime.Kind is Unspecified - force it to UTC
            var entryLastWrite = DateTime.SpecifyKind(remoteZipEntry.LastWriteTime.DateTime, DateTimeKind.Utc);

            // Truncate to whole seconds for both to handle ZIP format precision issues
            srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
            entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
            var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

            if (secondsDifference > 2) // ZIP format is only precise to 2 seconds
            {
                return true;
            }
            else
            {
                long srcSize = new FileInfo(localFilePath).Length;
                long entrySize = remoteZipEntry.Length;
                if (srcSize != entrySize)
                {
                    return true;
                }
                else
                {
                    using var sourceFileStream = File.OpenRead(localFilePath);
                    using var zipEntryStream = remoteZipEntry.Open();

                    bool areDifferent = AreFirst4KDifferent(sourceFileStream, zipEntryStream);

                    if (areDifferent)
                    {
                        return true;
                    }
                    else
                    {
                        zipEntryStream.Seek(0, SeekOrigin.Begin);
                        var srcHash = ComputeSHA256(localFilePath);
                        string destHash = ComputeSHA256(zipEntryStream);

                        if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsFileDifferent(string localFilePath, ZipLib.ZipEntry? remoteZipEntry, ZipLib.ZipFile? archive)
        {
            if (remoteZipEntry == null || archive == null) throw new InvalidOperationException("Zip file entry should not be null");

            var srcLastWrite = File.GetLastWriteTimeUtc(localFilePath);
            // ZIP entries store time as UTC, but DateTime.Kind is Unspecified - force it to UTC
            var entryLastWrite = remoteZipEntry.DateTime;

            // Truncate to whole seconds for both to handle ZIP format precision issues
            srcLastWrite = srcLastWrite.AddTicks(-(srcLastWrite.Ticks % TimeSpan.TicksPerSecond));
            entryLastWrite = entryLastWrite.AddTicks(-(entryLastWrite.Ticks % TimeSpan.TicksPerSecond));
            var secondsDifference = Math.Abs((srcLastWrite - entryLastWrite).TotalSeconds);

            if (secondsDifference > 2) // ZIP format is only precise to 2 seconds
            {
                return true;
            }
            else
            {
                long srcSize = new FileInfo(localFilePath).Length;
                long entrySize = remoteZipEntry.Size;
                if (srcSize != entrySize)
                {
                    return true;
                }
                else
                {
                    using var sourceFileStream = File.OpenRead(localFilePath);
                    using var zipEntryStream = archive.GetInputStream(remoteZipEntry);

                    bool areDifferent = AreFirst4KDifferent(sourceFileStream, zipEntryStream);

                    if (areDifferent)
                    {
                        return true;
                    }
                    else
                    {
                        zipEntryStream.Seek(0, SeekOrigin.Begin);
                        var srcHash = ComputeSHA256(localFilePath);
                        string destHash = ComputeSHA256(zipEntryStream);

                        if (!srcHash.Equals(destHash, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool AreFirst4KDifferent(string filePath1, string filePath2)
        {
            using (var stream1 = File.OpenRead(filePath1))
            using (var stream2 = File.OpenRead(filePath2))
            {
                return AreFirst4KDifferent(stream1, stream2);
            }
        }

        public static bool AreFirst4KDifferent(Stream stream1, Stream stream2)
        {
            const int bufferSize = 4096; // 4KB
            byte[] buffer1 = new byte[bufferSize];
            byte[] buffer2 = new byte[bufferSize];
            int bytesRead1 = stream1.Read(buffer1, 0, bufferSize);
            int bytesRead2 = stream2.Read(buffer2, 0, bufferSize);
            if (bytesRead1 != bytesRead2)
                return true; // Different sizes
            for (int i = 0; i < bytesRead1; i++)
            {
                if (buffer1[i] != buffer2[i])
                    return true; // Found a difference
            }
            return false;
        }

        public static string ComputeSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash);
        }

        public  static string ComputeSHA256(Stream stream)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash);
        }
    }
}
