using UnityEngine;
using System.Collections.Generic;

public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 50f;
    public float rotationSpeed = 3f;
    public float zoomSpeed = 50f;
    public float panSpeed = 0.3f;
    public float pivotDistance = 5f; // Distance in front of the camera for pivot point
    public float altZoomSpeed = 5f; // Speed for zooming when Alt + RMB is held
    public float focusSpeed = 5f; // Speed for focusing on marchers
    public float zoomMultiplier = 1.5f; // How close to zoom in based on the distance to the focus point
    public float additionalDistanceFactor = 1.2f; // Factor to ensure the entire object is in view

    // The target distance to stop the camera when focusing on a single marcher
    public float targetFocusDistance = 5f;

    private float yaw = 0f;
    private float pitch = 0f;

    private Vector3 focusPoint;
    private List<GameObject> selectedMarchers = new List<GameObject>();
    private bool isFocusing = false;
    private Vector3 initialCameraPosition; // Store the camera's initial position

    void Update()
    {
        if (isFocusing)
        {
            if (HandleUserInputInterrupt()) // Check if the user interrupts the focus
            {
                isFocusing = false;
            }
            else
            {
                MoveCameraToFocus();
            }
        }
        else
        {
            HandleRotation();
            HandleZoom();
            HandlePanning();
            HandlePivotRotation(); // Alt + Left Mouse Button Rotation
            HandleAltZoom(); // Alt + Right Mouse Button Zoom
            HandleCameraFocus(); // Focus on selected marchers
        }
    }

    bool HandleUserInputInterrupt()
    {
        // Check if any of the movement, rotation, or zoom inputs are triggered
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
               Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) ||
               Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
               Input.GetAxis("Mouse ScrollWheel") != 0;
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftAlt)) // Right mouse button without Alt key
        {
            yaw += rotationSpeed * Input.GetAxis("Mouse X");
            pitch -= rotationSpeed * Input.GetAxis("Mouse Y");

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * zoomSpeed * Time.deltaTime;
    }

    void HandlePanning()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            Vector3 panDirection = new Vector3(-Input.GetAxis("Mouse X") * panSpeed, -Input.GetAxis("Mouse Y") * panSpeed, 0);
            transform.Translate(panDirection, Space.Self);
        }
    }

    void HandlePivotRotation()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0)) // Alt + Left mouse button
        {
            // Calculate the pivot point in front of the camera
            Vector3 pivotPoint = transform.position + transform.forward * pivotDistance;

            // Calculate rotation angles based on mouse movement
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotationY = -Input.GetAxis("Mouse Y") * rotationSpeed;

            // Rotate the camera around the pivot point
            transform.RotateAround(pivotPoint, Vector3.up, rotationX);
            transform.RotateAround(pivotPoint, transform.right, rotationY);

            // Update yaw and pitch to reflect the new rotation (optional)
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
        }
    }

    void HandleAltZoom()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1)) // Alt + Right mouse button
        {
            // Calculate the zoom amount based on both X and Y mouse movements
            float zoomAmountX = Input.GetAxis("Mouse X") * altZoomSpeed * Time.deltaTime;
            float zoomAmountY = Input.GetAxis("Mouse Y") * altZoomSpeed * Time.deltaTime;

            // Combine the zoom amounts to move the camera forward or backward
            Vector3 zoomDirection = transform.forward * (zoomAmountY + zoomAmountX);
            transform.position += zoomDirection;
        }
    }

    void HandleCameraFocus()
    {
        if (Input.GetKeyDown(KeyCode.F)) // Press F to focus on selected marchers
        {
            if (selectedMarchers.Count > 0)
            {
                CalculateFocusPoint();
                initialCameraPosition = transform.position; // Store the initial camera position
                isFocusing = true;
            }
        }
    }

    void CalculateFocusPoint()
    {
        if (selectedMarchers.Count == 1)
        {
            focusPoint = selectedMarchers[0].transform.position;
        }
        else
        {
            Vector3 totalPosition = Vector3.zero;
            foreach (var marcher in selectedMarchers)
            {
                totalPosition += marcher.transform.position;
            }
            focusPoint = totalPosition / selectedMarchers.Count;
        }
    }

    void MoveCameraToFocus()
    {
        Vector3 directionToFocus = (focusPoint - initialCameraPosition).normalized;

        // Calculate the target distance to stop the camera based on the number of selected marchers
        float targetDistance = selectedMarchers.Count == 1 ? targetFocusDistance : CalculateRequiredDistanceToFit();

        // Lerp the camera position towards the calculated target position
        transform.position = Vector3.Lerp(transform.position, focusPoint - directionToFocus * targetDistance, focusSpeed * Time.deltaTime);
        transform.LookAt(focusPoint);

        if (Vector3.Distance(transform.position, focusPoint - directionToFocus * targetDistance) < 0.1f)
        {
            isFocusing = false; // Stop focusing once we're close enough
        }
    }

    float CalculateRequiredDistanceToFit()
    {
        if (selectedMarchers.Count == 1)
        {
            return targetFocusDistance;
        }

        Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
        foreach (var marcher in selectedMarchers)
        {
            bounds.Encapsulate(marcher.transform.position);
        }

        return bounds.size.magnitude * zoomMultiplier * additionalDistanceFactor;
    }

    // This method should be called by the MarcherSelector script whenever marchers are selected/deselected
    public void SetSelectedMarchers(List<GameObject> marchers)
    {
        selectedMarchers = marchers;
    }
}
