using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcherPositionDeleter
{
    private readonly EnsembleDirector2 director;
    private readonly MarcherPositionHistory positionHistory;
    private readonly Func<int, int> getMaxCountForSet;
    public bool isUndoContext = false;

    public MarcherPositionDeleter(EnsembleDirector2 director, MarcherPositionHistory positionHistory, Func<int, int> getMaxCountForSet)
    {
        this.director = director;
        this.positionHistory = positionHistory;
        this.getMaxCountForSet = getMaxCountForSet;
    }

    public bool Delete(MarcherPositionsManager marcher, int set, int count, out bool hadNextConfirmed)
    {
        hadNextConfirmed = false;
        int lastCount = getMaxCountForSet(set);

        Debug.Log($"🗑 Attempting to delete Set {set}, Count {count} for {marcher.name}");

        var interpolator = new MarcherInterpolator(marcher, getMaxCountForSet, () => marcher.transform.position);

        if (ShouldBlockDelete(marcher, set, count, interpolator, lastCount))
        {
            Debug.LogWarning($"⛔ Deletion blocked: Cannot delete Set {set}, Count {count} for {marcher.name}");
            return false;
        }

        PositionEntry beforeEntry = default;
        if (!isUndoContext)
        {
            beforeEntry = MarcherSnapshotUtility.CaptureBeforePositionType(marcher, set, count, getMaxCountForSet);
        }

        RemoveConfirmedEntry(marcher, set, count);

        bool hasNext = interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos);
        bool hasPrev = interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos);
        hadNextConfirmed = hasNext;

        bool result = false;

        if (!hasNext && hasPrev)
        {
            result = CleanupBackInferredAndReposition(marcher, set, count, prevSet, prevCount, prevPos);
        }
        else if (hasPrev && hasNext)
        {
            result = ReinterpolateBetweenAnchors(marcher, set, count, prevSet, prevCount, prevPos, nextSet, nextCount, nextPos, interpolator);
        }
        else
        {
            Debug.Log($"⚠️ No valid interpolation anchors found after deleting Set {set}, Count {count} for {marcher.name}. Only local cleanup done.");
            SyncAndNotify(marcher);
            result = true;
        }

        if (!isUndoContext)
        {
            RecordChanges(marcher, set, count, beforeEntry);
        }

        return result;
    }

    private bool ShouldBlockDelete(MarcherPositionsManager marcher, int set, int count, MarcherInterpolator interpolator, int lastCount)
    {
        if (count == lastCount && interpolator.TryFindNextConfirmedPosition(set, count, out _, out _, out _))
        {
            Debug.LogWarning($"❌ Cannot delete confirmed position at final count {count} of Set {set} — future confirmed exists.");
            return true;
        }

        if (!marcher.HasPositionAtCount(set, count) || !marcher.countPositions[set][count].IsConfirmed)
        {
            //Debug.LogWarning($"⚠️ No confirmed dot found at Set {set}, Count {count} for {marcher.name}");
            return true;
        }

        return false;
    }

    private void RemoveConfirmedEntry(MarcherPositionsManager marcher, int set, int count)
    {
        marcher.countPositions[set].Remove(count);
        Debug.Log($"✅ Removed confirmed position: Set {set}, Count {count} for {marcher.name}");
    }

    private bool CleanupBackInferredAndReposition(MarcherPositionsManager marcher, int set, int count, int prevSet, int prevCount, Vector3 prevPos)
    {
        Debug.Log($"📉 No next confirmed dot. Cleaning up inferred counts backwards from Set {set}, Count {count}");

        // 1. Generate interpolation step range between previous confirmed and deleted dot
        var interpolator = new MarcherInterpolator(marcher, getMaxCountForSet, () => marcher.transform.position);
        List<(int s, int c)> steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);

        // 2. Remove any inferred entries in that range
        int inferredRemoved = 0;
        foreach (var (s, c) in steps)
        {
            if (marcher.countPositions.TryGetValue(s, out var map) &&
                map.TryGetValue(c, out var entry) &&
                entry.IsInferred)
            {
                map.Remove(c);
                inferredRemoved++;
                Debug.Log($"❌ Removed inferred dot at Set {s}, Count {c}");
            }
        }

        Debug.Log($"🧹 Removed {inferredRemoved} inferred counts. Repositioning to Set {prevSet}, Count {prevCount}");

        // 3. Reposition marcher and finalize visuals
        marcher.transform.position = prevPos;

        if (marcher.TryGetComponent(out MarcherVisualStateController visual))
            visual.SetSelectorVisible(false);

        SyncAndNotify(marcher);
        return true;
    }

    private bool ReinterpolateBetweenAnchors( MarcherPositionsManager marcher, int set, int count, int prevSet, int prevCount, Vector3 prevPos, int nextSet, int nextCount, Vector3 nextPos, MarcherInterpolator interpolator)
    {
        MarcherInterpolator.ReinterpolateBetweenDots(
            marcher,
            set,
            count,
            prevPos,
            nextPos,
            prevSet,
            prevCount,
            nextSet,
            nextCount,
            getMaxCountForSet
        );

        SyncAndNotify(marcher);
        return true;
    }

    private void RecordChanges(MarcherPositionsManager marcher, int set, int count, PositionEntry beforeEntry)
    {
        var afterEntry = MarcherSnapshotUtility.CaptureAfterPositionType(marcher, set, count, getMaxCountForSet);

        Debug.Log($"🧪 Comparing DELETE: BEFORE = {beforeEntry.type}, AFTER = {afterEntry.type}");

        var intent = MarcherSnapshotUtility.DetermineDeletedIntent(beforeEntry, afterEntry);
        positionHistory.RecordChange(marcher, set, count, beforeEntry, afterEntry, intent);
    }

    private void SyncAndNotify(MarcherPositionsManager marcher)
    {
        marcher.SyncInspectorList();
        MarcherPositionsManager.RaisePositionUpdatedEvent();
    }
}
