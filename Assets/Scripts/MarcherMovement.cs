using UnityEngine;
using System.Collections.Generic;

public class MarcherMovement : MonoBehaviour
{
    public SelectedMarchers marcherSelector; // Reference to the MarcherSelector script
    public GameObject transformGizmoPrefab; // Prefab for the transform gizmo
    public LayerMask groundLayer; // Layer for the ground to detect movement plane
    public MarcherPositionsManager marcherPositionsManager; // Reference to the MarcherPositionsManager script
    public SnapToGridLines snapToGrid; // Add reference to SnapToGridWithLines

    public GameObject transformGizmo;
    public bool isMoving = false;
    public Camera cam;
    private Vector3 offset;
    private Plane movePlane; // Plane on which the marchers will move

    private const float RaycastDistance = 1000f; // Define a large raycast distance

    public LayerMask gizmoLayer; // Layer for the gizmo
    public LayerMask marcherLayer; // Layer for the marchers
    private bool isFreeDraggingGizmo = false;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && marcherSelector.selectedMarchers.Count > 0)
        {
            if (transformGizmo == null)
            {
                Debug.Log("MarcherMovement: W key pressed - Showing transform gizmo.");
                ShowTransformGizmo();
            }
            else
            {
                Debug.Log("MarcherMovement: W key pressed - Hiding transform gizmo.");
                foreach (var marcher in marcherSelector.selectedMarchers)
                {
                    marcher.transform.SetParent(null);
                }
                HideTransformGizmo();
            }
        }

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt) && marcherSelector.selectedMarchers.Count > 0)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check for gizmo interaction first
            if (transformGizmo != null && Physics.Raycast(ray, out hit, RaycastDistance, gizmoLayer))
            {
                Debug.Log($"Raycast hit gizmo: {hit.collider.gameObject.name}");
                float distance = 0f;

                // 🟨 SHIFT + CLICK on gizmo body (not axis) → Freeform move mode
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    Debug.Log("MarcherMovement: Shift+Clicked gizmo - initiating freeform gizmo drag.");

                    isFreeDraggingGizmo = true;
                    isMoving = true;
                    movePlane = new Plane(Vector3.up, hit.point);

                    
                    if (movePlane.Raycast(ray, out distance))
                    {
                        offset = ray.GetPoint(distance) - transformGizmo.transform.position;
                    }

                    // Temporarily unparent all selected marchers
                    foreach (var marcher in marcherSelector.selectedMarchers)
                    {
                        marcher.transform.SetParent(null);
                    }

                    return;
                }

                // 🎯 X/Z Axis Handle Drag Logic
                Plane movementPlane = new Plane(Vector3.up, hit.point);
         
                if (movementPlane.Raycast(ray, out distance))
                {
                    Vector3 pointOnPlane = ray.GetPoint(distance);

                    if (hit.collider.gameObject.name == "X Axis" && !isMoving)
                    {
                        Debug.Log("MarcherMovement: X axis handle selected - Starting movement.");
                        isMoving = true;
                        movePlane = new Plane(Vector3.up, hit.point);
                        offset = hit.point - transformGizmo.transform.position;

                        // Lock Z only
                        transformGizmo.transform.position = new Vector3(
                            transformGizmo.transform.position.x,
                            transformGizmo.transform.position.y,
                            pointOnPlane.z
                        );
                        return;
                    }
                    else if (hit.collider.gameObject.name == "Z Axis" && !isMoving)
                    {
                        Debug.Log("MarcherMovement: Z axis handle selected - Starting movement.");
                        isMoving = true;
                        movePlane = new Plane(Vector3.up, hit.point);
                        offset = hit.point - transformGizmo.transform.position;

                        // Lock X only
                        transformGizmo.transform.position = new Vector3(
                            pointOnPlane.x,
                            transformGizmo.transform.position.y,
                            transformGizmo.transform.position.z
                        );
                        return;
                    }
                }
            }

            else if (!Input.GetKey(KeyCode.LeftAlt) && transformGizmo != null)
            {
                // Only clear if not clicking a selected marcher
                if (Physics.Raycast(ray, out hit, RaycastDistance, marcherLayer))
                {
                    GameObject clicked = hit.collider.gameObject;
                    if (!marcherSelector.selectedMarchers.Contains(clicked))
                    {
                        marcherSelector.ClearSelection();
                        isMoving = false;
                        HideTransformGizmo();
                    }
                    else
                    {
                        Debug.Log("MarcherMovement: Clicked on a selected marcher - keeping selection and gizmo.");
                    }
                }
                else
                {
                    marcherSelector.ClearSelection();
                    isMoving = false;
                    HideTransformGizmo();
                }
            }
        }

        if (isMoving && transformGizmo != null && marcherSelector.selectedMarchers.Count > 0)
        {
            Debug.Log("MarcherMovement: Moving marcher(s) with gizmo.");
            MoveMarchers();
        }
        else if (isMoving && (transformGizmo == null || marcherSelector.selectedMarchers.Count == 0))
        {
            // Reset isMoving if conditions are no longer met
            isMoving = false;
        }

        if (Input.GetMouseButtonUp(0) && isMoving)
        {
            Debug.Log("MarcherMovement: Stopping movement.");
            isMoving = false;

            if (isFreeDraggingGizmo)
            {
                Debug.Log("MarcherMovement: Finishing freeform gizmo move - Reparenting marchers.");
                isFreeDraggingGizmo = false;

                // 🔄 Reparent all marchers to the gizmo again
                foreach (var marcher in marcherSelector.selectedMarchers)
                {
                    marcher.transform.SetParent(transformGizmo.transform);
                }
            }

            // 🟡 Optional: Snap gizmo to grid ONLY if holding Q
            if (Input.GetKey(KeyCode.Q))
            {
                transformGizmo.transform.position = snapToGrid.GetSnappedGizmoPosition(transformGizmo.transform.position);
            }

            // 💾 Save standby positions for each selected marcher
            int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

            foreach (var marcher in marcherSelector.selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    Vector3 pos = marcher.transform.position;
                    posManager.SaveStandbyPosition(currentSet, pos);
                }
            }
        }

    }

    void ShowTransformGizmo()
    {
        if (marcherSelector.selectedMarchers.Count > 0)
        {
            foreach (var marcher in marcherSelector.selectedMarchers)
            {
                marcherPositionsManager = marcher.GetComponent<MarcherPositionsManager>();
            }
        }

        Vector3 centerPoint = Vector3.zero;
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            centerPoint += marcher.transform.position;
        }
        centerPoint /= marcherSelector.selectedMarchers.Count;

        // Instantiate and snap the gizmo to the nearest grid point
        transformGizmo = Instantiate(transformGizmoPrefab, centerPoint, Quaternion.identity, transform);
        Debug.Log("MarcherMovement: Transform gizmo instantiated at center point of selected marchers." + centerPoint + ", " + snapToGrid.GetSnappedPosition(centerPoint));

        AdjustGizmoRotation();

        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            marcher.transform.SetParent(transformGizmo.transform);
            //marcher.transform.localPosition = Vector3.zero;
        }
    }

    public void ReanchorGizmoToMarcher(GameObject marcher)
    {
        if (transformGizmo == null || marcher == null) return;

        // 🔹 Step 1: Temporarily unparent all selected marchers
        foreach (var selected in marcherSelector.selectedMarchers)
        {
            selected.transform.SetParent(null);
        }

        // 🔹 Step 2: Move the gizmo to the new anchor position
        transformGizmo.transform.position = marcher.transform.position;
        Debug.Log($"MarcherMovement: Gizmo reanchored to {marcher.name} at {marcher.transform.position}");

        // 🔹 Step 3: Reparent all selected marchers back to the gizmo
        foreach (var selected in marcherSelector.selectedMarchers)
        {
            selected.transform.SetParent(transformGizmo.transform);
        }
    }


    bool IsGizmoXAxisHandleDragged()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider != null && hit.collider.gameObject.name == "X Axis") // Ensure the object name matches the X axis handle name
            {
                Debug.Log("MarcherMovement: X axis handle selected.");
                return true;
            }
        }
        return false;
    }

    bool IsGizmoZAxisHandleDragged()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider != null && hit.collider.gameObject.name == "Z Axis") // Ensure the object name matches the Z axis handle name
            {
                Debug.Log("MarcherMovement: Z axis handle selected.");
                return true;
            }
        }
        return false;
    }


    bool AreAllSetPositionsConfirmed(MarcherPositionsManager marcherPositionsManager)
    {
        int totalSetCount = SessionManager.instance.showStateSO.NumberOfSets;
        return marcherPositionsManager.setPositions.Count >= totalSetCount;
    }


    void AdjustGizmoRotation()
    {
        float cameraZPosition = cam.transform.position.z;

        if (cameraZPosition > 0)
        {
            transformGizmo.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("MarcherMovement: Gizmo rotation set to (0, 0, 0) for back sideline.");
        }
        else
        {
            transformGizmo.transform.rotation = Quaternion.Euler(0, 180, 0);
            Debug.Log("MarcherMovement: Gizmo rotation set to (0, 180, 0) for front sideline.");
        }
    }

    void MoveMarchers()
    {
        if (transformGizmo == null)
        {
            isMoving = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float distance;

        if (movePlane.Raycast(ray, out distance))
        {
            Vector3 pointOnPlane = ray.GetPoint(distance) - offset;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (IsGizmoXAxisHandleDragged())
                {
                    pointOnPlane = new Vector3(pointOnPlane.x, transformGizmo.transform.position.y, transformGizmo.transform.position.z);
                    Debug.Log("MarcherMovement: Movement constrained to the X axis.");
                }
                else if (IsGizmoZAxisHandleDragged())
                {
                    pointOnPlane = new Vector3(transformGizmo.transform.position.x, transformGizmo.transform.position.y, pointOnPlane.z);
                    Debug.Log("MarcherMovement: Movement constrained to the Z axis.");
                }
            }
            else
            {
                pointOnPlane.y = transformGizmo.transform.position.y;
            }

            // Clamp movement BEFORE deciding to snap
            pointOnPlane.x = Mathf.Clamp(pointOnPlane.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
            pointOnPlane.z = Mathf.Clamp(pointOnPlane.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

            // 🟡 Snap to grid only if holding Q
            Vector3 finalPosition = Input.GetKey(KeyCode.Q)
                ? snapToGrid.GetSnappedGizmoPosition(pointOnPlane)
                : pointOnPlane;

            transformGizmo.transform.position = finalPosition;
            Debug.Log($"MarcherMovement: Gizmo moved to {finalPosition} {(Input.GetKey(KeyCode.Q) ? "[SNAPPED]" : "[FREEFORM]")}");
            }
    }

    public void MoveGizmoToSelectedMarchers()
    {
        Vector3 centerPoint = Vector3.zero;
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            centerPoint += marcher.transform.position;
        }
        centerPoint /= marcherSelector.selectedMarchers.Count;

        // Snap gizmo to the nearest grid point at the center point
        transformGizmo.transform.position = snapToGrid.GetSnappedGizmoPosition(centerPoint);
        marcherPositionsManager.transform.localPosition = Vector3.zero;
        Debug.Log("MarcherMovement: Gizmo moved to the new center point of selected marchers.");
    }

    public void MoveGizmoToFirstSelectedMarcher(GameObject firstSelectedMarcher)
    {
        if (firstSelectedMarcher != null)
        {
            // Snap gizmo to the nearest grid point at the first selected marcher's position
            transformGizmo.transform.position = snapToGrid.GetSnappedGizmoPosition(firstSelectedMarcher.transform.position);
            firstSelectedMarcher.transform.localPosition = Vector3.zero;
            Debug.Log($"MarcherMovement: Gizmo moved to the position of the first selected marcher: {firstSelectedMarcher.name}.");
        }
    }

    public void HideTransformGizmo()
    {
        Destroy(transformGizmo);
        transformGizmo = null;
    }
}
