using System.Collections.Generic;
using UnityEngine;

public class LinesManager : MonoBehaviour
{
    private SnapToGridLines snapToGrid;
    private IntervalManager intervalManager;
    private GameObject marcherPrefab;
    private GameObject positionSpherePrefab;
    private float marcherSpacing;

    // Initialize method to set up necessary references
    public void Initialize(GameObject marcherPrefab, GameObject positionSpherePrefab, float marcherSpacing)
    {
        this.marcherPrefab = marcherPrefab;
        this.positionSpherePrefab = positionSpherePrefab;
        this.marcherSpacing = marcherSpacing;

        // Get necessary component in the scene if not already assigned
        snapToGrid = FindObjectOfType<SnapToGridLines>();

        if (snapToGrid == null)
        {
            Debug.LogError("SnapToGridLines component not found in the scene.");
        }
    }

    // Method to create a line formation
    public void CreateLineFormation(List<GameObject> marchers, IntervalManager.IntervalType interval)
    {
        // Calculate spacing based on the interval
        intervalManager = FindObjectOfType<IntervalManager>();
        float spacing = intervalManager.GetIntervalSpacing(interval);

        Debug.Log($"Creating Line Formation - Total Marchers: {marchers.Count}, Spacing: {spacing}");

        for (int i = 0; i < marchers.Count; i++)
        {
            // Calculate position for each marcher in the line based on angle and index
            Vector3 position = CalculateLinePosition(Vector3.zero, 0, i, spacing);

            // Snap position to the grid if snapToGrid is available
            if (snapToGrid != null)
            {
                position = snapToGrid.GetSnappedPosition(position);
            }

            // Set marcher's position
            marchers[i].transform.position = position;
        }
    }

    // Helper method to calculate position of each marcher in the line
    private Vector3 CalculateLinePosition(Vector3 startPosition, float angle, int index, float spacing)
    {
        // Calculate position offset based on angle and spacing
        float xOffset = index * spacing * Mathf.Cos(angle * Mathf.Deg2Rad);
        float zOffset = index * spacing * Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector3(startPosition.x + xOffset, startPosition.y, startPosition.z + zOffset);
    }
}
