using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    public SnapToGridLines snapToGrid;
    public FieldGridManager fieldManager;
    public IntervalManager intervalManager;

    // Initialize method to set up necessary references
    public void Initialize()
    {
        if (snapToGrid == null)
        {
            Debug.LogError("SnapToGridLines component not found in the scene.");
        }

        if (intervalManager == null)
        {
            Debug.LogError("IntervalManager component not found in the scene. Trying to assign manually...");
            intervalManager = new GameObject("IntervalManager").AddComponent<IntervalManager>(); // Create dynamically if missing
        }
    }

    // Method to create a box formation for marchers
    public void CreateBoxFormation(List<GameObject> marchers, IntervalManager.IntervalType interval, bool isFilled)
    {
        Vector3 center = fieldManager.GetFieldCenter();
        int count = marchers.Count;
        float spacing = intervalManager.GetIntervalSpacing(interval);

        // New logic: round up cols and rows to ensure all marchers fit
        int numCols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int numRows = Mathf.CeilToInt((float)count / numCols);

        Debug.Log($"✅ Rectangular Grid Calculated - Total: {count} | Rows: {numRows}, Columns: {numCols}, Spacing: {spacing}");


        if (isFilled)
        {
            ArrangeFilledBox(marchers, numRows, numCols, spacing, center);
        }
        else
        {
            ArrangeHollowBox(marchers, numRows, numCols, spacing, center);
        }
    }

    // Arrange marchers in a filled grid pattern within the box
    private void ArrangeFilledBox(List<GameObject> marchers, int rows, int columns, float spacing, Vector3 center)
    {
        int marcherIndex = 0;

        // Calculate the offset to center the formation
        float totalWidth = (columns - 1) * spacing;
        float totalHeight = (rows - 1) * spacing;
        Vector3 startOffset = center - new Vector3(totalWidth / 2, 0, totalHeight / 2);

        Debug.Log($"Arranging filled box at center {center}, starting from {startOffset}, with {rows} rows and {columns} columns.");

        // Place marchers in a filled grid
        for (int row = 0; row < rows && marcherIndex < marchers.Count; row++)
        {
            for (int col = 0; col < columns && marcherIndex < marchers.Count; col++)
            {
                Vector3 position = startOffset + new Vector3(col * spacing, 0, row * spacing);
                marchers[marcherIndex++].transform.position = snapToGrid.GetSnappedPosition(position);

                Debug.Log($"Positioning Marcher {marcherIndex} at {position} (Row: {row}, Col: {col})");
            }
        }
    }

    // Arrange marchers in a hollow box pattern around the edges
    private void ArrangeHollowBox(List<GameObject> marchers, int rows, int columns, float spacing, Vector3 center)
    {
        int marcherIndex = 0;

        // Calculate the offset to center the formation
        float totalWidth = (columns - 1) * spacing;
        float totalHeight = (rows - 1) * spacing;
        Vector3 startOffset = center - new Vector3(totalWidth / 2, 0, totalHeight / 2);

        Debug.Log($"Arranging hollow box at center {center}, starting from {startOffset}, with {rows} rows and {columns} columns.");

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Only place marchers on the outer edges
                if (row == 0 || row == rows - 1 || col == 0 || col == columns - 1)
                {
                    if (marcherIndex >= marchers.Count) return;

                    Vector3 position = startOffset + new Vector3(col * spacing, 0, row * spacing);
                    position = snapToGrid.GetSnappedPosition(position);

                    Debug.Log($"Positioning Marcher {marcherIndex} at {position} (Row: {row}, Col: {col})");

                    marchers[marcherIndex++].transform.position = position;
                }
            }
        }
    }
}
