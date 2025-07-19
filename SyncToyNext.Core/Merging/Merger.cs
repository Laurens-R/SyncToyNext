using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using SyncToyNext.Core.Helpers;
using SyncToyNext.Core.SyncPoints;
using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SyncToyNext.Core.Merging
{
    public enum TwoWayMergePolicy
    {
        SourceWins,
        Union
    }

    public class MergeConflict
    {
        public int BaseStartLine { get; set; }
        public int SourceStartLine { get; set; }
        public int TargetStartLine { get; set; }
        public string SourceChunk { get; set; } = string.Empty;
        public string TargetChunk { get; set; } = string.Empty;
        public ConflictType Type { get; set; } = ConflictType.ContentConflict;
    }

    public enum ConflictType
    {
        ContentConflict,    // Both sides changed the same content differently
        DeleteVsEdit,       // One side deleted, other side edited
        AddVsAdd           // Both sides added different content at the same location
    }

    public class MergeResult
    {
        public string MergedFileContent { get; set; } = string.Empty;
        public List<MergeConflict> MergeConflicts { get; set; } = new List<MergeConflict>();
        public MergeStatistics Statistics { get; set; } = new MergeStatistics();
    }

    public class MergeStatistics
    {
        public int LinesProcessed { get; set; }
        public int ConflictsFound { get; set; }
        public int AutoMergedChanges { get; set; }
        public int SourceOnlyChanges { get; set; }
        public int TargetOnlyChanges { get; set; }
        public bool WasFullyAutoMerged => ConflictsFound == 0;
    }

    public class Merger
    {
        // Configurable conflict markers for different contexts
        public static class ConflictMarkers
        {
            public static string SourceStart { get; set; } = "<<<<<<< SOURCE";
            public static string Separator { get; set; } = "=======";
            public static string TargetEnd { get; set; } = ">>>>>>> TARGET";
            public static string BaseMarker { get; set; } = "|||||||";
        }

        // Enhanced conflict marker method with optional base content for 3-way merges
        private static void AddConflictMarkers(List<string> merged, string sourceChunk, string targetChunk, string? baseChunk = null)
        {
            merged.Add(ConflictMarkers.SourceStart);
            if (!string.IsNullOrEmpty(sourceChunk))
            {
                var sourceLines = sourceChunk.Split('\n');
                merged.AddRange(sourceLines);
            }
            
            if (!string.IsNullOrEmpty(baseChunk))
            {
                merged.Add(ConflictMarkers.BaseMarker);
                var baseLines = baseChunk.Split('\n');
                merged.AddRange(baseLines);
            }
            
            merged.Add(ConflictMarkers.Separator);
            if (!string.IsNullOrEmpty(targetChunk))
            {
                var targetLines = targetChunk.Split('\n');
                merged.AddRange(targetLines);
            }
            merged.Add(ConflictMarkers.TargetEnd);
        }

        public static MergeResult ThreeWayMerge(string baseText, string sourceText, string targetText)
        {
            var mergeResult = new MergeResult();
            
            // Handle null/empty input edge cases
            if (string.IsNullOrEmpty(baseText)) baseText = string.Empty;
            if (string.IsNullOrEmpty(sourceText)) sourceText = string.Empty;
            if (string.IsNullOrEmpty(targetText)) targetText = string.Empty;

            // Handle case where all inputs are empty
            if (string.IsNullOrEmpty(baseText) && string.IsNullOrEmpty(sourceText) && string.IsNullOrEmpty(targetText))
            {
                mergeResult.MergedFileContent = string.Empty;
                return mergeResult;
            }

            // Quick check: if source and target are identical, return either
            if (sourceText == targetText)
            {
                mergeResult.MergedFileContent = sourceText;
                var lines = sourceText.Split('\n').Length;
                mergeResult.Statistics.LinesProcessed = lines;
                return mergeResult;
            }

            // Quick check: if source equals base, return target (only target changed)
            if (sourceText == baseText)
            {
                mergeResult.MergedFileContent = targetText;
                var lines = targetText.Split('\n').Length;
                mergeResult.Statistics.LinesProcessed = lines;
                mergeResult.Statistics.TargetOnlyChanges = lines;
                return mergeResult;
            }

            // Quick check: if target equals base, return source (only source changed)
            if (targetText == baseText)
            {
                mergeResult.MergedFileContent = sourceText;
                var lines = sourceText.Split('\n').Length;
                mergeResult.Statistics.LinesProcessed = lines;
                mergeResult.Statistics.SourceOnlyChanges = lines;
                return mergeResult;
            }

            var differ = new Differ();

            var baseLines = baseText.Replace("\r\n", "\n").Split('\n');
            var sourceLines = sourceText.Replace("\r\n", "\n").Split('\n');
            var targetLines = targetText.Replace("\r\n", "\n").Split('\n');

            // Get diff blocks (regions) from base to source and base to target
            var diffSource = differ.CreateLineDiffs(baseText, sourceText, false);
            var diffTarget = differ.CreateLineDiffs(baseText, targetText, false);

            int baseIndex = 0, sourceIndex = 0, targetIndex = 0;
            int sourceBlock = 0, targetBlock = 0;

            // Pre-allocate merged list with estimated capacity for better performance
            var estimatedLines = Math.Max(sourceLines.Length, Math.Max(baseLines.Length, targetLines.Length));
            var merged = new List<string>(estimatedLines + 10); // +10 for potential conflict markers

            while (baseIndex < baseLines.Length || sourceIndex < sourceLines.Length || targetIndex < targetLines.Length)
            {
                mergeResult.Statistics.LinesProcessed++;

                // Find the next diff block in source and target
                DiffPiece[]? sourceBlockLines = null;
                DiffPiece[]? targetBlockLines = null;
                int sourceBlockStart = -1, targetBlockStart = -1, sourceBlockLen = 0, targetBlockLen = 0;

                // Find the next source diff block that starts at or after baseIndex
                if (sourceBlock < diffSource.DiffBlocks.Count)
                {
                    var block = diffSource.DiffBlocks[sourceBlock];
                    if (block.DeleteStartA == baseIndex)
                    {
                        sourceBlockStart = block.DeleteStartA;
                        sourceBlockLen = block.DeleteCountA;
                        sourceBlockLines = new DiffPiece[block.InsertCountB];
                        for (int i = 0; i < block.InsertCountB; i++)
                        {
                            if (block.InsertStartB + i < sourceLines.Length)
                            {
                                sourceBlockLines[i] = new DiffPiece(
                                    sourceLines[block.InsertStartB + i], ChangeType.Inserted, block.InsertStartB + i);
                            }
                        }
                    }
                }

                // Find the next target diff block that starts at or after baseIndex
                if (targetBlock < diffTarget.DiffBlocks.Count)
                {
                    var block = diffTarget.DiffBlocks[targetBlock];
                    if (block.DeleteStartA == baseIndex)
                    {
                        targetBlockStart = block.DeleteStartA;
                        targetBlockLen = block.DeleteCountA;
                        targetBlockLines = new DiffPiece[block.InsertCountB];
                        for (int i = 0; i < block.InsertCountB; i++)
                        {
                            if (block.InsertStartB + i < targetLines.Length)
                            {
                                targetBlockLines[i] = new DiffPiece(
                                    targetLines[block.InsertStartB + i], ChangeType.Inserted, block.InsertStartB + i);
                            }
                        }
                    }
                }

                // If both source and target have a diff block at this region, check for conflict
                if (sourceBlockStart == baseIndex && targetBlockStart == baseIndex)
                {
                    // Compare the inserted lines
                    var sourceChunk = sourceBlockLines != null ? string.Join("\n", Array.ConvertAll(sourceBlockLines, l => l?.Text ?? string.Empty)) : "";
                    var targetChunk = targetBlockLines != null ? string.Join("\n", Array.ConvertAll(targetBlockLines, l => l?.Text ?? string.Empty)) : "";

                    if (sourceChunk == targetChunk)
                    {
                        // Both made the same change, take it
                        if (!string.IsNullOrEmpty(sourceChunk))
                        {
                            // Split multi-line chunks properly
                            var lines = sourceChunk.Split('\n');
                            merged.AddRange(lines);
                            mergeResult.Statistics.AutoMergedChanges += lines.Length;
                        }
                    }
                    else
                    {
                        // Conflict!
                        var conflict = new MergeConflict
                        {
                            BaseStartLine = baseIndex,
                            SourceStartLine = sourceIndex,
                            TargetStartLine = targetIndex,
                            SourceChunk = sourceChunk,
                            TargetChunk = targetChunk,
                            Type = ConflictType.ContentConflict
                        };
                        mergeResult.MergeConflicts.Add(conflict);
                        mergeResult.Statistics.ConflictsFound++;

                        AddConflictMarkers(merged, sourceChunk, targetChunk, 
                            sourceBlockLen > 0 && baseIndex < baseLines.Length ? 
                            string.Join("\n", baseLines.Skip(baseIndex).Take(sourceBlockLen)) : null);
                    }

                    // Advance all indices
                    baseIndex += Math.Max(sourceBlockLen, targetBlockLen);
                    sourceIndex += sourceBlockLines?.Length ?? 0;
                    targetIndex += targetBlockLines?.Length ?? 0;
                    sourceBlock++;
                    targetBlock++;
                }
                // Only source has a diff block
                else if (sourceBlockStart == baseIndex)
                {
                    var sourceChunk = sourceBlockLines != null ? string.Join("\n", Array.ConvertAll(sourceBlockLines, l => l?.Text ?? string.Empty)) : "";
                    if (!string.IsNullOrEmpty(sourceChunk))
                    {
                        var lines = sourceChunk.Split('\n');
                        merged.AddRange(lines);
                        mergeResult.Statistics.SourceOnlyChanges += lines.Length;
                    }

                    baseIndex += sourceBlockLen;
                    sourceIndex += sourceBlockLines?.Length ?? 0;
                    sourceBlock++;
                }
                // Only target has a diff block
                else if (targetBlockStart == baseIndex)
                {
                    var targetChunk = targetBlockLines != null ? string.Join("\n", Array.ConvertAll(targetBlockLines, l => l?.Text ?? string.Empty)) : "";
                    if (!string.IsNullOrEmpty(targetChunk))
                    {
                        var lines = targetChunk.Split('\n');
                        merged.AddRange(lines);
                        mergeResult.Statistics.TargetOnlyChanges += lines.Length;
                    }

                    baseIndex += targetBlockLen;
                    targetIndex += targetBlockLines?.Length ?? 0;
                    targetBlock++;
                }
                // No diff block at this region, handle deletions/edits/conflicts
                else
                {
                    if (baseIndex < baseLines.Length)
                    {
                        bool sourceHasLine = sourceIndex < sourceLines.Length;
                        bool targetHasLine = targetIndex < targetLines.Length;
                        
                        string? sourceLine = sourceHasLine ? sourceLines[sourceIndex] : null;
                        string? targetLine = targetHasLine ? targetLines[targetIndex] : null;
                        string baseLine = baseLines[baseIndex];

                        bool sourceUnchanged = sourceLine == baseLine;
                        bool targetUnchanged = targetLine == baseLine;

                        if (sourceUnchanged && targetUnchanged)
                        {
                            // Both kept the line unchanged
                            merged.Add(baseLine);
                            // This is not counted as auto-merged since it's unchanged
                        }
                        else if (!sourceHasLine && targetUnchanged)
                        {
                            // Source deleted, target unchanged - safe to delete
                            // Do not add the line (deletion)
                            mergeResult.Statistics.AutoMergedChanges++; // Safe deletion
                        }
                        else if (sourceUnchanged && !targetHasLine)
                        {
                            // Target deleted, source unchanged - safe to delete
                            // Do not add the line (deletion)
                            mergeResult.Statistics.AutoMergedChanges++; // Safe deletion
                        }
                        else if (!sourceHasLine && targetHasLine && !targetUnchanged)
                        {
                            // Source deleted, target changed: conflict
                            var conflict = new MergeConflict
                            {
                                BaseStartLine = baseIndex,
                                SourceStartLine = sourceIndex,
                                TargetStartLine = targetIndex,
                                SourceChunk = "",
                                TargetChunk = targetLine ?? "",
                                Type = ConflictType.DeleteVsEdit
                            };
                            mergeResult.MergeConflicts.Add(conflict);
                            mergeResult.Statistics.ConflictsFound++;
                            
                            merged.Add("<<<<<<< SOURCE");
                            merged.Add("=======");
                            merged.Add(targetLine ?? "");
                            merged.Add(">>>>>>> TARGET");
                        }
                        else if (sourceHasLine && !sourceUnchanged && !targetHasLine)
                        {
                            // Source changed, target deleted: conflict
                            var conflict = new MergeConflict
                            {
                                BaseStartLine = baseIndex,
                                SourceStartLine = sourceIndex,
                                TargetStartLine = targetIndex,
                                SourceChunk = sourceLine ?? "",
                                TargetChunk = "",
                                Type = ConflictType.DeleteVsEdit
                            };
                            mergeResult.MergeConflicts.Add(conflict);
                            mergeResult.Statistics.ConflictsFound++;
                            
                            merged.Add("<<<<<<< SOURCE");
                            merged.Add(sourceLine ?? "");
                            merged.Add("=======");
                            merged.Add(">>>>>>> TARGET");
                        }
                        else if (!sourceHasLine && !targetHasLine)
                        {
                            // Both deleted - do nothing
                            mergeResult.Statistics.AutoMergedChanges++; // Both sides agree on deletion
                        }
                        else if (sourceHasLine && targetHasLine && !sourceUnchanged && !targetUnchanged)
                        {
                            // Both changed - this should have been handled by diff blocks, but handle as conflict
                            if (sourceLine != targetLine)
                            {
                                var conflict = new MergeConflict
                                {
                                    BaseStartLine = baseIndex,
                                    SourceStartLine = sourceIndex,
                                    TargetStartLine = targetIndex,
                                    SourceChunk = sourceLine ?? "",
                                    TargetChunk = targetLine ?? "",
                                    Type = ConflictType.ContentConflict
                                };
                                mergeResult.MergeConflicts.Add(conflict);
                                mergeResult.Statistics.ConflictsFound++;
                                
                                merged.Add("<<<<<<< SOURCE");
                                merged.Add(sourceLine ?? "");
                                merged.Add("=======");
                                merged.Add(targetLine ?? "");
                                merged.Add(">>>>>>> TARGET");
                            }
                            else
                            {
                                // Both changed to the same thing
                                merged.Add(sourceLine ?? "");
                                mergeResult.Statistics.AutoMergedChanges++;
                            }
                        }
                        else
                        {
                            // One side changed, other unchanged - take the change
                            if (!sourceUnchanged && targetUnchanged)
                            {
                                merged.Add(sourceLine ?? "");
                                mergeResult.Statistics.SourceOnlyChanges++;
                            }
                            else if (sourceUnchanged && !targetUnchanged)
                            {
                                merged.Add(targetLine ?? "");
                                mergeResult.Statistics.TargetOnlyChanges++;
                            }
                        }
                    }
                    baseIndex++;
                    sourceIndex++;
                    targetIndex++;
                }
            }

            mergeResult.MergedFileContent = string.Join(Environment.NewLine, merged);
            return mergeResult;
        }

        public static MergeResult TwoWayMerge(string sourceText, string targetText, TwoWayMergePolicy policy = TwoWayMergePolicy.SourceWins)
        {
            var mergeResult = new MergeResult();
            
            // Handle null/empty input edge cases
            if (string.IsNullOrEmpty(sourceText)) sourceText = string.Empty;
            if (string.IsNullOrEmpty(targetText)) targetText = string.Empty;

            // Handle case where both inputs are empty
            if (string.IsNullOrEmpty(sourceText) && string.IsNullOrEmpty(targetText))
            {
                mergeResult.MergedFileContent = string.Empty;
                return mergeResult;
            }

            // Quick check: if source and target are identical, return either
            if (sourceText == targetText)
            {
                mergeResult.MergedFileContent = sourceText;
                mergeResult.Statistics.LinesProcessed = sourceText.Split('\n').Length;
                return mergeResult;
            }

            var differ = new Differ();

            var sourceLines = sourceText.Replace("\r\n", "\n").Split('\n');
            var targetLines = targetText.Replace("\r\n", "\n").Split('\n');

            var diff = differ.CreateLineDiffs(targetText, sourceText, false);

            int targetIndex = 0, sourceIndex = 0, blockIndex = 0;
            
            // Pre-allocate merged list with estimated capacity
            var estimatedLines = Math.Max(sourceLines.Length, targetLines.Length);
            var merged = new List<string>(estimatedLines + 10); // +10 for potential conflict markers

            while (targetIndex < targetLines.Length || sourceIndex < sourceLines.Length)
            {
                mergeResult.Statistics.LinesProcessed++;

                if (blockIndex < diff.DiffBlocks.Count &&
                    diff.DiffBlocks[blockIndex].DeleteStartA == targetIndex &&
                    diff.DiffBlocks[blockIndex].InsertStartB == sourceIndex)
                {
                    var block = diff.DiffBlocks[blockIndex];

                    var targetChunk = new List<string>(block.DeleteCountA);
                    for (int i = 0; i < block.DeleteCountA; i++)
                    {
                        if (block.DeleteStartA + i < targetLines.Length)
                            targetChunk.Add(targetLines[block.DeleteStartA + i]);
                    }

                    var sourceChunk = new List<string>(block.InsertCountB);
                    for (int i = 0; i < block.InsertCountB; i++)
                    {
                        if (block.InsertStartB + i < sourceLines.Length)
                            sourceChunk.Add(sourceLines[block.InsertStartB + i]);
                    }

                    if (targetChunk.Count > 0 && sourceChunk.Count > 0)
                    {
                        var targetContent = string.Join("\n", targetChunk);
                        var sourceContent = string.Join("\n", sourceChunk);
                        
                        if (targetContent != sourceContent)
                        {
                            // Different content - handle based on policy
                            if (policy == TwoWayMergePolicy.SourceWins)
                            {
                                merged.AddRange(sourceChunk);
                                mergeResult.Statistics.SourceOnlyChanges += sourceChunk.Count;
                            }
                            else // Union - mark as conflict for manual resolution
                            {
                                var conflict = new MergeConflict
                                {
                                    SourceStartLine = sourceIndex,
                                    TargetStartLine = targetIndex,
                                    SourceChunk = sourceContent,
                                    TargetChunk = targetContent,
                                    Type = ConflictType.ContentConflict
                                };
                                mergeResult.MergeConflicts.Add(conflict);
                                mergeResult.Statistics.ConflictsFound++;
                                
                                merged.Add("<<<<<<< SOURCE");
                                merged.AddRange(sourceChunk);
                                merged.Add("=======");
                                merged.AddRange(targetChunk);
                                merged.Add(">>>>>>> TARGET");
                            }
                        }
                        else
                        {
                            // Same content, just take it
                            merged.AddRange(sourceChunk);
                            mergeResult.Statistics.AutoMergedChanges += sourceChunk.Count;
                        }
                    }
                    else if (sourceChunk.Count > 0 && targetChunk.Count == 0)
                    {
                        // Source added lines
                        merged.AddRange(sourceChunk);
                        mergeResult.Statistics.SourceOnlyChanges += sourceChunk.Count;
                    }
                    else if (targetChunk.Count > 0 && sourceChunk.Count == 0)
                    {
                        // Target had lines that source deleted
                        if (policy == TwoWayMergePolicy.Union)
                        {
                            merged.AddRange(targetChunk);
                            mergeResult.Statistics.TargetOnlyChanges += targetChunk.Count;
                        }
                        else
                        {
                            // SourceWins: omit target-only lines (deletion)
                            mergeResult.Statistics.AutoMergedChanges += targetChunk.Count;
                        }
                    }

                    targetIndex += block.DeleteCountA;
                    sourceIndex += block.InsertCountB;
                    blockIndex++;
                }
                else
                {
                    if (targetIndex < targetLines.Length && sourceIndex < sourceLines.Length)
                    {
                        // If lines are the same, add either
                        if (sourceLines[sourceIndex] == targetLines[targetIndex])
                        {
                            merged.Add(sourceLines[sourceIndex]);
                            // Unchanged lines are not counted in statistics
                        }
                        else
                        {
                            // If lines differ and not in a diff block, handle according to policy
                            if (policy == TwoWayMergePolicy.Union)
                            {
                                // Add conflict markers for clarity in Union mode when lines differ
                                var conflict = new MergeConflict
                                {
                                    SourceStartLine = sourceIndex,
                                    TargetStartLine = targetIndex,
                                    SourceChunk = sourceLines[sourceIndex],
                                    TargetChunk = targetLines[targetIndex],
                                    Type = ConflictType.ContentConflict
                                };
                                mergeResult.MergeConflicts.Add(conflict);
                                mergeResult.Statistics.ConflictsFound++;
                                
                                merged.Add("<<<<<<< SOURCE");
                                merged.Add(sourceLines[sourceIndex]);
                                merged.Add("=======");
                                merged.Add(targetLines[targetIndex]);
                                merged.Add(">>>>>>> TARGET");
                            }
                            else // SourceWins
                            {
                                merged.Add(sourceLines[sourceIndex]);
                                mergeResult.Statistics.SourceOnlyChanges++;
                            }
                        }
                    }
                    else if (sourceIndex < sourceLines.Length)
                    {
                        // Only source has remaining lines
                        merged.Add(sourceLines[sourceIndex]);
                        mergeResult.Statistics.SourceOnlyChanges++;
                    }
                    else if (targetIndex < targetLines.Length)
                    {
                        // Only target has remaining lines
                        if (policy == TwoWayMergePolicy.Union)
                        {
                            merged.Add(targetLines[targetIndex]);
                            mergeResult.Statistics.TargetOnlyChanges++;
                        }
                        else
                        {
                            // SourceWins: omit target-only trailing lines
                            mergeResult.Statistics.AutoMergedChanges++; // Count as auto-applied deletion
                        }
                    }
                    targetIndex++;
                    sourceIndex++;
                }
            }

            mergeResult.MergedFileContent = string.Join(Environment.NewLine, merged);
            return mergeResult;
        }

        public static bool ManualMerge(string sourcePath, string targetPath, TwoWayMergePolicy policy, Action<string, int, int>? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                UserIO.Error("Source path is invalid or does not exist.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
            {
                UserIO.Error("Target path is invalid or does not exist.");
                return false;
            }

            UserIO.Message($"Starting actual file merging between source and target location.");

            var sourceFiles = FileHelpers.GetFilesInPath(sourcePath).ToList();
            var targetFiles = FileHelpers.GetFilesInPath(targetPath);
            var mergeSuccessful = true;
            int totalFiles = sourceFiles.Count;
            int processedFiles = 0;

            foreach (var sourceEntryPath in sourceFiles)
            {
                try
                {
                    var relativeSourcePath = Path.GetRelativePath(sourcePath, sourceEntryPath);
                    progressCallback?.Invoke(relativeSourcePath, processedFiles, totalFiles);
                    
                    var targetEntryPath = Path.Combine(targetPath, relativeSourcePath);
                    bool targetExists = File.Exists(targetEntryPath);

                    if (targetExists)
                    {
                        bool areFilesDifferent = FileHelpers.IsFileDifferent(sourceEntryPath, targetEntryPath);

                        if (areFilesDifferent)
                        {
                            bool isTextFile = FileHelpers.IsAcceptedTextExtension(Path.GetExtension(sourceEntryPath));
                            if (isTextFile)
                            {
                                //we use two-way merge instead of 3-way merge, because on a file system it is hard
                                //to guarantee a common-base. Maybe in the future if the remote side get's fleshed
                                //out a bit more.
                                var sourceContent = File.ReadAllText(sourceEntryPath);
                                var targetContent = File.ReadAllText(targetEntryPath);

                                var mergeResults = TwoWayMerge(sourceContent, targetContent, policy);
                                File.WriteAllText(targetEntryPath, mergeResults.MergedFileContent);

                                if (mergeResults.MergeConflicts.Count > 0)
                                {
                                    UserIO.Message($"Merge conflicts in: {relativeSourcePath} ({mergeResults.MergeConflicts.Count} conflicts)");
                                    mergeSuccessful = false;
                                }
                                else
                                {
                                    UserIO.Message($"Successfully merged: {relativeSourcePath} ({mergeResults.Statistics.AutoMergedChanges + mergeResults.Statistics.SourceOnlyChanges} source/auto-merged changes)");
                                }
                            }
                            else
                            {
                                //this is either not a supported text format or a binary. In either case we as we cannot
                                //do a safe line-by-line diff, we are going to do a full overwrite to the target.
                                File.Copy(sourceEntryPath, targetEntryPath, true);
                                UserIO.Message($"Binary file copied: {relativeSourcePath}");
                            }
                        }
                    }
                    else
                    {
                        //ensure the directory exists for the target.
                        var directory = Path.GetDirectoryName(targetEntryPath);
                        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.Copy(sourceEntryPath, targetEntryPath, true);
                        UserIO.Message($"New file added: {relativeSourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    UserIO.Error($"Error processing file {sourceEntryPath}: {ex.Message}");
                    mergeSuccessful = false;
                }
                finally
                {
                    processedFiles++;
                }
            }

            progressCallback?.Invoke("Completed", totalFiles, totalFiles);
            UserIO.Message($"File merging completed. Processed {processedFiles} files.");

            return mergeSuccessful;
        }

        public static bool Merge(LocalRepository sourceRepo, LocalRepository targetRepo, string baseReferenceSyncID, Action<string, int, int>? progressCallback = null)
        {

            UserIO.Message($"Starting actual file merging between source and target location.");

            var sourceFiles = sourceRepo.GetLocalFiles();
            var targetFiles = targetRepo.GetLocalFiles();
            var mergeSuccessful = true;
            int totalFiles = sourceFiles.Count();
            int processedFiles = 0;

            //if the base reference id is set, make sure 
            if (!String.IsNullOrEmpty(baseReferenceSyncID) && (!sourceRepo.HasSyncPointID(baseReferenceSyncID) || !targetRepo.HasSyncPointID(baseReferenceSyncID)))
            {
                UserIO.Error("Reference Sync ID was set, but it is not present in both locations. Cannot continue with merge");
                return false;
            }

            bool useFallBackSyncMethod = String.IsNullOrEmpty(baseReferenceSyncID);

            foreach (var sourceEntryPath in sourceFiles)
            {
                try
                {
                    var relativeSourcePath = Path.GetRelativePath(sourceRepo.LocalPath, sourceEntryPath);
                    progressCallback?.Invoke(relativeSourcePath, processedFiles, totalFiles);

                    var targetEntryPath = Path.Combine(targetRepo.LocalPath, relativeSourcePath);
                    bool targetExists = File.Exists(targetEntryPath);

                    if (targetExists)
                    {
                        bool areFilesDifferent = FileHelpers.IsFileDifferent(sourceEntryPath, targetEntryPath);

                        if (areFilesDifferent)
                        {
                            bool isTextFile = FileHelpers.IsAcceptedTextExtension(Path.GetExtension(sourceEntryPath));
                            if (isTextFile)
                            {
                                //we use two-way merge instead of 3-way merge, because on a file system it is hard
                                //to guarantee a common-base. Maybe in the future if the remote side get's fleshed
                                //out a bit more.
                                var sourceContent = File.ReadAllText(sourceEntryPath);
                                var targetContent = File.ReadAllText(targetEntryPath);

                                MergeResult? mergeResults = null;

                                if (!useFallBackSyncMethod)
                                {
                                    var baseContent = sourceRepo.ReadAllTextRemote(relativeSourcePath, baseReferenceSyncID);
                                    mergeResults = ThreeWayMerge(baseContent, sourceContent, targetContent);
                                    File.WriteAllText(targetEntryPath, mergeResults.MergedFileContent);
                                } else
                                {
                                    mergeResults = TwoWayMerge(sourceContent, targetContent, TwoWayMergePolicy.SourceWins);
                                    File.WriteAllText(targetEntryPath, mergeResults.MergedFileContent);
                                }

                                if (mergeResults.MergeConflicts.Count > 0)
                                {
                                    UserIO.Message($"Merge conflicts in: {relativeSourcePath} ({mergeResults.MergeConflicts.Count} conflicts)");
                                    mergeSuccessful = false;
                                }
                                else
                                {
                                    UserIO.Message($"Successfully merged: {relativeSourcePath} ({mergeResults.Statistics.AutoMergedChanges + mergeResults.Statistics.SourceOnlyChanges} source/auto-merged changes)");
                                }
                            }
                            else
                            {
                                //this is either not a supported text format or a binary. In either case we as we cannot
                                //do a safe line-by-line diff, we are going to do a full overwrite to the target.
                                File.Copy(sourceEntryPath, targetEntryPath, true);
                                UserIO.Message($"Binary file copied: {relativeSourcePath}");
                            }
                        }
                    }
                    else
                    {
                        //ensure the directory exists for the target.
                        var directory = Path.GetDirectoryName(targetEntryPath);
                        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.Copy(sourceEntryPath, targetEntryPath, true);
                        UserIO.Message($"New file added: {relativeSourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    UserIO.Error($"Error processing file {sourceEntryPath}: {ex.Message}");
                    mergeSuccessful = false;
                }
                finally
                {
                    processedFiles++;
                }
            }

            progressCallback?.Invoke("Completed", totalFiles, totalFiles);
            UserIO.Message($"File merging completed. Processed {processedFiles} files.");

            return mergeSuccessful;
        }
    }
}