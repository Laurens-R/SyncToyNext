using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;

namespace SyncToyNext.Core.SyncPoints.Differ
{
    public class MergeConflict
    {
        public int LineNumber { get; set; }
        public string SourceLine { get; set; } = string.Empty;
        public string TargetLine { get; set; } = string.Empty;
    }

    public class MergeResult
    {
        public string MergedFileContent { get; set; } = String.Empty;
        public List<MergeConflict> MergeConflicts { get; set; } = new List<MergeConflict>();
    }

    public class Merger
    {
        public static MergeResult ThreeWayMerge(string baseText, string sourceText, string targetText)
        { 
            var mergeResult = new MergeResult();

            var diffSource = InlineDiffBuilder.Diff(baseText, sourceText);
            var diffTarget = InlineDiffBuilder.Diff(baseText, targetText);

            var merged = new List<string>();
            int lineCount = Math.Max(diffSource.Lines.Count, diffTarget.Lines.Count);

            for (int i = 0; i < lineCount; i++)
            {
                var sourceLine = i < diffSource.Lines.Count ? diffSource.Lines[i] : null;
                var targetLine = i < diffTarget.Lines.Count ? diffTarget.Lines[i] : null;

                // Both lines are unchanged
                if (sourceLine?.Type == ChangeType.Unchanged && targetLine?.Type == ChangeType.Unchanged)
                {
                    if(!String.IsNullOrWhiteSpace(sourceLine.Text))
                        merged.Add(sourceLine.Text);
                }
                // Source changed, target unchanged
                else if (sourceLine?.Type != ChangeType.Unchanged && targetLine?.Type == ChangeType.Unchanged)
                {
                    if (sourceLine != null && !String.IsNullOrWhiteSpace(sourceLine.Text))
                        merged.Add(sourceLine.Text);
                }
                // Target changed, source unchanged
                else if (sourceLine?.Type == ChangeType.Unchanged && targetLine?.Type != ChangeType.Unchanged)
                {
                    if (targetLine != null && !String.IsNullOrWhiteSpace(targetLine.Text))
                        merged.Add(targetLine.Text);
                }
                // Both changed and are the same
                else if (sourceLine?.Text == targetLine?.Text)
                {
                    if(sourceLine != null && !String.IsNullOrWhiteSpace(sourceLine.Text))
                        merged.Add(sourceLine.Text);
                }
                // Both changed and are different (conflict)
                else
                {
                    var conflict = new MergeConflict
                    {
                        LineNumber = i,
                        SourceLine = sourceLine?.Text ?? string.Empty,
                        TargetLine = targetLine?.Text ?? string.Empty,
                    };

                    mergeResult.MergeConflicts.Add(conflict);

                    merged.Add("<<<<<<< SOURCE");
                    if (sourceLine != null)
                    {
                        merged.Add(sourceLine?.Text ?? string.Empty);
                    }
                    
                    merged.Add("=======");
                    
                    if (targetLine != null)
                    {
                        merged.Add(targetLine?.Text ?? string.Empty);
                    }

                    merged.Add(">>>>>>> TARGET");
                }
            }

            mergeResult.MergedFileContent =  string.Join(Environment.NewLine, merged);

            return mergeResult;
        }
    }
}
