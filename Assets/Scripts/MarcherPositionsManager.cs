using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages confirmed and inferred per-count position data for a marcher.
/// </summary>
public class MarcherPositionsManager : MonoBehaviour
{
    public Dictionary<int, Dictionary<int, PositionEntry>> countPositions = new Dictionary<int, Dictionary<int, PositionEntry>>();

    public List<CountPositionEntry> inspectorCountPositions = new List<CountPositionEntry>();

    private DotMarkerVisualizer visualizer;
    public static event System.Action OnAnyMarcherPositionUpdated;

    private void Awake()
    {
        visualizer = GetComponent<DotMarkerVisualizer>();
    }

    public Dictionary<int, Dictionary<int, PositionEntry>> GetAllCountPositions()
    {
        return countPositions;
    }

    public void SetPositionAtCount(int setIndex, int countIndex, Vector3 position, string type = "march")
    {
        if (!countPositions.ContainsKey(setIndex))
            countPositions[setIndex] = new Dictionary<int, PositionEntry>();

        countPositions[setIndex][countIndex] = new PositionEntry(position, type);
        visualizer?.ShowMarker(setIndex, position);
        SyncInspectorList();

        Debug.Log($"{name} ✅ SetPosition recorded: Set {setIndex}, Count {countIndex}, Type={type}, Pos={position}");
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
        visualizer?.ClearAllMarkers();
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

    private bool TryFindLastConfirmedPosition(int currentSetIndex, int beforeCountIndex, out int foundSetIndex, out int foundCountIndex, out Vector3 pos)
    {
        foundSetIndex = -1;
        foundCountIndex = -1;
        pos = Vector3.zero;

        // 1. Look in the current set first
        if (countPositions.TryGetValue(currentSetIndex, out var currentSet))
        {
            for (int i = beforeCountIndex - 1; i >= 1; i--)
            {
                if (currentSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = currentSetIndex;
                    foundCountIndex = i;
                    pos = entry.pos;
                    Debug.Log($"{name} 🔎 Found confirmed in current set {currentSetIndex}, Count {i} → {pos}");
                    return true;
                }
            }
        }

        // 2. Walk backward through previous sets
        for (int s = currentSetIndex - 1; s >= 0; s--)
        {
            if (!countPositions.TryGetValue(s, out var previousSet)) continue;

            int maxCount = GetMaxCountForSet(s);
            int minCount = (s == 0) ? 0 : 1;  // ✅ Only allow Count 0 in Set 0

            for (int i = maxCount; i >= minCount; i--)
            {
                if (previousSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = s;
                    foundCountIndex = i;
                    pos = entry.pos;
                    Debug.Log($"{name} 🔁 Fallback to Set {s}, Count {i} → {pos}");
                    return true;
                }
            }
        }

        Debug.LogWarning($"{name} 🛑 No confirmed point found before Set {currentSetIndex}, Count {beforeCountIndex}");
        return false;
    }
    public void ConfirmMarchAndFillBack(int targetSet, int targetCount, Vector3 finalPos)
    {
        if (targetSet == 0 && targetCount == 0)
        {
            if (!countPositions.ContainsKey(0))
                countPositions[0] = new Dictionary<int, PositionEntry>();

            countPositions[0][0] = new PositionEntry(finalPos, "march");
            SyncInspectorList();
            OnAnyMarcherPositionUpdated?.Invoke();
            Debug.Log($"{name} ✅ Directly confirmed Set 0, Count 0 (march) at {finalPos}");
            return;
        }

        if (!countPositions.ContainsKey(targetSet))
            countPositions[targetSet] = new Dictionary<int, PositionEntry>();

        if (!TryFindLastConfirmedPosition(targetSet, targetCount, out int startSet, out int startCount, out Vector3 startPos))
        {
            Debug.LogWarning($"{name} 🧩 No confirmed point before Set {targetSet}, Count {targetCount}");
            return;
        }

        Debug.Log($"{name} 🧠 Interpolating from Set {startSet}, Count {startCount} to Set {targetSet}, Count {targetCount}");

        List<(int set, int count)> steps = new List<(int set, int count)>();

        for (int s = startSet; s <= targetSet; s++)
        {
            int startC = (s == startSet) ? startCount + 1 : 1;

            if (s == 0) //Never interpolate in Set 0
            continue;

            int endC = (s == targetSet) 
            ? targetCount - 1 
            : (s == startSet ? GetMaxCountForSet(s) : Mathf.Min(GetMaxCountForSet(s), targetCount - 1));

            if (endC < startC) continue;

            for (int c = startC; c <= endC; c++)
            {
                if (c > GetMaxCountForSet(s)) break;
                steps.Add((s, c));
            }
        }

        int totalSteps = steps.Count + 1;
        for (int i = 0; i < steps.Count; i++)
        {
            float t = (i + 1f) / totalSteps;
            Vector3 pos = Vector3.Lerp(startPos, finalPos, t);
            int s = steps[i].set;
            int c = steps[i].count;

            if (!countPositions.ContainsKey(s))
                countPositions[s] = new Dictionary<int, PositionEntry>();

            countPositions[s][c] = new PositionEntry(pos, "inferred");
            Debug.Log($"{name} 🟡 Inferred: Set {s}, Count {c} → {pos}");
        }

        countPositions[targetSet][targetCount] = new PositionEntry(finalPos, "march");
        Debug.Log($"{name} ✅ Confirmed: Set {targetSet}, Count {targetCount} → {finalPos}");

        SyncInspectorList();
        OnAnyMarcherPositionUpdated?.Invoke();
    }
    public void ConfirmHoldAndFillBack(int setIndex, int targetCount, Vector3 pos)
    {
        if (setIndex == 0 && targetCount == 0)
        {
            if (!countPositions.ContainsKey(0))
                countPositions[0] = new Dictionary<int, PositionEntry>();

            countPositions[0][0] = new PositionEntry(pos, "hold");
            SyncInspectorList();
            OnAnyMarcherPositionUpdated?.Invoke();
            Debug.Log($"{name} ✅ Directly confirmed Set 0, Count 0 (hold) at {pos}");
            return;
        }
        if (!countPositions.ContainsKey(setIndex))
            countPositions[setIndex] = new Dictionary<int, PositionEntry>();

        for (int i = 1; i < targetCount; i++)
        {
            if (!countPositions[setIndex].ContainsKey(i) || countPositions[setIndex][i].IsUnset)
            {
                countPositions[setIndex][i] = new PositionEntry(pos, "inferred"); // 🟡 inferred
            }
        }

        countPositions[setIndex][targetCount] = new PositionEntry(pos, "hold"); // ✅ user confirmed
        SyncInspectorList();
        OnAnyMarcherPositionUpdated?.Invoke();
    }

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
