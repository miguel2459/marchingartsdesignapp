using UnityEngine;
using System.Collections.Generic;

public class TransformGizmoManager : MonoBehaviour
{
    public GameObject unifiedGizmoPrefab;
    public Camera cam;
    public SelectedMarchers selectedMarchers;
    public SnapToGridLines snapToGrid;
    public MarcherPositionHistory history;
    public LayerMask gizmoLayer;
    public LayerMask marcherLayer;
    private GameObject activeGizmo;
    private string currentMode = "position"; // Modes: position, rotate, scale
    public bool isMoving = false;
    private bool isFreeDraggingGizmo = false;
    private Plane movePlane;
    private Vector3 offset;
    public bool HasActiveGizmo => activeGizmo != null;
    public GameObject transformGizmo => activeGizmo;
    public bool IsFreeDraggingGizmo => isFreeDraggingGizmo;


    public bool IsGizmoActive()
    {
        return activeGizmo != null;
    }

    void Update()
    {
        if (selectedMarchers.SelectedCount == 0)
        {
            if (activeGizmo)
            {
                Destroy(activeGizmo);
                activeGizmo = null;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.W)) SetMode("position");
        if (Input.GetKeyDown(KeyCode.R)) SetMode("rotate");
        if (Input.GetKeyDown(KeyCode.E)) SetMode("scale");

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (activeGizmo != null && Physics.Raycast(ray, out hit, 1000f, gizmoLayer))
            {
                float distance;

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    Debug.Log("TransformGizmoManager: Shift+Clicked gizmo - initiating freeform drag.");
                    isFreeDraggingGizmo = true;

                    movePlane = new Plane(Vector3.up, hit.point);

                    if (movePlane.Raycast(ray, out distance))
                    {
                        offset = ray.GetPoint(distance) - activeGizmo.transform.position;
                    }

                    selectedMarchers.ForEachSelected(m => m.transform.SetParent(null));
                    return;
                }
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    Debug.Log("TransformGizmoManager: Regular gizmo click - initiating standard drag.");
                    isMoving = true;
                }
            }
        }

        if (isMoving && activeGizmo != null && selectedMarchers.SelectedCount > 0)
        {
            MoveGizmo();
        }

        if (Input.GetMouseButtonUp(0) && isMoving)
        {
            isMoving = false;
            if (isFreeDraggingGizmo)
            {
                isFreeDraggingGizmo = false;
                selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
            }
        }
    }

    public void SetMode(string mode)
    {
        // Toggle off if current mode is active
        if (HasActiveGizmo && currentMode == mode)
        {
            HideTransformGizmo();
            currentMode = null;
            return;
        }

        currentMode = mode;

        // === If gizmo already exists, just switch mode without moving ===
        if (activeGizmo)
        {
            var behavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
            if (behavior)
            {
                behavior.SetMode(currentMode);
            }
            return;
        }

        // === If no gizmo exists, create it at center of selected marchers ===
        Vector3 center = Vector3.zero;
        int count = 0;
        selectedMarchers.ForEachSelected(m =>
        {
            center += m.transform.position;
            count++;
        });
        center /= Mathf.Max(1, count); // Avoid divide by zero


        activeGizmo = Instantiate(unifiedGizmoPrefab, center, Quaternion.identity, transform);
        var newBehavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
        if (newBehavior)
        {
            newBehavior.cam = cam;
            newBehavior.selectedMarchers = selectedMarchers;
            newBehavior.snapToGrid = snapToGrid;
            newBehavior.SetMode(currentMode);
            newBehavior.gizmoManager = this;
            newBehavior.positionHistory = history;
        }

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
        selectedMarchers.ReCacheAnchorsForSelected(); // ensures anchor state is up-to-date
    }

    public void ReanchorGizmoToMarcher(GameObject marcher)
    {
        if (!activeGizmo || !marcher) return;

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(null));

        activeGizmo.transform.position = marcher.transform.position;
        Debug.Log($"TransformGizmoManager: Gizmo reanchored to {marcher.name} at {marcher.transform.position}");

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
    }

    public void HideTransformGizmo()
    {
        if (activeGizmo != null)
        {
            Transform ensembleParent = selectedMarchers.director.transform; // 👈 Get reference to EnsembleDirector2

            selectedMarchers.ForEachSelected(m =>
            {
                if (m != null) m.transform.SetParent(ensembleParent);
            });

            Destroy(activeGizmo);
            activeGizmo = null;
        }
    }

    void MoveGizmo()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float distance;

        if (movePlane.Raycast(ray, out distance))
        {
            Vector3 pointOnPlane = ray.GetPoint(distance) - offset;
            pointOnPlane.y = activeGizmo.transform.position.y;

            pointOnPlane.x = Mathf.Clamp(pointOnPlane.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
            pointOnPlane.z = Mathf.Clamp(pointOnPlane.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

            Vector3 finalPosition = Input.GetKey(KeyCode.Q)
                ? snapToGrid.GetSnappedGizmoPosition(pointOnPlane)
                : pointOnPlane;

            activeGizmo.transform.position = finalPosition;
        }
    }

    public void SetActiveCamera(Camera activeCam)
    {
        cam = activeCam;

        if (activeGizmo != null)
        {
            UnifiedGizmoBehavior behavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
            if (behavior != null)
            {
                behavior.cam = activeCam;
            }
        }
    }

}