using Stn.Core.Helpers;
using Stn.Core.Runners;
using Stn.Core.Merging;
using Stn.Core.UX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stn.Core.SyncPoints
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

            var sourceRepo = new Repository(sourceLocalPath);
            var targetRepo = new Repository(targetLocalPath);

            string newSyncPointID = SyncPoint.GenerateSyncpointID();

            if (!MergePreparation(sourceRepo, targetRepo, newSyncPointID))
            {
                return;
            }

            var referenceBaseSyncPoint = sourceRepo.LatestReferenceSyncPoint;

            if (!Merger.Merge(sourceRepo, targetRepo, referenceBaseSyncPoint?.SyncPointId ?? string.Empty))
            {
                UserIO.Message("There are still some merge conflicts that need to be resolved. Please resolve the conflicts and finalize the merge.");
                return;
            }

            UserIO.Message("No merge conflicts detected. Resuming post merge synchronization activities.");

            //perform step 3, 4 and 5
            PostMergeSynchronization(sourceRepo, targetRepo, newSyncPointID);
        }

        private static bool MergePreparation(Repository sourceRepo, Repository targetRepo, string newSyncPointID)
        {
            newSyncPointID = "PREMERGE-" + newSyncPointID;
            string newSyncPointDesc = "Pre-Merge Syncpoint";

            sourceRepo.Push(newSyncPointID, newSyncPointDesc);
            targetRepo.Push(newSyncPointID, newSyncPointDesc);

            if (sourceRepo.LatestSyncPoint?.SyncPointId != newSyncPointID || targetRepo.LatestSyncPoint?.SyncPointId == null)
            {
                UserIO.Error("Could not retrieve newly created recovery syncpoints.");
                return false;
            }

            UserIO.Message($"Recovery syncpoints for both source and target created. Both with ID: {newSyncPointID}.");
            return true;
        }

        private static void PostMergeSynchronization(Repository sourceRepo, Repository targetRepo, string newSyncPointID)
        {
            newSyncPointID = "POSTMERGE-" + newSyncPointID;
            string newSyncPointDesc = "Post-Merge Syncpoint";

            //if we get hear we assume we can proceed with step 3: creating a syncpoint for the target.
            UserIO.Message("Creating new post-merge syncpoint for target");
            targetRepo.Push(newSyncPointID, newSyncPointDesc, true);

            //step 4: sync the contents of the target back to the source. (to receive back all the merged stuff as well).
            UserIO.Message("Synching changes in target back to source");
            ManualRunner.Run(targetRepo.LocalPath, sourceRepo.LocalPath);

            //step 5: create a syncpoint for the source location.
            UserIO.Message("Creating new post-merge syncpoint for source");
            sourceRepo.Push(newSyncPointID, newSyncPointDesc, true);

            UserIO.Message("Merge process completed!");
        }
    }
}