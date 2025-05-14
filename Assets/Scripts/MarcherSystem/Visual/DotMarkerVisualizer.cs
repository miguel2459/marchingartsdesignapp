using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages visual dot markers (e.g., DotMarker.prefab) for this specific marcher.
/// One marker per confirmed setPosition. Prefab includes optional label.
/// </summary>
public class DotMarkerVisualizer : MonoBehaviour
{
    [Header("Prefab Reference")]
    public GameObject dotMarkerPrefab;

    [Header("Offset Settings")]
    public float yOffset = 0.00f;

    // Key: setIndex → Value: marker GameObject
    private Dictionary<int, GameObject> markers = new Dictionary<int, GameObject>();


    /// <summary>
    /// Show or update a dot marker at a given position for a specific set.
    /// </summary>
    public void ShowMarker(int setIndex, Vector3 position)
    {
        if (markers.TryGetValue(setIndex, out GameObject existingMarker))
        {
            existingMarker.transform.position = position + Vector3.up * yOffset;
            return;
        }

        // Create new marker
        GameObject marker = Instantiate(dotMarkerPrefab, position + Vector3.up * yOffset, Quaternion.identity, transform);
        marker.name = $"Set{setIndex}_DotMarker";

        // Update label if available
        TextMeshPro tmp = marker.GetComponentInChildren<TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = $"Set {setIndex}";
        }

        markers[setIndex] = marker;
    }

    /// <summary>
    /// Remove marker for a specific set.
    /// </summary>
    public void HideMarker(int setIndex)
    {
        if (markers.TryGetValue(setIndex, out GameObject marker))
        {
            Destroy(marker);
            markers.Remove(setIndex);
        }
    }

    /// <summary>
    /// Clear all markers for this marcher.
    /// </summary>
    public void ClearAllMarkers()
    {
        foreach (var marker in markers.Values)
        {
            Destroy(marker);
        }
        markers.Clear();
    }

    /// <summary>
    /// Check if this marcher has a dot marker for the given set index.
    /// </summary>
    public bool HasMarker(int setIndex) => markers.ContainsKey(setIndex);
}
