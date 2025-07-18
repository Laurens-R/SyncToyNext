using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;

namespace SyncToyNext.Core.SyncPoints.Diff
{
    public class MergeConflict
    {
        public int BaseStartLine { get; set; }
        public int SourceStartLine { get; set; }
        public int TargetStartLine { get; set; }
        public string SourceChunk { get; set; } = string.Empty;
        public string TargetChunk { get; set; } = string.Empty;
    }

    public class MergeResult
    {
        public string MergedFileContent { get; set; } = string.Empty;
        public List<MergeConflict> MergeConflicts { get; set; } = new List<MergeConflict>();
    }

    public class Merger
    {
        public static MergeResult ThreeWayMerge(string baseText, string sourceText, string targetText)
        {
            var mergeResult = new MergeResult();
            var differ = new Differ();

            var baseLines = baseText.Replace("\r\n", "\n").Split('\n');
            var sourceLines = sourceText.Replace("\r\n", "\n").Split('\n');
            var targetLines = targetText.Replace("\r\n", "\n").Split('\n');

            // Get diff blocks (regions) from base to source and base to target
            var diffSource = differ.CreateLineDiffs(baseText, sourceText, false);
            var diffTarget = differ.CreateLineDiffs(baseText, targetText, false);

            int baseIndex = 0, sourceIndex = 0, targetIndex = 0;
            int sourceBlock = 0, targetBlock = 0;

            var merged = new List<string>();

            while (baseIndex < baseLines.Length || sourceIndex < sourceLines.Length || targetIndex < targetLines.Length)
            {
                // Find the next diff block in source and target
                DiffPiece[]? sourceBlockLines = null;
                DiffPiece[]? targetBlockLines = null;
                int sourceBlockStart = -1, targetBlockStart = -1, sourceBlockLen = 0, targetBlockLen = 0;

                // Find the next source diff block that starts at or after baseIndex
                if (sourceBlock < diffSource.DiffBlocks.Count &&
                    diffSource.DiffBlocks[sourceBlock].InsertStartB == sourceIndex)
                {
                    var block = diffSource.DiffBlocks[sourceBlock];
                    sourceBlockStart = block.DeleteStartA;
                    sourceBlockLen = block.DeleteCountA;
                    sourceBlockLines = new DiffPiece[block.InsertCountB];
                    
                    for (int i = 0; i < block.InsertCountB; i++)
                    {
                        sourceBlockLines[i] = new DiffPiece(
                            sourceLines[block.InsertStartB + i], ChangeType.Inserted, block.InsertStartB + i);
                    }
                }

                // Find the next target diff block that starts at or after baseIndex
                if (targetBlock < diffTarget.DiffBlocks.Count &&
                    diffTarget.DiffBlocks[targetBlock].InsertStartB == targetIndex)
                {
                    var block = diffTarget.DiffBlocks[targetBlock];
                    targetBlockStart = block.DeleteStartA;
                    targetBlockLen = block.DeleteCountA;
                    targetBlockLines = new DiffPiece[block.InsertCountB];
                    
                    for (int i = 0; i < block.InsertCountB; i++)
                    {
                        targetBlockLines[i] = new DiffPiece(
                            targetLines[block.InsertStartB + i], ChangeType.Inserted, block.InsertStartB + i);
                    }
                }

                // If both source and target have a diff block at this region, check for conflict
                if (sourceBlockStart == baseIndex && targetBlockStart == baseIndex)
                {
                    // Compare the inserted lines
                    var sourceChunk = sourceBlockLines != null ? string.Join("\n", Array.ConvertAll(sourceBlockLines, l => l.Text)) : "";
                    var targetChunk = targetBlockLines != null ? string.Join("\n", Array.ConvertAll(targetBlockLines, l => l.Text)) : "";

                    if (sourceChunk == targetChunk)
                    {
                        // Both made the same change, take it
                        if (!string.IsNullOrEmpty(sourceChunk))
                            merged.Add(sourceChunk);
                    }
                    else
                    {
                        // Conflict!
                        mergeResult.MergeConflicts.Add(new MergeConflict
                        {
                            BaseStartLine = baseIndex,
                            SourceStartLine = sourceIndex,
                            TargetStartLine = targetIndex,
                            SourceChunk = sourceChunk,
                            TargetChunk = targetChunk
                        });

                        merged.Add("<<<<<<< SOURCE");
                        if (!string.IsNullOrEmpty(sourceChunk))
                            merged.Add(sourceChunk);
                        merged.Add("=======");
                        if (!string.IsNullOrEmpty(targetChunk))
                            merged.Add(targetChunk);
                        merged.Add(">>>>>>> TARGET");
                    }

                    // Advance all indices
                    baseIndex += sourceBlockLen; // or targetBlockLen, should be the same
                    sourceIndex += sourceBlockLines?.Length ?? 0;
                    targetIndex += targetBlockLines?.Length ?? 0;
                    sourceBlock++;
                    targetBlock++;
                }
                // Only source has a diff block
                else if (sourceBlockStart == baseIndex)
                {
                    var sourceChunk = sourceBlockLines != null ? string.Join("\n", Array.ConvertAll(sourceBlockLines, l => l.Text)) : "";
                    if (!string.IsNullOrEmpty(sourceChunk))
                        merged.Add(sourceChunk);

                    baseIndex += sourceBlockLen;
                    sourceIndex += sourceBlockLines?.Length ?? 0;
                    sourceBlock++;
                }
                // Only target has a diff block
                else if (targetBlockStart == baseIndex)
                {
                    var targetChunk = targetBlockLines != null ? string.Join("\n", Array.ConvertAll(targetBlockLines, l => l.Text)) : "";
                    if (!string.IsNullOrEmpty(targetChunk))
                        merged.Add(targetChunk);

                    baseIndex += targetBlockLen;
                    targetIndex += targetBlockLines?.Length ?? 0;
                    targetBlock++;
                }
                // No diff block at this region, take unchanged line from base/source/target
                else
                {
                    // Take the line from base/source/target (they should all be the same here)
                    if (baseIndex < baseLines.Length)
                        merged.Add(baseLines[baseIndex]);
                    baseIndex++;
                    sourceIndex++;
                    targetIndex++;
                }
            }

            mergeResult.MergedFileContent = string.Join(Environment.NewLine, merged);
            return mergeResult;
        }

        public static MergeResult TwoWayMerge(string sourceText, string targetText)
        {
            var mergeResult = new MergeResult();
            var differ = new Differ();

            var sourceLines = sourceText.Replace("\r\n", "\n").Split('\n');
            var targetLines = targetText.Replace("\r\n", "\n").Split('\n');

            var diff = differ.CreateLineDiffs(targetText, sourceText, false);

            int targetIndex = 0, sourceIndex = 0, blockIndex = 0;
            var merged = new List<string>();

            while (targetIndex < targetLines.Length || sourceIndex < sourceLines.Length)
            {
                // If we're at a diff block, handle it
                if (blockIndex < diff.DiffBlocks.Count &&
                    diff.DiffBlocks[blockIndex].DeleteStartA == targetIndex &&
                    diff.DiffBlocks[blockIndex].InsertStartB == sourceIndex)
                {
                    var block = diff.DiffBlocks[blockIndex];

                    // Get the changed regions
                    var targetChunk = new List<string>();
                    for (int i = 0; i < block.DeleteCountA; i++)
                        targetChunk.Add(targetLines[block.DeleteStartA + i]);

                    var sourceChunk = new List<string>();
                    for (int i = 0; i < block.InsertCountB; i++)
                        sourceChunk.Add(sourceLines[block.InsertStartB + i]);

                    // If both regions are non-empty and different, it's a conflict
                    if (targetChunk.Count > 0 && sourceChunk.Count > 0 && string.Join("\n", targetChunk) != string.Join("\n", sourceChunk))
                    {
                        mergeResult.MergeConflicts.Add(new MergeConflict
                        {
                            SourceStartLine = sourceIndex,
                            TargetStartLine = targetIndex,
                            SourceChunk = string.Join("\n", sourceChunk),
                            TargetChunk = string.Join("\n", targetChunk)
                        });

                        merged.Add("<<<<<<< SOURCE");
                        merged.AddRange(sourceChunk);
                        merged.Add("=======");
                        merged.AddRange(targetChunk);
                        merged.Add(">>>>>>> TARGET");
                    }
                    // If only source has content, take source
                    else if (sourceChunk.Count > 0)
                    {
                        merged.AddRange(sourceChunk);
                    }
                    // If only target has content, take target
                    else if (targetChunk.Count > 0)
                    {
                        merged.AddRange(targetChunk);
                    }

                    targetIndex += block.DeleteCountA;
                    sourceIndex += block.InsertCountB;
                    blockIndex++;
                }
                else
                {
                    // No diff block, lines are the same
                    if (targetIndex < targetLines.Length)
                        merged.Add(targetLines[targetIndex]);
                    targetIndex++;
                    sourceIndex++;
                }
            }

            mergeResult.MergedFileContent = string.Join(Environment.NewLine, merged);
            return mergeResult;
        }
    }
}