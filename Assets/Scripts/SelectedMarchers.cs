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
    public CameraControl cameraControl;
    public Camera cam;
    public TransformGizmoManager transformGizmoManager;
    public List<GameObject> selectedMarchers = new List<GameObject>();
    public ShapeMarchers shapeMarchers;  // assign in Inspector
    public CountsProgressBar countsProgressBar;
    public MarcherPositionService marcherPositionService;
    public DashedPathPreviewManager dashedPathPreviewManager; // assign in inspector

    private void Update()
    {
        CheckForSpaceBarSetPosition(); // march
        CheckForDeleteConfirmedPosition(); // 🔥 delete confirmed count

        if (Input.GetKeyDown(KeyCode.G)) SnapSelectedMarchersToGrid();
        if (Input.GetKeyDown(KeyCode.B)) ArrangeSelectedInBox();
        if (Input.GetKeyDown(KeyCode.A)) SelectAllMarchers();

        dashedPathPreviewManager?.UpdatePreviewCycle();
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
        
        UpdateCameraFocus();
        dashedPathPreviewManager?.RegisterSelectedMarchers(selectedMarchers);

        Debug.Log($"SelectedMarchers: 🔢 Selected all {allMarchers.Length} marchers.");
    }

    private void ArrangeSelectedInBox()
    {
        if (selectedMarchers.Count == 0 || shapeMarchers == null)
        {
            Debug.LogWarning("Box shape failed: No marchers selected or ShapeMarchers not assigned.");
            return;
        }

        IntervalManager.IntervalType estimatedInterval = shapeMarchers.intervalManager.EstimateIntervalType(selectedMarchers);
        ShapeGroup group = new ShapeGroup(ShapeMarchers.ShapeType.Box, estimatedInterval)
        {
            marchers = new List<GameObject>(selectedMarchers),
            isFilled = true // Change to false if you want a hollow box
        };

        Vector3 center = Vector3.zero;
        foreach (var m in selectedMarchers)
            center += m.transform.position;
        center /= selectedMarchers.Count;

        shapeMarchers.ArrangeFormation(group, center);

        // Optional: Recenter gizmo after box is created
        if (transformGizmoManager.HasActiveGizmo && selectedMarchers.Count > 0)
        {
            transformGizmoManager.ReanchorGizmoToMarcher(selectedMarchers[0]);
        }

        Debug.Log($"SelectedMarchers: 🧱 Box formation applied to {selectedMarchers.Count} marchers.");
    }

    private void SnapSelectedMarchersToGrid()
    {
        if (selectedMarchers.Count == 0 || transformGizmoManager == null || transformGizmoManager.snapToGrid == null)
            return;

        // Step 1: Snap current positions to nearest grid
        foreach (GameObject marcher in selectedMarchers)
        {
            Vector3 currentPos = marcher.transform.position;
            Vector3 snapped = transformGizmoManager.snapToGrid.GetSnappedGizmoPosition(currentPos);

            snapped.x = Mathf.Clamp(snapped.x, transformGizmoManager.snapToGrid.currentFieldMin.x, transformGizmoManager.snapToGrid.currentFieldMax.x);
            snapped.z = Mathf.Clamp(snapped.z, transformGizmoManager.snapToGrid.currentFieldMin.y, transformGizmoManager.snapToGrid.currentFieldMax.y);
            snapped.y = currentPos.y;

            marcher.transform.position = snapped;
        }

        Debug.Log($"SelectedMarchers: 🔲 Snapped {selectedMarchers.Count} marchers to grid.");

        // Step 2: Equalize spacing into box formation using detected interval
        IntervalManager.IntervalType interval = shapeMarchers.intervalManager.EstimateIntervalType(selectedMarchers);
        Vector3 center = Vector3.zero;

        foreach (var m in selectedMarchers)
            center += m.transform.position;

        center /= selectedMarchers.Count;

        ShapeGroup group = new ShapeGroup(ShapeMarchers.ShapeType.Box, interval)
        {
            marchers = new List<GameObject>(selectedMarchers),
            isFilled = true
        };

        shapeMarchers.ArrangeFormation(group, center);

        Debug.Log($"SelectedMarchers: 🧮 Equalized spacing into box using {interval} around center {center}");

        // Step 3: Reanchor gizmo (optional)
        if (transformGizmoManager.HasActiveGizmo && selectedMarchers.Count > 0)
        {
            transformGizmoManager.ReanchorGizmoToMarcher(selectedMarchers[0]);
        }
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
            countsProgressBar?.UpdateCountSubtextsForSet(setToUse);
            director.UpdateInspectorSetProgress();
            director.VisualizePathsForSet(setToUse);
            dashedPathPreviewManager?.StopAllPreviews();
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
        UpdateCameraFocus(); // Let CameraControl know about the selection change

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

             // Keep camera focus update
             UpdateCameraFocus();
             //dashedPathPreviewManager?.StopAllPreviews();

            Debug.Log($"SelectedMarchers: ❎ {marcher.name} deselected. Path visualization updated for set {currentSetIndex}.");
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
            int lastCount = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(currentSet, out var timing)
                ? timing.count : -1;

            if (countToDelete == lastCount)
            {
                Debug.LogWarning($"❌ Cannot delete confirmed position at final count {countToDelete} of Set {currentSet}.");
                return;
            }

            Debug.Log($"🗑 Attempting to delete confirmed position: Set {currentSet}, Count {countToDelete}");

            foreach (GameObject marcher in selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    marcherPositionService.DeleteConfirmedPosition(posManager, currentSet, countToDelete);
                }
            }

            countsProgressBar?.UpdateCountSubtextsForSet(currentSet);
            director.UpdateInspectorSetProgress();
            director.VisualizePathsForSet(currentSet);
            dashedPathPreviewManager?.StopAllPreviews();
        }
    }

    public void UpdateCameraFocus()
    {
        cameraControl?.SetSelectedMarchers(selectedMarchers);
    }
}