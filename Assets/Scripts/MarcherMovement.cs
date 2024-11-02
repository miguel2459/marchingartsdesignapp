using UnityEngine;
using System.Collections.Generic;

public class MarcherMovement : MonoBehaviour
{
    public SelectedMarchers marcherSelector; // Reference to the MarcherSelector script
    public GameObject transformGizmoPrefab; // Prefab for the transform gizmo
    public LayerMask groundLayer; // Layer for the ground to detect movement plane
    public MarcherPositionsManager marcherPositionsManager; // Reference to the MarcherPositionsManager script

    public GameObject transformGizmo;
    private bool isMoving = false;
    private Camera cam;
    private Vector3 offset;
    private Plane movePlane; // Plane on which the marchers will move

    void Start()
    {
        cam = Camera.main;
        marcherSelector = FindObjectOfType<SelectedMarchers>();
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
                // Unparent marchers before destroying gizmo
                foreach (var marcher in marcherSelector.selectedMarchers)
                {
                    marcher.transform.SetParent(null);
                }
                HideTransformGizmo();
            }
        }

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt) && marcherSelector.selectedMarchers.Count > 0 && transformGizmo != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Calculate the movement plane and the point on the plane where the ray hit
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
                HideTransformGizmo();
            }
        }

        if (isMoving)
        {
            Debug.Log("MarcherMovement: Moving marcher(s) with gizmo.");
            MoveMarchers();
        }

        if (Input.GetMouseButtonUp(0) && isMoving)
        {
            Debug.Log("MarcherMovement: Stopping movement.");
            isMoving = false;
        }
    }

    void ShowTransformGizmo()
    {   
        // Ensure marchers are selected
        if (marcherSelector.selectedMarchers.Count > 0)
        {
            foreach (var marcher in marcherSelector.selectedMarchers)
            {
                marcherPositionsManager = marcher.GetComponent<MarcherPositionsManager>();

                // Check if all setSpheres are filled
                if (marcherPositionsManager != null && AreAllSetSpheresFilled(marcherPositionsManager))
                {
                    Debug.Log($"{marcher.name}: All setSpheres are filled. Cannot move marcher.");
                    return; // Prevent gizmo from being shown
                }
            }
        }

        Vector3 centerPoint = Vector3.zero;
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            centerPoint += marcher.transform.position;
        }
        centerPoint /= marcherSelector.selectedMarchers.Count;

        // Instantiate the gizmo at the calculated center point
        transformGizmo = Instantiate(transformGizmoPrefab, centerPoint, Quaternion.identity, transform);
        Debug.Log("MarcherMovement: Transform gizmo instantiated at center point of selected marchers.");

        // Adjust the gizmo rotation based on camera position relative to the football field
        AdjustGizmoRotation();

        // Parent each selected marcher to the gizmo once when the gizmo is shown
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            marcher.transform.SetParent(transformGizmo.transform);
        }
    }

    bool AreAllSetSpheresFilled(MarcherPositionsManager marcherPositionsManager)
    {
        foreach (var sphere in marcherPositionsManager.setSpheres)
        {
            if (sphere == null)
            {
                return false; // Found an empty spot, so not all are filled
            }
        }
        return true; // All are filled
    }

    void AdjustGizmoRotation()
    {
        // Assuming the football field runs along the X axis, we can determine the camera's position based on its Z coordinate.
        float cameraZPosition = cam.transform.position.z;

        // Here you might define the thresholds for the "front" and "back" of the field.
        // Assuming the front of the field has higher Z values and the back has lower Z values.
        if (cameraZPosition > 0) // Adjust this condition based on your field's orientation
        {
            // Camera is viewing from the back sideline
            transformGizmo.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("MarcherMovement: Gizmo rotation set to (0, 0, 0) for back sideline.");
        }
        else
        {
            // Camera is viewing from the front sideline
            transformGizmo.transform.rotation = Quaternion.Euler(0, 180, 0);
            Debug.Log("MarcherMovement: Gizmo rotation set to (0, 180, 0) for front sideline.");
        }
    }

    bool IsGizmoXAxisHandleDragged()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.name == "X Axis") // Assuming X axis mesh is named "XAxis"
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
            if (hit.collider.gameObject.name == "Z Axis") // Assuming Z axis mesh is named "ZAxis"
            {
                Debug.Log("MarcherMovement: Z axis handle selected.");
                return true;
            }
        }
        return false;
    }

    void MoveMarchers()
    {
        foreach (var marcher in marcherSelector.selectedMarchers)
        {
            marcherPositionsManager = marcher.GetComponent<MarcherPositionsManager>();

            // Check if all setSpheres are filled
            if (marcherPositionsManager != null && AreAllSetSpheresFilled(marcherPositionsManager))
            {
                Debug.Log($"{marcher.name}: All setSpheres are filled. Cannot move marcher.");
                return; // Prevent movement
            }
        }

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
                    Debug.Log("MarcherMovement: Movement constrained to the X axis.");
                }
                else if (IsGizmoZAxisHandleDragged())
                {
                    // Constrain movement to the Z axis only
                    pointOnPlane = new Vector3(transformGizmo.transform.position.x, transformGizmo.transform.position.y, pointOnPlane.z);
                    Debug.Log("MarcherMovement: Movement constrained to the Z axis.");
                }
            }
            else
            {
                // Restrict movement to the XZ plane
                pointOnPlane.y = transformGizmo.transform.position.y;
            }

            // Move the gizmo directly to the calculated position on the plane
            transformGizmo.transform.position = pointOnPlane;
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

        // Move the gizmo to the center point of the newly selected marcher(s)
        transformGizmo.transform.position = centerPoint;
        Debug.Log("MarcherMovement: Gizmo moved to the new center point of selected marchers.");
    }

    public void MoveGizmoToFirstSelectedMarcher(GameObject firstSelectedMarcher)
    {
        if (firstSelectedMarcher != null)
        {
            transformGizmo.transform.position = firstSelectedMarcher.transform.position;
            firstSelectedMarcher.transform.localPosition = Vector3.zero;
            Debug.Log($"MarcherMovement: Gizmo moved to the position of the first selected marcher: {firstSelectedMarcher.name}.");
        }
    }
    public void HideTransformGizmo()
    {
        Destroy(transformGizmo);
        //Debug.Log("MarcherMovement: Transform gizmo destroyed.");
        transformGizmo = null;
    }
}
