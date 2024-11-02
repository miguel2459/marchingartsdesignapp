using UnityEngine;
using System.Collections.Generic;

public class SetSphereMovement : MonoBehaviour
{
    public SphereSelector setSphereSelector; // Reference to the SetSphereSelector script
    public GameObject transformGizmoPrefab; // Prefab for the transform gizmo
    public LayerMask groundLayer; // Layer for the ground to detect movement plane

    public GameObject transformGizmo;
    private bool isMoving = false;
    private Camera cam;
    private Vector3 offset;
    private Plane movePlane; // Plane on which the set spheres will move

    void Start()
    {
        cam = Camera.main;
        setSphereSelector = GetComponent<SphereSelector>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && setSphereSelector.selectedSpheres.Count > 0 && transformGizmo == null)
        {
            Debug.Log("SetSphereMovement: W key pressed - Showing transform gizmo.");
            ShowTransformGizmo();
        }

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt) && setSphereSelector.selectedSpheres.Count > 0 && transformGizmo != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Plane movementPlane = new Plane(Vector3.up, hit.point);
                float distance;
                if (movementPlane.Raycast(ray, out distance))
                {
                    Vector3 pointOnPlane = ray.GetPoint(distance);

                    if (hit.collider.gameObject.name == "X Axis" && !isMoving) // Adjust names based on your actual object names
                    {
                        Debug.Log("SetSphereMovement: X axis handle selected - Starting movement.");
                        isMoving = true;
                        movePlane = new Plane(Vector3.up, hit.point);
                        offset = hit.point - transformGizmo.transform.position;

                        // Lock movement to X axis
                        transformGizmo.transform.position = new Vector3(transformGizmo.transform.position.x, transformGizmo.transform.position.y, pointOnPlane.z);
                        return;
                    }
                    else if (hit.collider.gameObject.name == "Z Axis" && !isMoving) // Adjust names based on your actual object names
                    {
                        Debug.Log("SetSphereMovement: Z axis handle selected - Starting movement.");
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
                setSphereSelector.ClearSphereSelection();
                HideTransformGizmo();
            }
        }

        if (isMoving)
        {
            Debug.Log("SetSphereMovement: Moving set sphere(s) with gizmo.");
            MoveSetSpheres();
        }

        if (Input.GetMouseButtonUp(0) && isMoving)
        {
            Debug.Log("SetSphereMovement: Stopping movement.");
            isMoving = false;
        }
    }

    void ShowTransformGizmo()
    {
        Vector3 centerPoint = Vector3.zero;
        foreach (var sphere in setSphereSelector.selectedSpheres)
        {
            centerPoint += sphere.transform.position;
        }
        centerPoint /= setSphereSelector.selectedSpheres.Count;

        // Instantiate the gizmo at the calculated center point
        transformGizmo = Instantiate(transformGizmoPrefab, centerPoint, Quaternion.identity, transform);
        Debug.Log("SetSphereMovement: Transform gizmo instantiated at center point of selected set spheres.");

        // Adjust the gizmo rotation based on camera position relative to the scene
        AdjustGizmoRotation();

        // Parent each selected set sphere to the gizmo once when the gizmo is shown
        foreach (var sphere in setSphereSelector.selectedSpheres)
        {
            sphere.transform.SetParent(transformGizmo.transform);
        }
    }

    void AdjustGizmoRotation()
    {
        float cameraZPosition = cam.transform.position.z;

        // Assuming the front of the scene has higher Z values and the back has lower Z values.
        if (cameraZPosition > 0)
        {
            transformGizmo.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("SetSphereMovement: Gizmo rotation set to (0, 0, 0) for back view.");
        }
        else
        {
            transformGizmo.transform.rotation = Quaternion.Euler(0, 180, 0);
            Debug.Log("SetSphereMovement: Gizmo rotation set to (0, 180, 0) for front view.");
        }
    }

    void MoveSetSpheres()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float distance;

        if (movePlane.Raycast(ray, out distance))
        {
            Vector3 pointOnPlane = ray.GetPoint(distance) - offset;

            // Constrain movement based on the selected axis handle
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (IsGizmoXAxisHandleDragged())
                {
                    // Constrain movement to the X axis only
                    pointOnPlane = new Vector3(pointOnPlane.x, transformGizmo.transform.position.y, transformGizmo.transform.position.z);
                    Debug.Log("SetSphereMovement: Movement constrained to the X axis.");
                }
                else if (IsGizmoZAxisHandleDragged())
                {
                    // Constrain movement to the Z axis only
                    pointOnPlane = new Vector3(transformGizmo.transform.position.x, transformGizmo.transform.position.y, pointOnPlane.z);
                    Debug.Log("SetSphereMovement: Movement constrained to the Z axis.");
                }
            }
            else
            {
                // Restrict movement to the XZ plane
                pointOnPlane.y = transformGizmo.transform.position.y;
            }

            // Move the gizmo directly to the calculated position on the plane
            transformGizmo.transform.position = pointOnPlane;
            UpdateSetSpheresPositions(); // Update the positions in MarcherPositionsManager
            Debug.Log($"SetSphereMovement: Gizmo moved to new position {transformGizmo.transform.position}.");
        }
    }

    void UpdateSetSpheresPositions()
    {
        foreach (var sphere in setSphereSelector.selectedSpheres)
        {
            MarcherPositionsManager marcherPositionsManager = sphere.GetComponentInParent<MarcherPositionsManager>();
            if (marcherPositionsManager != null)
            {
                int index = System.Array.IndexOf(marcherPositionsManager.setSpheres, sphere);
                if (index != -1)
                {
                    marcherPositionsManager.positions[index] = sphere.transform.position; // Update the position
                    Debug.Log($"SetSphereMovement: Updated position of sphere at index {index} in MarcherPositionsManager.");
                }
            }
        }
    }

    bool IsGizmoXAxisHandleDragged()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.name == "X Axis")
            {
                Debug.Log("SetSphereMovement: X axis handle selected.");
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
            if (hit.collider.gameObject.name == "Z Axis")
            {
                Debug.Log("SetSphereMovement: Z axis handle selected.");
                return true;
            }
        }
        return false;
    }

    public void HideTransformGizmo()
    {
        Destroy(transformGizmo);
        transformGizmo = null;
    }
}
