using SyncToyNext.Core.Helpers;
using SyncToyNext.Core.Runners;
using SyncToyNext.Core.SyncPoints.Diff;
using SyncToyNext.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncToyNext.Core.SyncPoints
{
    public class SyncPointMerger
    {
        public static void Merge(string sourceLocalPath, string targetLocalPath)
        {
            //to be clear both the source local path and the target local path are both local versions of their respective remotes.
            //
            //step 1: create new syncpoints for both local paths and make sure to sync them to their respective remotes
            //        (these two new syncpoints will also act as a failback/recovery point if the merge goes wrong).
            //step 2: take both newly created syncpoints and merge them using the MergeSyncPoints method.
            //step 3: after the merge (assuming its succesfull) create a new syncpoint in the target and sync with the remote.
            //step 4: perform a horizontal full sync back from the local target path to the original local source path.
            //step 5: create a new sync-point in the local source path and sync with the remote.
            //
            //result: both locations and their respective remotes should be 1:1 the same in terms of content.

            UserIO.Message("Starting merge process");

            var sourceConfig = RemoteConfig.Load(sourceLocalPath);
            var targetConfig = RemoteConfig.Load(targetLocalPath);

            if (sourceConfig == null || targetConfig == null)
            {
                UserIO.Error("Merging requires that the two provided local repositories both contain a remote configuration");
                return;
            }

            var sourceManager = new SyncPointManager(sourceConfig.RemotePath, sourceLocalPath);
            var targetManager = new SyncPointManager(targetConfig.RemotePath, targetLocalPath);

            //step 1
            ManualRunner.Run(sourceLocalPath, sourceConfig.RemotePath, true, string.Empty, "Pre-Merge Syncpoint");
            ManualRunner.Run(targetLocalPath, targetConfig.RemotePath, true, string.Empty, "Pre-Merge Syncpoint");

            sourceManager.RefreshSyncPoints();
            targetManager.RefreshSyncPoints();

            //step 2
            var sourceSyncPoint = sourceManager.SyncPoints.FirstOrDefault();
            var targetSyncPoint = targetManager.SyncPoints.FirstOrDefault();

            if (sourceSyncPoint == null || targetSyncPoint == null)
            {
                UserIO.Error("Could not retrieve newly created recovery syncpoints.");
                return;
            }

            UserIO.Message($"Recovery syncpoints for both source and target created: {sourceSyncPoint.SyncPointId} and {targetSyncPoint.SyncPointId} respectively.");

            UserIO.Message($"Starting actual file merging between source and target location.");

            var conflicts = MergeSyncPoints(sourceLocalPath, targetLocalPath);

            if (conflicts.Count > 0)
            {
                UserIO.Message("There are still some merge conflicts that need to be resolved. Please resolve the conflicts and finalize the merge.");
                return;
            }

            UserIO.Message("No merge conflicts detected. Resuming post merge synchronization activities.");

            //perform step 3, 4 and 5
            PostMergeSynchronization(sourceLocalPath, targetLocalPath, sourceConfig, targetConfig);
        }

        private static void PostMergeSynchronization(string sourceLocalPath, string targetLocalPath, RemoteConfig sourceConfig, RemoteConfig targetConfig)
        {

            //if we get hear we assume we can proceed with step 3: creating a syncpoint for the target.
            UserIO.Message("Creating new post-merge syncpoint for target");
            ManualRunner.Run(targetLocalPath, targetConfig.RemotePath, true, string.Empty, "Post-Merge Syncpoint");


            //step 4: sync the contents of the target back to the source. (to receive back all the merged stuff as well).
            UserIO.Message("Synching changes in target back to source");
            ManualRunner.Run(targetLocalPath, sourceLocalPath);

            //step 5: create a syncpoint for the source location.
            UserIO.Message("Creating new post-merge syncpoint for source");
            ManualRunner.Run(sourceLocalPath, sourceConfig.RemotePath, true, string.Empty, "Post-Merge Syncpoint");

            UserIO.Message("Merge process completed!");
        }

        /// <summary>
        /// Performs the actual merge between two locations/syncpoints.
        /// This method will try to perform auto-merge and will return a list if that fails
        /// due to merge conflicts.
        /// </summary>
        /// <param name="sourceSyncPoint"></param>
        /// <param name="targetSyncPoint"></param>
        /// <param name="sourceManager"></param>
        /// <param name="targetManager"></param>
        /// <returns>The list of merge conflicts.</returns>
        private static List<MergeConflict> MergeSyncPoints(string sourcePath, string targetPath)
        {
            var sourceFiles = FileHelpers.GetFilesInPath(sourcePath);
            var targetFiles = FileHelpers.GetFilesInPath(targetPath);

            foreach (var sourceEntryPath in sourceFiles)
            {
                var relativeSourcePath = Path.GetRelativePath(sourcePath, sourceEntryPath);
                var targetEntryPath = Path.Combine(targetPath, relativeSourcePath);
                bool targetExists = File.Exists(targetEntryPath);

                if (targetExists)
                {
                    bool areFilesDifferent = FileHelpers.IsFileDifferent(sourceEntryPath, targetEntryPath);

                    if (areFilesDifferent)
                    {
                        bool isTextFile = FileHelpers.IsAcceptedTextExtension(sourceEntryPath);
                        if (isTextFile)
                        {
                            var mergeResults = Merger.TwoWayMerge(sourceEntryPath, targetEntryPath);
                            File.WriteAllText(targetEntryPath, mergeResults.MergedFileContent);

                            if (mergeResults.MergeConflicts.Count > 0) {
                                UserIO.Message($"Merge conflicts in: { relativeSourcePath }");
                            }
                        }
                        else
                        {
                            //this is either not a supported text format or a binary. In either case we as we cannot
                            //do a safe line-by-line diff, we are going to do a full overwrite to the target.
                            File.Copy(sourceEntryPath, targetEntryPath, true);
                        }
                    }
                } else
                {
                    //ensure the directory exists for the target.
                    var directory = Path.GetDirectoryName(sourcePath);
                    if(!String.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(sourceEntryPath, targetEntryPath, true);
                }
            }

            return new List<MergeConflict>();
        }
    }
}