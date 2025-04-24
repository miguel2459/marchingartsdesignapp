using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages confirmed and inferred per-count position data for a marcher.
/// </summary>
public class MarcherPositionsManager : MonoBehaviour
{
    [HideInInspector] public MarcherPathVisualizer pathVisualizer;

    public Dictionary<int, Dictionary<int, PositionEntry>> countPositions = new Dictionary<int, Dictionary<int, PositionEntry>>();

    public List<CountPositionEntry> inspectorCountPositions = new List<CountPositionEntry>();
    public static event System.Action OnAnyMarcherPositionUpdated;
    private MarcherInterpolator interpolator;

    private void Awake()
    {
        pathVisualizer = GetComponent<MarcherPathVisualizer>();
        interpolator = new MarcherInterpolator(
            this,
            GetMaxCountForSet,
            () => transform.position
        );
    }
    public static void RaisePositionUpdatedEvent()
    {
        OnAnyMarcherPositionUpdated?.Invoke();
    }

    public Dictionary<int, Dictionary<int, PositionEntry>> GetAllCountPositions()
    {
        return countPositions;
    }

    public void ShowPath(Vector3[] path)
    {
        pathVisualizer?.ShowPath(path);
    }

    public void HidePath()
    {
        pathVisualizer?.HidePath();
    }

    public Vector3 GetPositionAtCount(int setIndex, int countIndex)
    {
        // 1. Check current set
        if (countPositions.TryGetValue(setIndex, out var currentSetCounts))
        {
            // 1a. Direct match
            if (currentSetCounts.TryGetValue(countIndex, out var directEntry) && !directEntry.IsUnset)
                return directEntry.pos;

            // 1b. Fallback within current set
            for (int i = countIndex - 1; i >= 1; i--)
            {
                if (currentSetCounts.TryGetValue(i, out var fallback) && fallback.IsConfirmed)
                    return fallback.pos;
            }
        }

        // 2. Fallback to last confirmed position in previous set
        for (int s = setIndex - 1; s >= 0; s--)
        {
            if (countPositions.TryGetValue(s, out var previousSetCounts))
            {
                // Find the highest confirmed count in the previous set
                for (int i = 100; i >= 0; i--) // Replace 100 with expected max count per set
                {
                    if (previousSetCounts.TryGetValue(i, out var prevEntry) && prevEntry.IsConfirmed)
                        return prevEntry.pos;
                }
            }
        }

        // 3. Fallback default if nothing is found
        return transform.position;
    }

    public bool HasPositionAtCount(int setIndex, int countIndex)
    {
        return countPositions.ContainsKey(setIndex) && countPositions[setIndex].ContainsKey(countIndex)
            && !countPositions[setIndex][countIndex].IsUnset;
    }

    public Vector3[] GetInterpolatedPath(int setIndex, int totalCounts, Vector3 fromPosition)
    {
        List<Vector3> path = new List<Vector3>();

        // Start from final position of previous set
        path.Add(fromPosition);

        for (int i = 1; i <= totalCounts; i++)
        {
            path.Add(GetPositionAtCount(setIndex, i));
        }

        return path.ToArray();
    }

    public void ClearAllPositions()
    {
        countPositions.Clear();
        inspectorCountPositions.Clear();
    }

    public void InitializeSetCount(int totalSets)
    {
        Debug.Log($"{name} initialized with {totalSets} sets (count-level positioning).");
    }

    public string GetTagForCount(int setIndex, int countIndex)
    {
        if (HasPositionAtCount(setIndex, countIndex))
            return countPositions[setIndex][countIndex].type;
        return "unset";
    }

    public bool IsUnsetAt(int setIndex, int countIndex)
    {
        return countPositions.ContainsKey(setIndex)
            && countPositions[setIndex].ContainsKey(countIndex)
            && countPositions[setIndex][countIndex].IsUnset;
    }

    // public void DeleteConfirmedPosition(int setIndex, int countIndex)
    // {
    //     int lastCount = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing)
    //         ? timing.count : -1;
    //     if (countIndex == lastCount)
    //     {
    //         Debug.LogWarning($"{name} ❌ Cannot delete confirmed position at last count of Set {setIndex}");
    //         return;
    //     }

    //     if (!HasPositionAtCount(setIndex, countIndex) || !countPositions[setIndex][countIndex].IsConfirmed)
    //     {
    //         Debug.LogWarning($"{name} ❌ No confirmed position at Set {setIndex}, Count {countIndex} to delete.");
    //         return;
    //     }

    //     countPositions[setIndex].Remove(countIndex);
    //     Debug.Log($"{name} 🗑 Deleted confirmed position at Set {setIndex}, Count {countIndex}");

    //     bool hasPrev = interpolator.TryFindLastConfirmedPosition(setIndex, countIndex, out int prevSet, out int prevCount, out Vector3 prevPos);
    //     bool hasNext = interpolator.TryFindNextConfirmedPosition(setIndex, countIndex, out int nextSet, out int nextCount, out Vector3 nextPos);

    //     if (!hasPrev)
    //     {
    //         Debug.LogWarning($"{name} ⚠️ No previous confirmed position found. Path cannot be recalculated.");
    //     }

    //     if (hasPrev && !hasNext)
    //     {
    //         pathVisualizer?.HidePath();
    //     }

    //     if (hasPrev && hasNext)
    //     {
    //         Debug.Log($"{name} 🔁 Recalculating path from Set {prevSet}, Count {prevCount} ➡ Set {nextSet}, Count {nextCount}");

    //         List<(int s, int c)> steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
    //         interpolator.ApplyInterpolatedPositions(prevPos, nextPos, steps);

    //         if (countPositions.TryGetValue(setIndex, out var currentSet) &&
    //             currentSet.TryGetValue(countIndex, out var interpolated))
    //         {
    //             transform.position = interpolated.pos;
    //             Debug.Log($"{name} ⬅️ Snapped to recalculated inferred position at Set {setIndex}, Count {countIndex}: {interpolated.pos}");
    //         }

    //         if (TryGetComponent<Unit>(out var unit))
    //         {
    //             unit.SetSelector(false);
    //         }

    //         Debug.Log($"{name} ✅ Refilled {steps.Count} inferred counts between confirmed anchors.");
    //     }

    //     SyncInspectorList();
    //     OnAnyMarcherPositionUpdated?.Invoke();
    // }


    // public void ConfirmMarchAndFillBack(int targetSet, int targetCount, Vector3 finalPos)
    // {
    //     // === EDGE CASE: Set 0, Count 0 ===
    //     if (targetSet == 0 && targetCount == 0)
    //     {
    //         if (!countPositions.ContainsKey(0))
    //             countPositions[0] = new Dictionary<int, PositionEntry>();

    //         countPositions[0][0] = new PositionEntry(finalPos, "march");
    //         SyncInspectorList();
    //         OnAnyMarcherPositionUpdated?.Invoke();
    //         Debug.Log($"{name} ✅ Directly confirmed Set 0, Count 0 (march) at {finalPos}");
    //         return;
    //     }

    //     // === Ensure Set Exists ===
    //     if (!countPositions.ContainsKey(targetSet))
    //         countPositions[targetSet] = new Dictionary<int, PositionEntry>();

    //     // === Confirm Current Position ===
    //     countPositions[targetSet][targetCount] = new PositionEntry(finalPos, "march");
    //     Debug.Log($"{name} ✅ Confirmed: Set {targetSet}, Count {targetCount} → {finalPos}");

    //     // === BACKWARD INTERPOLATION ===
    //     if (interpolator.TryFindLastConfirmedPosition(targetSet, targetCount, out int prevSet, out int prevCount, out Vector3 prevPos))
    //     {
    //         Debug.Log($"{name} 🔙 Interpolating from Set {prevSet}, Count {prevCount} → Set {targetSet}, Count {targetCount}");
    //         var backSteps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, targetSet, targetCount - 1);
    //         interpolator.ApplyInterpolatedPositions(prevPos, finalPos, backSteps);
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"{name} ⚠️ No previous confirmed position found before Set {targetSet}, Count {targetCount}");
    //     }

    //     // === FORWARD INTERPOLATION ===
    //     if (interpolator.TryFindNextConfirmedPosition(targetSet, targetCount, out int nextSet, out int nextCount, out Vector3 nextPos))
    //     {
    //         Debug.Log($"{name} 🔜 Interpolating forward to Set {nextSet}, Count {nextCount}");
    //         var fwdSteps = interpolator.GetInterpolationSteps(targetSet, targetCount + 1, nextSet, nextCount - 1);
    //         interpolator.ApplyInterpolatedPositions(finalPos, nextPos, fwdSteps);
    //     }

    //     // === Visual Feedback ===
    //     if (TryGetComponent<Unit>(out var unit))
    //         unit.SetSelector(true);

    //     SyncInspectorList();
    //     OnAnyMarcherPositionUpdated?.Invoke();
    // }
    public void SyncInspectorList()
    {
        inspectorCountPositions = new List<CountPositionEntry>();

        foreach (var set in countPositions)
        {
            foreach (var count in set.Value)
            {
                inspectorCountPositions.Add(new CountPositionEntry
                {
                    setIndex = set.Key,
                    countIndex = count.Key,
                    position = count.Value.pos,
                    type = count.Value.type
                });
            }
        }
    }
    private int GetMaxCountForSet(int setIndex)
    {
        return SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count
            : 100;
    }
    [System.Serializable]
    public class CountPositionEntry
    {
        public int setIndex;
        public int countIndex;
        public Vector3 position;
        public string type;
    }
}
