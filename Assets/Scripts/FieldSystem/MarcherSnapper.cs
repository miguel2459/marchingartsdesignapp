using System.Collections.Generic;
using UnityEngine;

public static class MarcherSnapper
{
    /// <summary>
    /// Snaps a position to the nearest grid point, clamped to field boundaries.
    /// </summary>
    public static Vector3 SnapToGrid(Vector3 rawPosition, SnapToGridLines snapToGrid)
    {
        Vector3 snapped = snapToGrid.GetSnappedGizmoPosition(rawPosition);
        snapped.x = Mathf.Clamp(snapped.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
        snapped.z = Mathf.Clamp(snapped.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
        snapped.y = rawPosition.y;
        return snapped;
    }

    /// <summary>
    /// Applies snapping to all marchers in a list.
    /// </summary>

    public static void SnapSelectionToGrid(MarcherSelectionManager selectionManager, SnapToGridLines snapToGrid)
    {
        if (selectionManager == null || snapToGrid == null || selectionManager.SelectedMarchers.Count == 0)
            return;

        SnapAllToGrid(selectionManager.SelectedMarchers, snapToGrid);

        Debug.Log($"MarcherSnapper: 🧲 Snapped {selectionManager.SelectedMarchers.Count} marchers to grid.");
    }

    public static void SnapAllToGrid(IEnumerable<GameObject> marchers, SnapToGridLines snapToGrid)
    {
        foreach (var marcher in marchers)
        {
            if (marcher == null) continue;

            Vector3 snapped = SnapToGrid(marcher.transform.position, snapToGrid);
            marcher.transform.position = snapped;
        }
    }

}
