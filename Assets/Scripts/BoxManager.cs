using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    private SnapToGridLines snapToGrid;
    private IntervalManager intervalManager;
    private GameObject marcherPrefab;
    private GameObject positionSpherePrefab;

    // Initialize method to set up necessary references
    public void Initialize(GameObject marcherPrefab, GameObject positionSpherePrefab, float marcherSpacing)
    {
        this.marcherPrefab = marcherPrefab;
        this.positionSpherePrefab = positionSpherePrefab;

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

    // Method to create a box formation for marchers
    public void CreateBoxFormation(List<GameObject> marchers, IntervalManager.IntervalType interval, bool isFilled)
    {
        float count = marchers.Count;
        float spacing = intervalManager.GetIntervalSpacing(interval);

        Debug.Log($"Marcher Count: {count}, interval size: {spacing}");

        // Step 1: Calculate the approximate size of the box
        int gridSize = Mathf.FloorToInt(Mathf.Sqrt(count));  // 4x4 for 16 marchers

        // Step 2: Calculate the box width and height based on interval spacing
        int numRows = gridSize;
        int numCols = gridSize;

        // If the number of marchers isn't a perfect square, fill any remaining marchers
        if (numRows * numCols < count)
        {
            numRows += 1;  // Add one more column if there are remaining marchers
        }

        Debug.Log($"Creating Box Formation - Rows: {numRows}, Columns: {numCols}, Spacing: {spacing}, IsFilled: {isFilled}");

        if (isFilled)
        {
            ArrangeFilledBox(marchers, numRows, numCols, spacing);
        }
        else
        {
            ArrangeHollowBox(marchers, numRows, numCols, spacing);
        }
    }

    // Arrange marchers in a filled grid pattern within the box
    private void ArrangeFilledBox(List<GameObject> marchers, int rows, int columns, float spacing)
    {
        int marcherIndex = 0;   //This variable tracks which marcher in the list is currently being positioned.

        Debug.Log($"Arranging filled box with adjusted Rows: {rows}, Columns: {columns}");

        //This loop iterates over each row in the grid.
        for (int row = 0; row < rows && marcherIndex < marchers.Count; row++)
        {
            //This loop iterates over each column in the current row.
            for (int col = 0; col < columns && marcherIndex < marchers.Count; col++)
            {
                // Calculate the position in the box pattern based on spacing and number of marchers
                Vector3 position = new Vector3(col * spacing, 0, row * spacing);

                // Snap the position to the grid if necessary, then set the marcher's position
                marchers[marcherIndex++].transform.position = snapToGrid.GetSnappedPosition(position);
                Debug.Log($"Positioning Marcher {marcherIndex} at {position} (Row: {row}, Col: {col})");
            }
        }

        // Account for remaining marchers that didn't fit in the perfect square
        while (marcherIndex < marchers.Count)
        {
            int additionalRow = marcherIndex / columns;
            int additionalCol = marcherIndex % columns;

            // Place remaining marchers in the next row
            Vector3 offset = new Vector3(additionalCol * spacing, 0, additionalRow * spacing);

            Vector3 position = new Vector3(offset.x, 0, offset.z);
            marchers[marcherIndex].transform.position = snapToGrid.GetSnappedPosition(position);

            Debug.Log($"Positioning Remaining Marcher {marcherIndex} at {position} (Row: {additionalRow}, Col: {additionalCol})");

            marcherIndex++;
        }
    }

    // Arrange marchers in a hollow box pattern around the edges
    private void ArrangeHollowBox(List<GameObject> marchers, int rows, int columns, float spacing)
    {
        int marcherIndex = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Only place marchers on the outer edges of the box
                if ((row == 0 || row == rows - 1) || (col == 0 || col == columns - 1))
                {
                    if (marcherIndex >= marchers.Count) return;

                    Vector3 offset = new Vector3(col * spacing, 0, row * spacing);
                    Vector3 rotatedOffset = Quaternion.Euler(0, 0, 0) * offset;

                    Vector3 position = snapToGrid != null ? snapToGrid.GetSnappedPosition(rotatedOffset) : rotatedOffset;
                    Debug.Log($"Positioning Marcher {marcherIndex} at {position} (Row: {row}, Col: {col})");

                    marchers[marcherIndex++].transform.position = position;
                }
            }
        }
    }
}
