using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcherPositionDeleter
{
    private readonly EnsembleDirector2 director;
    private readonly MarcherPositionHistory positionHistory;
    private readonly Func<int, int> getMaxCountForSet;

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
            return false;

        RemoveConfirmedEntry(marcher, set, count);

        bool hasNext = interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos);
        bool hasPrev = interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos);
        hadNextConfirmed = hasNext;

        if (!hasNext && hasPrev)
            return CleanupBackInferredAndReposition(marcher, set, count, prevSet, prevCount, prevPos);

        if (hasPrev && hasNext)
            return ReinterpolateBetweenAnchors(marcher, set, count, prevSet, prevCount, prevPos, nextSet, nextCount, nextPos, interpolator);

        Debug.Log($"⚠️ No valid re-interpolation range found after deleting Set {set}, Count {count}");
        SyncAndNotify(marcher);
        return true;
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
        PositionEntry oldEntry = marcher.countPositions[set][count];

        positionHistory.RecordChange(
            marcher,
            set,
            count,
            oldEntry,
            new PositionEntry(),
            MarcherPositionHistory.ChangeIntent.Reconfirm
        );

        marcher.countPositions[set].Remove(count);
        Debug.Log($"✅ Removed confirmed position: Set {set}, Count {count} for {marcher.name}");
    }

    private bool CleanupBackInferredAndReposition(MarcherPositionsManager marcher, int set, int count, int prevSet, int prevCount, Vector3 prevPos)
    {
        Debug.Log($"📉 No next confirmed dot. Cleaning up inferred counts backwards from Set {set}, Count {count}");

        int inferredRemoved = 0;
        int startSet = Mathf.Min(set, prevSet);
        int endSet = Mathf.Max(set, prevSet);

        for (int s = startSet; s <= endSet; s++)
        {
            if (!marcher.countPositions.TryGetValue(s, out var map)) continue;

            int startCount, endCount;

            if (set == prevSet)
            {
                startCount = Mathf.Min(count, prevCount) + 1;
                endCount = Mathf.Max(count, prevCount) - 1;
            }
            else if (s == set)
            {
                startCount = 1;
                endCount = count - 1;
            }
            else if (s == prevSet)
            {
                continue; // don't clean fallback confirmed set
            }
            else
            {
                startCount = 1;
                endCount = getMaxCountForSet(s);
            }

            List<int> toRemove = new List<int>();

            for (int c = startCount; c <= endCount; c++)
            {
                if (map.TryGetValue(c, out var entry) && entry.IsInferred)
                {
                    toRemove.Add(c);
                    Debug.Log($"🧽 Queued inferred count for removal → Set {s}, Count {c}");
                }
            }

            foreach (int c in toRemove)
            {
                map.Remove(c);
                inferredRemoved++;
                Debug.Log($"❌ Removed inferred dot at Set {s}, Count {c}");
            }
        }

        Debug.Log($"🧹 Removed {inferredRemoved} inferred counts. Repositioning to Set {prevSet}, Count {prevCount}");
        marcher.transform.position = prevPos;

        if (marcher.TryGetComponent<MarcherVisualStateController>(out var visual))
            visual.SetSelectorVisible(false);

        SyncAndNotify(marcher);
        return true;
    }

    private bool ReinterpolateBetweenAnchors(
        MarcherPositionsManager marcher,
        int set,
        int count,
        int prevSet,
        int prevCount,
        Vector3 prevPos,
        int nextSet,
        int nextCount,
        Vector3 nextPos,
        MarcherInterpolator interpolator
    )
    {
        Debug.Log($"🔁 Re-interpolating between Set {prevSet}, Count {prevCount} and Set {nextSet}, Count {nextCount}");

        var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
        interpolator.ApplyInterpolatedPositions(prevPos, nextPos, steps);

        if (marcher.HasPositionAtCount(set, count))
        {
            Vector3 newPos = marcher.GetPositionAtCount(set, count);
            marcher.transform.position = newPos;
            Debug.Log($"🔄 Repositioned marcher to interpolated point at Set {set}, Count {count}");

            if (marcher.TryGetComponent<MarcherVisualStateController>(out var visual))
                visual.SetSelectorVisible(false);
        }

        SyncAndNotify(marcher);
        return true;
    }

    private void SyncAndNotify(MarcherPositionsManager marcher)
    {
        marcher.SyncInspectorList();
        MarcherPositionsManager.RaisePositionUpdatedEvent();
    }
}
