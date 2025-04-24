using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;


/// <summary>
/// Handles user selection of marchers and confirms set positions when spacebar is pressed.
/// </summary>
public class SelectedMarchers : MonoBehaviour
{
    [Header("Selection Settings")]
    public Color highlightColor = Color.blue;
    public Color normalColor = Color.white;
    public LayerMask marcherLayer;

    [Header("References")]
    public EnsembleDirector2 director;
    public CameraControl cameraControl;
    public Camera cam;
    public TransformGizmoManager transformGizmoManager;

    public List<GameObject> selectedMarchers = new List<GameObject>();
    public bool selectAllMarchers; // for inspector testing
    public ShapeMarchers shapeMarchers;  // assign in Inspector
    public CountsProgressBar countsProgressBar;
    public MarcherPositionService marcherPositionService;


    private void Update()
    {
        CheckForSpaceBarSetPosition(); // march
        CheckForDeleteConfirmedPosition(); // 🔥 delete confirmed count

        if (Input.GetKeyDown(KeyCode.G)) SnapSelectedMarchersToGrid();
        if (Input.GetKeyDown(KeyCode.B)) ArrangeSelectedInBox();
        if (Input.GetKeyDown(KeyCode.A)) SelectAllMarchers();
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

    /// <summary>
    /// If spacebar is pressed, confirms the transform.position as a SetPosition for each selected marcher.
    /// </summary>
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
                    marcher.GetComponent<MarcherVisualStateController>()?.SetSelectorVisible(true);
                }
            }
            countsProgressBar?.UpdateCountSubtextsForSet(setToUse);
            director.UpdateInspectorSetProgress();
            director.VisualizePathsForSet(setToUse);
        }
    }


    public void SelectMarcher(GameObject marcher)
    {
        if (!selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Add(marcher);

            var rend = marcher.GetComponent<Renderer>();
            if (rend != null) rend.material.color = highlightColor;

            // Hide all paths first
            foreach (var m in director.Marchers)
            {
                m.HidePath();
            }

            // Only show for selected with vibrant blue
            Color highlightLineColor = new Color(0f, 0.81f, 1f, 1f); // #00CFFF
            int set = int.Parse(SessionManager.instance.showStateSO.LastSet);

            if (SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(set, out var timing))
            {
                int totalCounts = timing.count;
                int fallbackSet = (set == 1) ? 0 : set - 1;
                int fallbackCount = 0;

                if (set > 1 &&
                    SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var prevTiming))
                {
                    fallbackCount = prevTiming.count;
                }

                foreach (var selected in selectedMarchers)
                {
                    if (selected.TryGetComponent(out MarcherPositionsManager pos))
                    {
                        Vector3 start = pos.GetPositionAtCount(fallbackSet, fallbackCount);
                        Vector3[] path = pos.GetInterpolatedPath(set, totalCounts, start);
                        pos.ShowPath(path);
                        pos.pathVisualizer?.SetColor(highlightLineColor);
                    }
                }
            }
            Debug.Log($"SelectedMarchers: ✅ {marcher.name} selected.");
        }
    }

    public void DeselectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
        {
            // 1) Remove from our list
            selectedMarchers.Remove(marcher);

            int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

            if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
            {
                director.ColorMarchersForSet(currentSet, new[] { posManager });
                posManager.HidePath();
            }

            Debug.Log($"SelectedMarchers: ❎ {marcher.name} deselected.");
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
            }
        }
        selectedMarchers.Clear();
        transformGizmoManager?.HideTransformGizmo();
        transformGizmoManager.isMoving = false;

        // Re‑apply progress‑state colors for *all* marchers
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

        // Reapply path lines for all marchers
        if (SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(currentSet, out var timing))
        {
            int totalCounts = timing.count;
            int fallbackSet = (currentSet == 1) ? 0 : currentSet - 1;
            int fallbackCount = 0;

            if (currentSet > 1 &&
                SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var prevTiming))
            {
                fallbackCount = prevTiming.count;
            }

            RenderDefaultPathsForAll();
        }

        // Re‑apply progress‑state visuals
        director.ColorMarchersForSet(currentSet);

        Debug.Log("SelectedMarchers: 🧹 Selection cleared and marchers recolored to progress state.");
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
        }
    }


    public void UpdateCameraFocus()
    {
        cameraControl?.SetSelectedMarchers(selectedMarchers);
    }
}