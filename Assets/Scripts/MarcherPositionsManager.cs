using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages confirmed and inferred per-count position data for a marcher.
/// </summary>
public class MarcherPositionsManager : MonoBehaviour
{
    [HideInInspector] public MarcherConfirmedPathVisualizer pathVisualizer;

    public Dictionary<int, Dictionary<int, PositionEntry>> countPositions = new Dictionary<int, Dictionary<int, PositionEntry>>();

    public List<CountPositionEntry> inspectorCountPositions = new List<CountPositionEntry>();
    public static event System.Action OnAnyMarcherPositionUpdated;
    private MarcherInterpolator interpolator;

    private void Awake()
    {
        pathVisualizer = GetComponentInChildren<MarcherConfirmedPathVisualizer>();
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
    public bool IsHoldingAtCount(int setIndex, int countIndex)
    {
        if (countIndex <= 0 || !HasPositionAtCount(setIndex, countIndex))
            return false;

        Vector3 current = GetPositionAtCount(setIndex, countIndex);
        Vector3 previous;

        if (countIndex > 1)
        {
            previous = GetPositionAtCount(setIndex, countIndex - 1);
        }
        else
        {
            // Edge case: countIndex == 1 → fallback to last count of previous set
            int prevSet = setIndex - 1;
            if (prevSet < 0) return false;

            int fallbackCount = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(prevSet, out var timing)
                ? timing.count : 0;

            if (fallbackCount == 0 || !HasPositionAtCount(prevSet, fallbackCount))
                return false;

            previous = GetPositionAtCount(prevSet, fallbackCount);
        }

        return Vector3.Distance(current, previous) < 0.01f;
    }

    public Vector3[] GetConfirmedPath()
    {
        List<Vector3> confirmedPoints = new List<Vector3>();

        foreach (var setEntry in countPositions)
        {
            foreach (var countEntry in setEntry.Value)
            {
                var positionEntry = countEntry.Value;
                if (positionEntry != null && positionEntry.IsConfirmed)
                {
                    confirmedPoints.Add(positionEntry.pos);
                }
            }
        }

        return confirmedPoints.ToArray();
    }
    

    public bool HasFullProgressForSet(int setIndex)
    {
        if (!countPositions.TryGetValue(setIndex, out var counts))
            return false;

        int maxCount = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count : 8;

        for (int i = 1; i <= maxCount; i++)
        {
            if (!counts.TryGetValue(i, out var entry) || entry.IsUnset)
                return false;
        }

        return true;
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
