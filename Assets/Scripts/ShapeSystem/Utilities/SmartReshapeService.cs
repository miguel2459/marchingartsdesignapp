using System.Collections.Generic;
using UnityEngine;

public static class SmartReshapeService
{
    /// <summary>
    /// Assigns selected marchers to box positions minimizing distance and checking for path crossings.
    /// </summary>
    public static Dictionary<GameObject, Vector3> GetSmartAssignments(
        List<GameObject> selection,
        List<Vector3> boxTargets)
    {
        var assignments = new Dictionary<GameObject, Vector3>();
        var marchersLeft = new List<GameObject>(selection);
        var positionsLeft = new List<Vector3>(boxTargets);

        while (marchersLeft.Count > 0 && positionsLeft.Count > 0)
        {
            float bestDist = float.MaxValue;
            GameObject bestMarcher = null;
            Vector3 bestTarget = Vector3.zero;

            foreach (var marcher in marchersLeft)
            {
                foreach (var pos in positionsLeft)
                {
                    float dist = Vector3.Distance(marcher.transform.position, pos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestMarcher = marcher;
                        bestTarget = pos;
                    }
                }
            }

            assignments[bestMarcher] = bestTarget;
            marchersLeft.Remove(bestMarcher);
            positionsLeft.Remove(bestTarget);
        }

        return assignments;
    }

    public static void ApplyBoxAssignment(
    List<GameObject> selection,
    ShapeMarchers shapeMarchers,
    TransformGizmoManager gizmoManager,
    SnapToGridLines snapToGrid,
    Vector3 center)
    {
        if (selection == null || selection.Count == 0 || shapeMarchers?.boxManager == null)
            return;

        var interval = shapeMarchers.intervalManager.EstimateIntervalType(selection);
        var targetPositions = shapeMarchers.boxManager.GetBoxPositionsPreviewRespectingShapeHybrid(selection, interval, center);

        var assignments = GetSmartAssignments(selection, targetPositions);
        var crossings = PathCrossingDetector.DetectCrossings(assignments);

        if (crossings.Count > 0)
        {
            Debug.LogWarning("⚠️ Path crossing detected between:");
            foreach (var pair in crossings)
                Debug.Log($"    - {pair.Item1.name} ↔ {pair.Item2.name}");
        }

        foreach (var kvp in assignments)
        {
            GameObject marcher = kvp.Key;
            Vector3 snapped = MarcherSnapper.SnapToGrid(kvp.Value, snapToGrid);
            marcher.transform.position = snapped;
        }

        if (gizmoManager.HasActiveGizmo && selection.Count > 0)
            gizmoManager.ReanchorGizmoToMarcher(selection[0]);

        Debug.Log($"SmartReshapeService: ✅ Path-reviewed smart snap complete. Crossings: {crossings.Count}");
    }

    public static Vector3 GetFocalPoint(IReadOnlyList<GameObject> marchers)
    {
        if (marchers == null || marchers.Count == 0)
            return Vector3.zero;

        Vector3 total = Vector3.zero;
        foreach (var m in marchers)
        {
            total += m.transform.position;
        }

        Vector3 focal = total / marchers.Count;
        Debug.Log("SmartReshapeService: 📍 Calculated focal point: " + focal);
        return focal;
    }
}
