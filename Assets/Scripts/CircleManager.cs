using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
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

        // Get necessary components in the scene if not already assigned
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

    // Method to create a circular formation
    public void CreateCircleFormation(List<GameObject> marchers, IntervalManager.IntervalType interval, bool isFilled = false)
    {
        float radius = 12;
        float spacing = intervalManager.GetIntervalSpacing(interval);

        if (isFilled)
        {
            ArrangeFilledCircle(marchers, radius, spacing);
        }
        else
        {
            ArrangeHollowCircle(marchers, radius, spacing);
        }
    }

    // Arrange marchers in a filled circular formation
    private void ArrangeFilledCircle(List<GameObject> marchers, float radius, float spacing)
    {
        int marcherIndex = 0;

        // Loop to create multiple concentric circles from inner radius to outer radius
        for (float currentRadius = spacing; currentRadius <= radius; currentRadius += spacing)
        {
            int pointsInCircle = Mathf.RoundToInt(2 * Mathf.PI * currentRadius / spacing);

            for (int i = 0; i < pointsInCircle; i++)
            {
                if (marcherIndex >= marchers.Count) return;

                // Calculate the angle and position for each marcher
                float theta = i * 2 * Mathf.PI / pointsInCircle;
                float x = currentRadius * Mathf.Cos(theta);
                float z = currentRadius * Mathf.Sin(theta);

                Vector3 position = snapToGrid.GetSnappedPosition(new Vector3(x, 0.76f, z));
                marchers[marcherIndex++].transform.position = position;
            }
        }
    }

    // Arrange marchers in a hollow circle formation
    private void ArrangeHollowCircle(List<GameObject> marchers, float radius, float spacing)
    {
        int pointsInCircle = Mathf.RoundToInt(2 * Mathf.PI * radius / spacing);

        for (int i = 0; i < pointsInCircle && i < marchers.Count; i++)
        {
            // Calculate the angle and position for each marcher
            float theta = i * 2 * Mathf.PI / pointsInCircle;
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);

            Vector3 position = snapToGrid.GetSnappedPosition(new Vector3(x, 0.76f, z));
            marchers[i].transform.position = position;
        }
    }
}
