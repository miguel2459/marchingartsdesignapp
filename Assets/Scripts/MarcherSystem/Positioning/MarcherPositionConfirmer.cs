using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcherPositionConfirmer
{
    private readonly EnsembleDirector2 director;
    private readonly MarcherPositionHistory positionHistory;
    private readonly Func<int, int> getMaxCountForSet;
    public bool isUndoContext = false;


    public MarcherPositionConfirmer(EnsembleDirector2 director, MarcherPositionHistory positionHistory, Func<int, int> getMaxCountForSet)
    {
        this.director = director;
        this.positionHistory = positionHistory;
        this.getMaxCountForSet = getMaxCountForSet;
    }

    public void Confirm(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        EnsureSetExists(marcher, set);

        if (!isUndoContext)
        {
            TrackUndoForConfirm(marcher, set, count, finalPos);
        }
        else
        {
            UndoDeleteToReconfirm(marcher, set, count, finalPos);
        }

        marcher.SyncInspectorList();

        int visualizeSet = (set == 0) ? 1 : set;
        director?.VisualizePathsForSet(visualizeSet);
    }

    private void TrackUndoForConfirm(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        var beforeEntry = MarcherSnapshotUtility.CaptureBeforePositionType(marcher, set, count, getMaxCountForSet);

        marcher.countPositions[set][count] = new PositionEntry(finalPos, "march");

        MarcherInterpolator.ReinterpolateAroundDot(marcher, set, count, finalPos, getMaxCountForSet);

        RecordChanges(marcher, set, count, beforeEntry);
    }

    private void UndoDeleteToReconfirm(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        marcher.countPositions[set][count] = new PositionEntry(finalPos, "march");

        MarcherInterpolator.ReinterpolateAroundDot(marcher, set, count, finalPos, getMaxCountForSet);
    }


    private void RecordChanges(MarcherPositionsManager marcher, int set, int count, PositionEntry beforeEntry)
    {
        var afterEntry = MarcherSnapshotUtility.CaptureAfterPositionType(marcher, set, count, getMaxCountForSet);

        Debug.Log($"🧪 Comparing CONFIRM: BEFORE = {beforeEntry.type}, AFTER = {afterEntry.type}");

        var livePosition = marcher.transform.position;
        var intent = MarcherSnapshotUtility.DetermineConfirmedIntentWithLivePosition(livePosition, beforeEntry);

        positionHistory.RecordChange(marcher, set, count, beforeEntry, afterEntry, intent);
    }

    private void EnsureSetExists(MarcherPositionsManager marcher, int set)
    {
        if (!marcher.countPositions.ContainsKey(set))
            marcher.countPositions[set] = new Dictionary<int, PositionEntry>();
    }
}
