using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    public void CreateBoxFormation(List<GameObject> marchers, IntervalManager.IntervalType interval, bool isFilled, Vector3? centerOverride = null)
    {
        Vector3 center = centerOverride ?? fieldManager.GetFieldCenter();
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

    public List<Vector3> GetBoxPositionsPreviewRespectingShapeHybrid(List<GameObject> marchers, IntervalManager.IntervalType interval, Vector3 center)
    {
        int count = marchers.Count;
        float spacing = intervalManager.GetIntervalSpacing(interval);
        float tolerance = spacing * 0.6f;

        // Step 1: Bounding box (SPACE aspect)
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var m in marchers)
        {
            Vector3 pos = m.transform.position;
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minZ = Mathf.Min(minZ, pos.z);
            maxZ = Mathf.Max(maxZ, pos.z);
        }

        float width = Mathf.Max(1f, maxX - minX);   // lateral (X)
        float height = Mathf.Max(1f, maxZ - minZ);  // vertical (Z)
        float aspectRatioFromSpace = width / height;

        // Step 2: Cluster positions into distinct columns and rows (DENSITY aspect)
        List<float> uniqueCols = new List<float>();
        List<float> uniqueRows = new List<float>();

        foreach (var m in marchers)
        {
            float x = m.transform.position.x;
            float z = m.transform.position.z;

            if (!uniqueCols.Any(c => Mathf.Abs(c - x) < tolerance))
                uniqueCols.Add(x);

            if (!uniqueRows.Any(r => Mathf.Abs(r - z) < tolerance))
                uniqueRows.Add(z);
        }

        int colCount = uniqueCols.Count;
        int rowCount = uniqueRows.Count;
        float aspectRatioFromDensity = (float)colCount / Mathf.Max(1, rowCount);

        // Step 3: Combine the two for a more intelligent shape estimation
        float hybridAspectRatio = (aspectRatioFromSpace + aspectRatioFromDensity) / 2f;

        // Step 4: Compute columns/rows from hybrid ratio
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count * hybridAspectRatio));
        int rows = Mathf.CeilToInt((float)count / columns);

        while (columns * rows < count)
        {
            if (columns <= rows) columns++;
            else rows++;
        }

        // Debug logs
        Debug.Log($"📐 SPACE Aspect: {aspectRatioFromSpace:F2}, DENSITY Aspect: {aspectRatioFromDensity:F2}, Hybrid: {hybridAspectRatio:F2}");
        Debug.Log($"🧮 Grid Target: {columns} columns x {rows} rows → {columns * rows} slots for {count} marchers");

        // Step 5: Generate snapped positions
        List<Vector3> positions = new List<Vector3>();
        float totalWidth = (columns - 1) * spacing;
        float totalHeight = (rows - 1) * spacing;
        Vector3 startOffset = center - new Vector3(totalWidth / 2, 0, totalHeight / 2);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (positions.Count >= count)
                    break;

                Vector3 pos = startOffset + new Vector3(col * spacing, 0, row * spacing);
                positions.Add(snapToGrid.GetSnappedPosition(pos));
            }
        }

        return positions;
    }



}
