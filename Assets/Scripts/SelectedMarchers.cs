using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Handles user selection of marchers and confirms set positions when spacebar is pressed.
/// </summary>
public class SelectedMarchers : MonoBehaviour
{
    [Header("Selection Settings")]
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;
    public LayerMask marcherLayer;

    [Header("References")]
    public CameraControl cameraControl;
    public Camera cam;
    public TransformGizmoManager transformGizmoManager;

    public List<GameObject> selectedMarchers = new List<GameObject>();
    public bool selectAllMarchers; // for inspector testing
    public ShapeMarchers shapeMarchers;  // assign in Inspector


    private void Update()
    {
        CheckForSpaceBarSetPosition();

        if (Input.GetKeyDown(KeyCode.G))
        {
            SnapSelectedMarchersToGrid();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ArrangeSelectedInBox();
        }
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
        if (Input.GetKeyDown(KeyCode.Space) && transformGizmoManager.HasActiveGizmo && selectedMarchers.Count > 0)
        {
            int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

            Debug.Log($"SelectedMarchers: ⏺️ Setting SetPosition for {selectedMarchers.Count} marchers on Set {currentSet}");

            foreach (GameObject marcher in selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    posManager.SetPosition(currentSet, marcher.transform.position);
                }
            }
        }
    }

    public void SelectMarcher(GameObject marcher)
    {
        if (!selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Add(marcher);
            marcher.GetComponent<Renderer>().material.color = highlightColor;
            marcher.GetComponent<Unit>()?.SetSelector(true);

            if (transformGizmoManager.HasActiveGizmo)
            {
                marcher.transform.SetParent(transformGizmoManager.transformGizmo.transform);
            }

            Debug.Log($"SelectedMarchers: ✅ {marcher.name} selected.");
        }
    }

    public void DeselectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Remove(marcher);
            marcher.GetComponent<Renderer>().material.color = normalColor;
            marcher.GetComponent<Unit>()?.SetSelector(false);

            if (transformGizmoManager.HasActiveGizmo)
            {
                marcher.transform.SetParent(null);
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
                if (unit != null) unit.SetSelector(false);

                if (transformGizmoManager != null && transformGizmoManager.HasActiveGizmo)
                {
                    marcher.transform.SetParent(null);
                }
            }
        }

        selectedMarchers.Clear();
        transformGizmoManager?.HideTransformGizmo();
        transformGizmoManager.isMoving = false;

        Debug.Log("SelectedMarchers: 🧹 Selection cleared.");
    }

    public void UpdateCameraFocus()
    {
        cameraControl?.SetSelectedMarchers(selectedMarchers);
    }
}