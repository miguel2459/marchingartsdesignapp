using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;


/// <summary>
/// Handles user selection of marchers and confirms set positions when spacebar is pressed.
/// </summary>
public class SelectedMarchers : MonoBehaviour
{
    [Header("Selection Settings")]
    public Color normalColor = Color.white;
    public LayerMask marcherLayer;
    public EnsembleDirector2 director;
    public ICameraFocusHandler cameraFocusHandler;
    public Camera cam;
    public TransformGizmoManager transformGizmoManager;
    public List<GameObject> selectedMarchers = new List<GameObject>();
    public ShapeMarchers shapeMarchers;  // assign in Inspector
    public CountsProgressBar countsProgressBar;
    public MarcherPositionService marcherPositionService;
    public DashedPathPreviewManager dashedPathPreviewManager; // assign in inspector
    public MarcherPositionHistory positionHistory;

    private void Update()
    {
        CheckForSpaceBarSetPosition(); // march
        CheckForDeleteConfirmedPosition(); // 🔥 delete confirmed count

        if (Input.GetKeyDown(KeyCode.G)) SnapToGridOnly();
        if (Input.GetKeyDown(KeyCode.B)) SnapAndRespaceSmartReviewed();
        if (Input.GetKeyDown(KeyCode.A)) SelectAllMarchers();

        dashedPathPreviewManager?.UpdatePreviewCycle();

        if (Input.GetKeyDown(KeyCode.F) && selectedMarchers.Count > 0)
        {
            UpdateCameraFocus();
        }
    }

    public void SelectAllMarchers()
    {
        GameObject[] allMarchers = GameObject.FindGameObjectsWithTag("Marcher");

        if (allMarchers.Length == 0)
        {
            Debug.LogWarning("SelectedMarchers: No marchers found with tag 'Marcher'");
            return;
        }

        ClearSelection(); // Optional: Clear any previous selection

        foreach (GameObject marcher in allMarchers)
        {
            SelectMarcher(marcher);
        }
        
        dashedPathPreviewManager?.RegisterSelectedMarchers(selectedMarchers);

        Debug.Log($"SelectedMarchers: 🔢 Selected all {allMarchers.Length} marchers.");
    }

    public void SnapAndRespaceSmartReviewed()
    {
        if (selectedMarchers.Count == 0 || shapeMarchers?.boxManager == null)
            return;

        Vector3 center = GetFocalPoint();
        var interval = shapeMarchers.intervalManager.EstimateIntervalType(selectedMarchers);
        var targetPositions = shapeMarchers.boxManager.GetBoxPositionsPreviewRespectingShapeHybrid(selectedMarchers, interval, center);
        
        var assignments = new Dictionary<GameObject, Vector3>();
        var marchersLeft = new List<GameObject>(selectedMarchers);
        var positionsLeft = new List<Vector3>(targetPositions);

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

        // 🔍 Check for path crossings
        var crossings = DetectPathCrossings(assignments);
        if (crossings.Count > 0)
        {
            Debug.LogWarning("⚠️ Path crossing detected between:");
            foreach (var pair in crossings)
                Debug.Log($"    - {pair.Item1.name} ↔ {pair.Item2.name}");

            // [TODO] Optionally implement swap attempts here to resolve crossings
        }

        // 🧭 Apply results
        foreach (var kvp in assignments)
        {
            GameObject marcher = kvp.Key;
            Vector3 target = kvp.Value;

            Vector3 snapped = transformGizmoManager.snapToGrid.GetSnappedGizmoPosition(target);
            snapped.y = marcher.transform.position.y;
            marcher.transform.position = snapped;
        }

        if (transformGizmoManager.HasActiveGizmo && selectedMarchers.Count > 0)
            transformGizmoManager.ReanchorGizmoToMarcher(selectedMarchers[0]);

        Debug.Log($"SelectedMarchers: 🧠 Path-reviewed smart snap complete. Crossings: {crossings.Count}");
    }

    private List<(GameObject, GameObject)> DetectPathCrossings(Dictionary<GameObject, Vector3> assignments)
    {
        List<(GameObject, GameObject)> crossings = new List<(GameObject, GameObject)>();

        var pairs = assignments.ToList();
        for (int i = 0; i < pairs.Count; i++)
        {
            var aStart = pairs[i].Key.transform.position;
            var aEnd = pairs[i].Value;

            for (int j = i + 1; j < pairs.Count; j++)
            {
                var bStart = pairs[j].Key.transform.position;
                var bEnd = pairs[j].Value;

                if (SegmentsCross2D(aStart, aEnd, bStart, bEnd))
                    crossings.Add((pairs[i].Key, pairs[j].Key));
            }
        }

        return crossings;
    }

    private bool SegmentsCross2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector2 A1 = new Vector2(a1.x, a1.z);
        Vector2 A2 = new Vector2(a2.x, a2.z);
        Vector2 B1 = new Vector2(b1.x, b1.z);
        Vector2 B2 = new Vector2(b2.x, b2.z);

        return DoLinesIntersect(A1, A2, B1, B2);
    }

    // Standard 2D line segment intersection
    private bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float o1 = Orientation(p1, p2, q1);
        float o2 = Orientation(p1, p2, q2);
        float o3 = Orientation(q1, q2, p1);
        float o4 = Orientation(q1, q2, p2);

        return o1 != o2 && o3 != o4;
    }

    private float Orientation(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
    }

    public void SnapToGridOnly()
    {
        if (selectedMarchers.Count == 0 || transformGizmoManager?.snapToGrid == null)
            return;

        foreach (GameObject marcher in selectedMarchers)
        {
            Vector3 currentPos = marcher.transform.position;
            Vector3 snapped = transformGizmoManager.snapToGrid.GetSnappedGizmoPosition(currentPos);

            snapped.x = Mathf.Clamp(snapped.x, transformGizmoManager.snapToGrid.currentFieldMin.x, transformGizmoManager.snapToGrid.currentFieldMax.x);
            snapped.z = Mathf.Clamp(snapped.z, transformGizmoManager.snapToGrid.currentFieldMin.y, transformGizmoManager.snapToGrid.currentFieldMax.y);
            snapped.y = currentPos.y;

            marcher.transform.position = snapped;
        }

        Debug.Log($"SelectedMarchers: 🧲 Snapped {selectedMarchers.Count} marchers to nearest grid points.");
    }

    public void CheckForSpaceBarSetPosition()
    {
        if (Input.GetKeyDown(KeyCode.Space) && selectedMarchers.Count > 0)
        {
            int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
            int setToUse;
            int countToUse;

            // Determine if a count is highlighted in CountsProgressBar
            int activeCountIndex = countsProgressBar != null ? countsProgressBar.GetActiveCountIndex() : -1;

            if (activeCountIndex >= 0)
            {
                // 🧠 A specific count is selected
                setToUse = currentSet;
                countToUse = activeCountIndex + 1; // Convert to 1-based
                Debug.Log($"SelectedMarchers: ⏺️ Setting positions at Set {setToUse}, Count {countToUse}");
            }
            else
            {
                // 🧠 No count highlighted — use last count of previous set
                if (currentSet == 1)
                {
                    setToUse = 0;
                    countToUse = 0;
                    Debug.Log($"SelectedMarchers: ⏺️ Fallback to Set 0, Count 0");
                }
                else
                {
                    setToUse = currentSet - 1;
                    countToUse = SessionManager.instance.runtimeCacheSO.SetTimingMap[setToUse].count;
                    Debug.Log($"SelectedMarchers: ⏺️ Fallback to Set {setToUse}, Last Count {countToUse}");
                }
            }
            positionHistory.BeginBatch();
            foreach (GameObject marcher in selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    marcherPositionService.ConfirmMarcherPosition(posManager, setToUse, countToUse, marcher.transform.position);
                    bool isHolding = posManager.IsHoldingAtCount(setToUse, countToUse);
                    Unit unit = posManager.GetComponent<Unit>();
                    unit.SetSelector(true, isHolding);
                }
            }
            positionHistory.EndBatch();

            countsProgressBar?.UpdateCountSubtextsForSet(setToUse);
            director.UpdateInspectorSetProgress();
            director.VisualizePathsForSet(setToUse);
            ReCacheAnchorsForSelected();              // update anchor context
            dashedPathPreviewManager?.DisableAllPreviews();
        }
    }

    public void SelectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
            return;

        selectedMarchers.Add(marcher);
        //initialPositions[marcher] = marcher.transform.position; // Keep track for drag start detection

        // 1. Set visual state (Highlight color, selector visibility)
        if (marcher.TryGetComponent(out MarcherVisualStateController visual))
            visual.SetSelected(true); // Handles material color too

        // 2. Cache anchors for dashed preview (logic remains the same)
        marcher.GetComponent<MarcherDashedPathCoordinator>()?.CacheAnchorsFromSceneContext();
        int currentSetIndex = int.Parse(SessionManager.instance.showStateSO.LastSet);
        director.VisualizePathsForSet(currentSetIndex);
        dashedPathPreviewManager?.RegisterSelectedMarchers(selectedMarchers);
      

        Debug.Log($"✅ [SelectedMarchers] {marcher.name} selected. Path visualization updated for set {currentSetIndex}. Anchors cached.");
    }

    public void DeselectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Remove(marcher);
            if (marcher.TryGetComponent(out MarcherVisualStateController visual))
            {
                visual.SetSelected(false); // Reset visual state
            }

            // ADDED: Refresh path visualization after deselecting
            int currentSetIndex = int.Parse(SessionManager.instance.showStateSO.LastSet);
            director.VisualizePathsForSet(currentSetIndex); // Refresh paths for current set

            // Re-apply progress color if needed (optional, VisualizePathsForSet might handle this implicitly if no selection)
            director.ColorMarchersForSet(currentSetIndex, new[] { marcher.GetComponent<MarcherPositionsManager>() });

            //Debug.Log($"SelectedMarchers: ❎ {marcher.name} deselected. Path visualization updated for set {currentSetIndex}.");
        }
    }

    public void ReanchorToExisting(GameObject marcher)
    {
        if (transformGizmoManager != null)
        {
            transformGizmoManager.ReanchorGizmoToMarcher(marcher);
        }
    }

    public void ClearSelection()
    {
        // Re‑apply progress‑state colors for *all* marchers
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        
        foreach (GameObject marcher in selectedMarchers.ToList())
        {
            if (marcher != null)
            {
                var renderer = marcher.GetComponent<Renderer>();
                var unit = marcher.GetComponent<Unit>();

                if (renderer != null) renderer.material.color = normalColor;

                if (transformGizmoManager != null && transformGizmoManager.HasActiveGizmo)
                {
                    marcher.transform.SetParent(director.transform);
                }
                // Re‑apply progress‑state visuals
                DeselectMarcher(marcher);
            }
        }
        selectedMarchers.Clear();
        transformGizmoManager?.HideTransformGizmo();
        transformGizmoManager.isMoving = false;

        RenderDefaultPathsForAll();
        dashedPathPreviewManager?.StopAllPreviews();

        Debug.Log("SelectedMarchers: 🧹 Selection cleared and marchers recolored to progress state.");
    }

    public void ReCacheAnchorsForSelected()
    {
        StartCoroutine(DelayedCacheAnchors());
    }

    private IEnumerator DelayedCacheAnchors()
    {
        yield return null; // Wait one frame to allow UI state to update

        if (selectedMarchers == null || selectedMarchers.Count == 0)
            yield break;

        foreach (var marcher in selectedMarchers)
        {
            if (marcher == null)
                continue;

            marcher.GetComponent<MarcherDashedPathCoordinator>()?.CacheAnchorsFromSceneContext();
            dashedPathPreviewManager?.DisableAllPreviews();
        }
    }

    private void RenderDefaultPathsForAll()
    {
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

        if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(currentSet, out var timing)) return;

        int totalCounts = timing.count;
        int fallbackSet = (currentSet == 1) ? 0 : currentSet - 1;
        int fallbackCount = 0;

        if (currentSet > 1 &&
            SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var prevTiming))
        {
            fallbackCount = prevTiming.count;
        }

        foreach (var marcher in director.Marchers)
        {
            Vector3 start = marcher.GetPositionAtCount(fallbackSet, fallbackCount);
            Vector3[] path = marcher.GetInterpolatedPath(currentSet, totalCounts, start);
            marcher.ShowPath(path);

            // Default white-blueish color
            marcher.pathVisualizer?.SetColor(new Color(1f, 1f, 1f, 0.8f));
        }
    }

    private void CheckForDeleteConfirmedPosition()
    {
        if (Input.GetKeyDown(KeyCode.Delete) && selectedMarchers.Count > 0)
        {
            int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
            int countIndex = countsProgressBar != null ? countsProgressBar.GetActiveCountIndex() : -1;

            if (countIndex < 0)
            {
                Debug.LogWarning("❌ No count is currently selected. Cannot delete.");
                return;
            }

            int countToDelete = countIndex + 1; // because activeCountIndex is 0-based
            bool anyDeleted = false;
            bool anyHadNext = false;

            foreach (GameObject marcher in selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    if (marcherPositionService.DeleteConfirmedPosition(posManager, currentSet, countToDelete, out bool hadNext))
                    {
                        anyDeleted = true;
                        if (hadNext) anyHadNext = true;
                    }
                }
            }

            if (!anyDeleted)
            {
                Debug.Log("❌ No marcher confirmed positions were deleted.");
                return;
            }

            // ✅ Only runs if at least one marcher had a confirmed position deleted
            director.UpdateInspectorSetProgress();
            director.VisualizePathsForSet(currentSet);
            ReCacheAnchorsForSelected();
            
            int updatedCountTotal = director.GetCountTotalForSet(currentSet); // <- make sure this exists
            
            countsProgressBar?.RenderCounts(currentSet, updatedCountTotal);
            countsProgressBar?.UpdateCountSubtextsForSet(currentSet);            
            dashedPathPreviewManager?.DisableAllPreviews();

            if (anyHadNext)
                countsProgressBar.OnCountButtonClicked(currentSet, countToDelete);
        }
    }

    public void UpdateCameraFocus()
    {
        if (selectedMarchers == null || selectedMarchers.Count == 0) return;

        Vector3 focalPoint = GetFocalPoint();
        cameraFocusHandler?.SetSelectedMarchers(selectedMarchers);
        cameraFocusHandler?.FocusOnSelection(focalPoint);
        Debug.Log("SelectedMarchers.cs calling to set marchers and focusonSelection");
    }

    public Vector3 GetFocalPoint()
    {
        if (selectedMarchers == null || selectedMarchers.Count == 0)
            return Vector3.zero;

        Vector3 total = Vector3.zero;
        foreach (var m in selectedMarchers)
            total += m.transform.position;
        
        Vector3 focal = total / selectedMarchers.Count;
        Debug.Log("Getting Focal Point for camera focus" + focal);

        return focal;
    }

    public void SetActiveCamera(Camera activeCam)
    {
        cam = activeCam;
        transformGizmoManager.SetActiveCamera(activeCam);

        bool isTopDown = (activeCam.orthographic == true);

        foreach (var marcher in director.Marchers)
        {
            var billboard = marcher.GetComponentInChildren<MarcherLabelBillboard>();
            if (billboard != null)
            {
                billboard.SetCamera(activeCam);
                billboard.SetMode(isTopDown);
            }
        }
    }
}