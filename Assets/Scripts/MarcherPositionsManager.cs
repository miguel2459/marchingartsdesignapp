using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CountPositionEntry
{
    public int setIndex;
    public int countIndex;
    public Vector3 position;
}

/// <summary>
/// Manages confirmed and inferred per-count position data for a marcher.
/// </summary>
public class MarcherPositionsManager : MonoBehaviour
{
    public Dictionary<int, Dictionary<int, Vector3>> countPositions = new Dictionary<int, Dictionary<int, Vector3>>();

    [SerializeField] private List<CountPositionEntry> inspectorCountPositions = new List<CountPositionEntry>();

    private DotMarkerVisualizer visualizer;

    private void Awake()
    {
        visualizer = GetComponent<DotMarkerVisualizer>();
    }

    public void SetPositionAtCount(int setIndex, int countIndex, Vector3 position)
    {
        if (!countPositions.ContainsKey(setIndex))
            countPositions[setIndex] = new Dictionary<int, Vector3>();

        countPositions[setIndex][countIndex] = position;

        visualizer?.ShowMarker(setIndex, position);
        SyncInspectorList();

        Debug.Log($"{name} ✅ SetPosition recorded for Set {setIndex}, Count {countIndex}: {position}");
    }

    public Vector3 GetPositionAtCount(int setIndex, int countIndex)
    {
        if (countPositions.TryGetValue(setIndex, out var counts))
        {
            if (counts.TryGetValue(countIndex, out var position))
                return position;

            // Try inferred hold: find the latest count less than this one
            for (int i = countIndex - 1; i >= 1; i--)
            {
                if (counts.TryGetValue(i, out var fallback))
                    return fallback;
            }
        }

        return transform.position; // Fallback to current position if nothing else found
    }

    public bool HasPositionAtCount(int setIndex, int countIndex)
    {
        return countPositions.ContainsKey(setIndex) && countPositions[setIndex].ContainsKey(countIndex);
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
                    position = count.Value
                });
            }
        }
    }

    public Dictionary<int, Dictionary<int, Vector3>> GetAllCountPositions() => countPositions;
}
