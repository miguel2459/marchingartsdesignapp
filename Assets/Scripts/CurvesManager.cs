using System.Collections.Generic;
using UnityEngine;

public class CurvesManager : MonoBehaviour
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

        // Get necessary components in the scene if not assigned
        snapToGrid = FindObjectOfType<SnapToGridLines>();
        intervalManager = FindObjectOfType<IntervalManager>();

        if (snapToGrid == null)
        {
            Debug.LogError("SnapToGridLines component not found in the scene.");
        }

        if (intervalManager == null)
        {
            Debug.LogError("IntervalManager component not found in the scene.");
        }
    }

    // Method to create a curve formation for marchers
    public void CreateCurveFormation(List<GameObject> marchers, IntervalManager.IntervalType interval)
    {
        float radius = 12;
        float spacing = intervalManager.GetIntervalSpacing(interval);

        for (int i = 0; i < marchers.Count; i++)
        {
            // Calculate the position in the curve based on spacing, radius, and angle
            float theta = i * spacing / radius;
            float x = radius * Mathf.Cos(theta + 2 * Mathf.Deg2Rad);
            float z = radius * Mathf.Sin(theta + 2 * Mathf.Deg2Rad);

            // Set each marcher position using SnapToGrid to align on the grid
            Vector3 targetPosition = new Vector3(x, 0.76f, z);
            marchers[i].transform.position = snapToGrid.GetSnappedPosition(targetPosition);
        }
    }
}
