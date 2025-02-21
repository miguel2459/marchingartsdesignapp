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
    private Camera cam;
    private Vector3 offset;
    private Plane movePlane; // Plane on which the marchers will move

    private const float RaycastDistance = 1000f; // Define a large raycast distance

    public LayerMask gizmoLayer; // Layer for the gizmo
    public LayerMask marcherLayer; // Layer for the marchers

    void Start()
    {
        cam = Camera.main;
        marcherSelector = GetComponent<SelectedMarchers>();
        snapToGrid = FindObjectOfType<SnapToGridLines>(); // Assuming SnapToGridWithLines is on the same GameObject
    }

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

                Plane movementPlane = new Plane(Vector3.up, hit.point);
                float distance;
                if (movementPlane.Raycast(ray, out distance))
                {
                    Vector3 pointOnPlane = ray.GetPoint(distance);

                    if (hit.collider.gameObject.name == "X Axis" && !isMoving) // Adjust names based on your actual object names
                    {
                        Debug.Log("MarcherMovement: X axis handle selected - Starting movement.");
                        isMoving = true;
                        movePlane = new Plane(Vector3.up, hit.point);
                        offset = hit.point - transformGizmo.transform.position;

                        // Lock movement to X axis
                        transformGizmo.transform.position = new Vector3(transformGizmo.transform.position.x, transformGizmo.transform.position.y, pointOnPlane.z);
                        return;
                    }
                    else if (hit.collider.gameObject.name == "Z Axis" && !isMoving) // Adjust names based on your actual object names
                    {
                        Debug.Log("MarcherMovement: Z axis handle selected - Starting movement.");
                        isMoving = true;
                        movePlane = new Plane(Vector3.up, hit.point);
                        offset = hit.point - transformGizmo.transform.position;

                        // Lock movement to Z axis
                        transformGizmo.transform.position = new Vector3(pointOnPlane.x, transformGizmo.transform.position.y, transformGizmo.transform.position.z);
                        return;
                    }
                }
            }
            else if (!Input.GetKey(KeyCode.LeftAlt) && transformGizmo != null) // Prevent clearing selection if Alt is pressed
            {
                marcherSelector.ClearSelection();
                isMoving = false; // Reset isMoving when selection is cleared
                HideTransformGizmo();
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

            // Snap to grid when releasing the mouse button
            transformGizmo.transform.position = snapToGrid.GetSnappedGizmoPosition(transformGizmo.transform.position);
        }
    }

    void ShowTransformGizmo()
    {
        if (marcherSelector.selectedMarchers.Count > 0)
        {
            foreach (var marcher in marcherSelector.selectedMarchers)
            {
                marcherPositionsManager = marcher.GetComponent<MarcherPositionsManager>();

                if (marcherPositionsManager != null && AreAllSetSpheresFilled(marcherPositionsManager))
                {
                    Debug.Log($"{marcher.name}: All setSpheres are filled. Cannot move marcher.");
                    return;
                }
            }
        }

        Vector3 centerPoint = Vector3.zero;
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            centerPoint += marcher.transform.position;
        }
        centerPoint /= marcherSelector.selectedMarchers.Count;

        // Instantiate and snap the gizmo to the nearest grid point
        transformGizmo = Instantiate(transformGizmoPrefab, snapToGrid.GetSnappedGizmoPosition(centerPoint), Quaternion.identity, transform);
        Debug.Log("MarcherMovement: Transform gizmo instantiated at center point of selected marchers." + centerPoint + ", " + snapToGrid.GetSnappedPosition(centerPoint));

        AdjustGizmoRotation();

        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            marcher.transform.SetParent(transformGizmo.transform);
            //marcher.transform.localPosition = Vector3.zero;
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


    bool AreAllSetSpheresFilled(MarcherPositionsManager marcherPositionsManager)
    {
        foreach (var sphere in marcherPositionsManager.setSpheres)
        {
            if (sphere == null)
            {
                return false;
            }
        }
        return true;
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

        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            marcherPositionsManager = marcher.GetComponent<MarcherPositionsManager>();

            if (marcherPositionsManager != null && AreAllSetSpheresFilled(marcherPositionsManager))
            {
                Debug.Log($"{marcher.name}: All setSpheres are filled. Cannot move marcher.");
                return;
            }
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

            // Clamp movement BEFORE snapping to grid
            pointOnPlane.x = Mathf.Clamp(pointOnPlane.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
            pointOnPlane.z = Mathf.Clamp(pointOnPlane.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

            // Move and snap the gizmo to the nearest grid point
            transformGizmo.transform.position = snapToGrid.GetSnappedGizmoPosition(pointOnPlane);
            Debug.Log($"MarcherMovement: Gizmo moved to new position {transformGizmo.transform.position}.");
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
