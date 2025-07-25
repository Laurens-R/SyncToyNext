using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.Helpers
{
    public class FileHelpers
    {
        private static List<string> acceptedTextExtensions = new List<string>();

        static FileHelpers()
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

        public static bool IsAcceptedTextExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (!extension.StartsWith(".")) extension = "." + extension;

            return acceptedTextExtensions.Contains(extension.ToLowerInvariant());
        }

        public static IEnumerable<string> GetFilesInPath(string sourcePath)
        {
            return Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.stn{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !f.TrimEnd(Path.DirectorySeparatorChar).EndsWith($"{Path.DirectorySeparatorChar}.stn", StringComparison.OrdinalIgnoreCase));

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

                    bool areDifferent = FileHelpers.AreFirst4KDifferent(sourceFileStream, zipEntryStream);

                    if (areDifferent)
                    {
                        return true;
                    }
                    else
                    {
                        zipEntryStream.Seek(0, SeekOrigin.Begin);
                        var srcHash = FileHelpers.ComputeSHA256(localFilePath);
                        string destHash = FileHelpers.ComputeSHA256(zipEntryStream);

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
