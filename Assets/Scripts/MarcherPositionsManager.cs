using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SetPositionEntry
{
    public int setIndex;
    public Vector3 position;
}


/// <summary>
/// Stores editable position data for this marcher across all sets:
/// - setPositions: confirmed with spacebar
/// - standbyPositions: moved but not yet confirmed
/// </summary>
public class MarcherPositionsManager : MonoBehaviour
{
    // ✅ Vector3-only position storage
    public Dictionary<int, Vector3> setPositions = new Dictionary<int, Vector3>();
    public Dictionary<int, Vector3> standbyPositions = new Dictionary<int, Vector3>();

    [SerializeField] private List<SetPositionEntry> inspectorSetPositions = new List<SetPositionEntry>();
    [SerializeField] private List<SetPositionEntry> inspectorStandbyPositions = new List<SetPositionEntry>();



    private DotMarkerVisualizer visualizer;

    private void Awake()
    {
        visualizer = GetComponent<DotMarkerVisualizer>();
    }

    /// <summary>
    /// Confirms a position for a set (user pressed spacebar).
    /// Adds to setPositions, clears standby, and updates visual.
    /// </summary>
    public void SetPosition(int setIndex, Vector3 position)
    {
        setPositions[setIndex] = position;
        standbyPositions.Remove(setIndex);

        visualizer?.ShowMarker(setIndex, position);
        SyncInspectorLists();
        
        Debug.Log($"{name} ✅ SetPosition recorded for Set {setIndex}: {position}");
    }

    /// <summary>
    /// Saves a temporary (unsaved) position change for a set.
    /// Used for editing workflows.
    /// </summary>
    public void SaveStandbyPosition(int setIndex, Vector3 position)
    {
        if (!setPositions.ContainsKey(setIndex))
        {
            standbyPositions[setIndex] = position;
            SyncInspectorLists();

            Debug.Log($"{name} 💾 StandbyPosition saved for Set {setIndex}: {position}");
        }
        else
        {
            Debug.Log($"{name} ⛔ Set {setIndex} already confirmed — standby not saved.");
        }
    }

    /// <summary>
    /// Gets the current active position for a set (set or standby).
    /// </summary>
    public Vector3 GetCurrentPosition(int setIndex)
    {
        if (setPositions.TryGetValue(setIndex, out Vector3 setPos))
            return setPos;

        if (standbyPositions.TryGetValue(setIndex, out Vector3 standbyPos))
            return standbyPos;

        return transform.position;
    }

    /// <summary>
    /// Returns true if a set position has been confirmed.
    /// </summary>
    public bool HasSetPosition(int setIndex) => setPositions.ContainsKey(setIndex);

    /// <summary>
    /// Clears all saved positions and visuals.
    /// </summary>
    public void ClearAllPositions()
    {
        setPositions.Clear();
        standbyPositions.Clear();
        visualizer?.ClearAllMarkers();
    }

    /// <summary>
    /// Used by the Director to ensure this marcher is aware of new set count.
    /// </summary>
    public void InitializeSetCount(int totalSets)
    {
        // Optionally preload standby for each set (or leave dynamic)
        Debug.Log($"{name} initialized with {totalSets} potential sets.");
    }

    /// <summary>
    /// Returns all set positions sorted by set index (for animation).
    /// </summary>
    public Vector3[] GetAllSetPositionsSorted()
    {
        List<Vector3> sortedPositions = new List<Vector3>();
        for (int i = 0; i < SessionManager.instance.showStateSO.NumberOfSets; i++)
        {
            if (setPositions.TryGetValue(i + 1, out Vector3 pos)) // 1-based set index
            {
                sortedPositions.Add(pos);
            }
        }
        return sortedPositions.ToArray();
    }

    public void SyncInspectorLists()
    {
        inspectorSetPositions = new List<SetPositionEntry>();
        foreach (var kvp in setPositions)
        {
            inspectorSetPositions.Add(new SetPositionEntry { setIndex = kvp.Key, position = kvp.Value });
        }

        inspectorStandbyPositions = new List<SetPositionEntry>();
        foreach (var kvp in standbyPositions)
        {
            inspectorStandbyPositions.Add(new SetPositionEntry { setIndex = kvp.Key, position = kvp.Value });
        }
    }

}
