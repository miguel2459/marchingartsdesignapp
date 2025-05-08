using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central service to handle confirming, deleting, and interpolating marcher positions.
/// </summary>
public class MarcherPositionService : MonoBehaviour
{
    public event Action OnAnyMarcherPositionUpdated = delegate { };

    [SerializeField] private EnsembleSessionLoader sessionLoader;
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private EnsembleDirector2 director;

    /// <summary>
    /// Confirm a "march" tag and interpolate around it.
    /// </summary>
    public void ConfirmMarcherPosition(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        EnsureSetExists(marcher, set);
        //positionHistory.BeginBatch();

        // 🔍 Determine if this is a new confirmation or a reconfirm
        bool alreadyConfirmed = marcher.HasPositionAtCount(set, count) && marcher.countPositions[set][count].IsConfirmed;

        // 🧠 Capture state before confirmation
        PositionEntry oldEntry = alreadyConfirmed ? marcher.countPositions[set][count] : new PositionEntry();
        Dictionary<(int set, int count), PositionEntry> beforeSnapshot = GetAffectedPositionSnapshot(marcher, set, count);

        // ✍️ Apply confirmed position
        PositionEntry newEntry = new PositionEntry(finalPos, "march");
        marcher.countPositions[set][count] = newEntry;

        // 🗂️ Tag the core confirmed change properly
        positionHistory.RecordChange(
            marcher,
            set,
            count,
            oldEntry,
            newEntry,
            alreadyConfirmed ? MarcherPositionHistory.ChangeIntent.Reconfirm
                            : MarcherPositionHistory.ChangeIntent.ConfirmedMove
        );

        // 🔁 Interpolate surrounding counts
        var interpolator = new MarcherInterpolator(marcher, GetMaxCountForSet, () => marcher.transform.position);

        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos))
        {
            var backSteps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, finalPos, backSteps);
        }

        if (interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos))
        {
            var forwardSteps = interpolator.GetInterpolationSteps(set, count + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(finalPos, nextPos, forwardSteps);
        }

        // 🧠 Capture state after interpolation
        Dictionary<(int set, int count), PositionEntry> afterSnapshot = GetAffectedPositionSnapshot(marcher, set, count);

        // 🔁 Record snapshot changes as Reconfirm entries (only for surrounding counts)
        foreach (var kvp in beforeSnapshot)
        {
            var key = kvp.Key;
            if (key.set == set && key.count == count) continue; // skip the main dot (already recorded)

            PositionEntry before = kvp.Value;
            PositionEntry after = afterSnapshot.ContainsKey(key) ? afterSnapshot[key] : new PositionEntry();

            positionHistory.RecordChange(
                marcher,
                key.set,
                key.count,
                before,
                after,
                MarcherPositionHistory.ChangeIntent.Reconfirm
            );
        }

        //positionHistory.EndBatch();

        SyncAndNotify(marcher);

        // 👁️ Refresh visuals
        int visualizeSet = (set == 0) ? 1 : set;
        if (director != null)
            director.VisualizePathsForSet(visualizeSet);
    }



    /// <summary>
    /// Delete a confirmed dot and reinterpolate the surrounding area.
    /// </summary>
    public bool DeleteConfirmedPosition(MarcherPositionsManager marcher, int set, int count, out bool hadNextConfirmed)
    {
        hadNextConfirmed = false;
        int lastCount = GetMaxCountForSet(set);
        Debug.Log($"🗑 Attempting to delete Set {set}, Count {count} for {marcher.name}");

        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcher,
            GetMaxCountForSet,
            () => marcher.transform.position
        );

        // 🛑 Block deletion if this is the final count AND it has a next confirmed dot
        if (count == lastCount && interpolator.TryFindNextConfirmedPosition(set, count, out _, out _, out _))
        {
            Debug.LogWarning($"❌ Cannot delete confirmed position at final count {count} of Set {set} — future confirmed exists.");
            return false;
        }

        // 🛑 Block if no confirmed position exists
        if (!marcher.HasPositionAtCount(set, count) || !marcher.countPositions[set][count].IsConfirmed)
        {
            Debug.LogWarning($"⚠️ No confirmed dot found at Set {set}, Count {count} for {marcher.name}");
            return false;
        }

        // ✅ Delete the confirmed entry
        PositionEntry oldEntry = marcher.countPositions[set][count];
        positionHistory.RecordChange(marcher, set, count, oldEntry, new PositionEntry());
        marcher.countPositions[set].Remove(count);
        Debug.Log($"✅ Removed confirmed position: Set {set}, Count {count} for {marcher.name}");

        bool hasNext = interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos);
        bool hasPrev = interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos);
        hadNextConfirmed = hasNext;

        // 🔁 CASE 1: No next confirmed — clean inferred positions between this and fallback, then reposition
        if (!hasNext && hasPrev)
        {
            Debug.Log($"📉 No next confirmed dot. Cleaning up inferred counts backwards from Set {set}, Count {count}");

            int inferredRemoved = 0;
            int startSet = Math.Min(set, prevSet);
            int endSet = Math.Max(set, prevSet);

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
                    continue; // do not clean inferred from fallback confirmed set
                }
                else
                {
                    startCount = 1;
                    endCount = GetMaxCountForSet(s);
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

        // 🔁 CASE 2: Re-interpolate between prev and next confirmed
        if (hasPrev && hasNext)
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

        Debug.Log($"⚠️ No valid re-interpolation range found after deleting Set {set}, Count {count}");
        SyncAndNotify(marcher);
        return true;
    }

    private Dictionary<(int set, int count), PositionEntry> GetAffectedPositionSnapshot(MarcherPositionsManager marcher, int set, int count)
    {
        var snapshot = new Dictionary<(int set, int count), PositionEntry>();
        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcher,
            GetMaxCountForSet,
            () => marcher.transform.position
        );

        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out _))
        {
            if (interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out _))
            {
                var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
                foreach (var (s, c) in steps)
                {
                    if (marcher.countPositions.TryGetValue(s, out var counts) &&
                        counts.TryGetValue(c, out var entry))
                    {
                        snapshot[(s, c)] = new PositionEntry(entry.pos, entry.type);
                    }
                }
            }
        }

        return snapshot;
    }


    /// <summary>
    /// Ensure set exists in the dictionary before writing.
    /// </summary>
    private void EnsureSetExists(MarcherPositionsManager marcher, int set)
    {
        if (!marcher.countPositions.ContainsKey(set))
            marcher.countPositions[set] = new Dictionary<int, PositionEntry>();
    }
    

    public int GetMaxCountForSet(int setIndex)
    {
        return sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count : 8;
    }

    private void SyncAndNotify(MarcherPositionsManager marcher)
    {
        marcher.SyncInspectorList();
        OnAnyMarcherPositionUpdated.Invoke();
    }
}
