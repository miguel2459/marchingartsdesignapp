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
        return interpolator.GetBestPosition(setIndex, countIndex);
    }

    public bool HasPositionAtCount(int setIndex, int countIndex)
    {
        return countPositions.ContainsKey(setIndex) && countPositions[setIndex].ContainsKey(countIndex)
            && !countPositions[setIndex][countIndex].IsUnset;
    }
    public bool HasConfirmedOrInferredAtSet(int set)
    {
        if (!countPositions.TryGetValue(set, out var map)) return false;

        foreach (var entry in map.Values)
        {
            if (entry.IsConfirmed || entry.IsInferred)
                return true;
        }

        return false;
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

    public void InitializeSetCount(int totalSets)
    {
        //Debug.Log($"{name} initialized with {totalSets} sets (count-level positioning).");
    }

    public string GetTagForCount(int setIndex, int countIndex)
    {
        if (HasPositionAtCount(setIndex, countIndex))
            return countPositions[setIndex][countIndex].type;
        return "unset";
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

    public bool TryGetConfirmedPosition(int setIndex, int countIndex, out Vector3 pos)
    {
        pos = Vector3.zero;

        if (countPositions.TryGetValue(setIndex, out var setMap) &&
            setMap.TryGetValue(countIndex, out var entry) &&
            entry.IsConfirmed)
        {
            pos = entry.pos;
            return true;
        }

        return false;
    }

    public void LoadPositions(Dictionary<int, Dictionary<int, PositionEntry>> loadedPositions)
    {
        countPositions = loadedPositions;
        SyncInspectorList();
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
