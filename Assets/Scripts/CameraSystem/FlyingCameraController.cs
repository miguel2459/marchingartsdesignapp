using UnityEngine;
using System.Collections.Generic;

public class FlyingCameraController : MonoBehaviour, ICameraFocusHandler

{
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 50f;
    public float rotationSpeed = 3f;
    public float zoomSpeed = 50f;
    public float panSpeed = 0.3f;
    public float pivotDistance = 5f;
    public float altZoomSpeed = 5f;
    public float focusSpeed = 5f;
    public float zoomMultiplier = 1.5f;
    public float additionalDistanceFactor = 1.2f;
    public float targetFocusDistance = 5f;
    private Camera topDownCam;
    private float yaw = 0f;
    private float pitch = 0f;

    private Vector3 focusPoint;
    private Vector3 initialCameraPosition;
    private List<GameObject> selectedMarchers = new List<GameObject>();
    private bool isFocusing = false;
    private bool isActive = true;

    public TransformGizmoManager gizmoManager;
    [SerializeField] private CameraModeManager cameraModeManager;

    void Awake()
    {
        //Debug.Log($"[FlyingCameraController] 🔵 Awake — Position: {transform.position}, Rotation: {transform.rotation}");
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (!isActive) return;

        if (isFocusing)
        {
            if (HandleUserInputInterrupt()) isFocusing = false;
            else MoveCameraToFocus();
        }
        else
        {
            HandleRotation();
            HandleZoom();
            HandlePanning();
            HandlePivotRotation();
            HandleAltZoom();
        }
    }

    public void Enable() => isActive = true;
    public void Disable() => isActive = false;

    bool HandleUserInputInterrupt()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.E) ||
               Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.R) ||
               Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.G) ||
               Input.GetKey(KeyCode.B) || Input.GetKey(KeyCode.V) ||
               Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.X) ||
               Input.GetMouseButton(1) || Input.GetMouseButton(2) ||
               Input.GetAxis("Mouse ScrollWheel") != 0;
    }

    public void SetInitialTransform(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        //Debug.Log($"[FlyingCameraController] 🧭 SetInitialTransform — Position: {transform.position}, Rotation: {transform.rotation}");
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.LeftAlt))
        {
            yaw += rotationSpeed * Input.GetAxis("Mouse X");
            pitch -= rotationSpeed * Input.GetAxis("Mouse Y");
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }

    void HandleZoom()
    {
        if (cameraModeManager != null && cameraModeManager.IsInputBlocked()) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * zoomSpeed * Time.deltaTime;
    }

    void HandlePanning()
    {
        if (Input.GetMouseButton(2))
        {
            Vector3 panDirection = new Vector3(-Input.GetAxis("Mouse X") * panSpeed, -Input.GetAxis("Mouse Y") * panSpeed, 0);
            transform.Translate(panDirection, Space.Self);
        }
    }

    void HandlePivotRotation()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
        {
            Vector3 pivotPoint = transform.position + transform.forward * pivotDistance;
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotationY = -Input.GetAxis("Mouse Y") * rotationSpeed;

            transform.RotateAround(pivotPoint, Vector3.up, rotationX);
            transform.RotateAround(pivotPoint, transform.right, rotationY);

            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
        }
    }

    void HandleAltZoom()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
        {
            float zoomAmountX = Input.GetAxis("Mouse X") * altZoomSpeed * Time.deltaTime;
            float zoomAmountY = Input.GetAxis("Mouse Y") * altZoomSpeed * Time.deltaTime;
            Vector3 zoomDirection = transform.forward * (zoomAmountY + zoomAmountX);
            transform.position += zoomDirection;
        }
    }

    public void ApplyZoom(float delta)
    {
        transform.position += transform.forward * delta * zoomSpeed * Time.deltaTime;
    }

    public void ApplyRotation(Vector2 delta)
    {
        yaw += delta.x * rotationSpeed * Time.deltaTime;
        pitch -= delta.y * rotationSpeed * Time.deltaTime;
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
    
    public void ApplyPan(Vector2 delta)
    {
        Vector3 panDirection = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0);
        transform.Translate(panDirection, Space.Self);
    }



    void MoveCameraToFocus()
    {
        Vector3 targetFocus;
        float targetDistance;

        if (gizmoManager != null && gizmoManager.IsGizmoActive())
        {
            targetFocus = gizmoManager.transformGizmo.transform.position;
            targetDistance = targetFocusDistance;
            Debug.Log($"🎯 Focusing on Gizmo at {targetFocus} with fixed distance {targetDistance}");
        }
        else if (selectedMarchers.Count > 0)
        {
            if (selectedMarchers.Count == 1)
            {
                targetFocus = selectedMarchers[0].transform.position;
                targetDistance = targetFocusDistance;
                Debug.Log($"🎯 Focusing on single marcher at {targetFocus} with fixed distance {targetDistance}");
            }
            else
            {
                Vector3 totalPosition = Vector3.zero;
                foreach (var marcher in selectedMarchers)
                    totalPosition += marcher.transform.position;

                targetFocus = totalPosition / selectedMarchers.Count;
                targetDistance = CalculateRequiredDistanceToFit();

                Debug.Log($"🎯 Focusing on center of {selectedMarchers.Count} marchers at {targetFocus} with dynamic distance {targetDistance}");
            }
        }
        else
        {
            Debug.LogWarning("❌ No target for camera focus — no gizmo and no marchers selected.");
            isFocusing = false;
            return;
        }

        Vector3 directionToFocus = (targetFocus - initialCameraPosition).normalized;
        Vector3 finalPosition = targetFocus - directionToFocus * targetDistance;

        Debug.DrawLine(initialCameraPosition, finalPosition, Color.cyan); // Visual line in Scene view

        Debug.Log($"📸 Moving camera from {transform.position} → {finalPosition} (direction {directionToFocus})");

        transform.position = Vector3.Lerp(transform.position, finalPosition, focusSpeed * Time.deltaTime);
        transform.LookAt(targetFocus);

        float distanceToTarget = Vector3.Distance(transform.position, finalPosition);
        Debug.Log($"📏 Distance to target: {distanceToTarget}");

        if (distanceToTarget < 0.1f)
        {
            Debug.Log("✅ Focus complete — camera arrived at target.");
            isFocusing = false;
        }
    }


    float CalculateRequiredDistanceToFit()
    {
        if (selectedMarchers.Count == 1)
            return targetFocusDistance;

        Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
        foreach (var marcher in selectedMarchers)
            bounds.Encapsulate(marcher.transform.position);

        return bounds.size.magnitude * zoomMultiplier * additionalDistanceFactor;
    }

    public void SetSelectedMarchers(List<GameObject> marchers)
    {
        selectedMarchers = marchers;
    }
    public void FocusOnSelection(Vector3 focalPoint)
    {
        if (selectedMarchers.Count == 0)
        {
            Debug.LogWarning("❌ Focus aborted: No marchers selected.");
            return;
        }

        Debug.Log($"📸 Focus triggered on {selectedMarchers.Count} selected marcher(s). Focal point = {focalPoint}");

        focusPoint = focalPoint;
        initialCameraPosition = transform.position;
        isFocusing = true;

        MoveCameraToFocus();
    }
}
