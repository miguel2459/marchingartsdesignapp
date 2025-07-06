using UnityEngine;
using System.Collections.Generic;

public class FlyingCameraController : MonoBehaviour, ICameraFocusHandler

{
    public float rotationSpeed = 3f;

    public float panSpeed = 0.3f;
    public float pivotDistance = 5f;
    
    [Header("Desktop Zoom Settings")]
    public float zoomSpeed = 50f;
    public float zoomMultiplier = 1f;
    
    public float focusSpeed;
    public float additionalDistanceFactor;
    private float yaw = 0f;
    private float pitch = 0f;
    
    private List<GameObject> selectedMarchers = new List<GameObject>();
    private bool isFocusing = false;
    private bool isActive = true;
    private bool isMobile;
    
    [Header("Camera Boundaries")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 75f;
    [SerializeField] private float minY = 5f;
    [SerializeField] private float maxY = 40f;
    [SerializeField] private float minZ = 5f;
    [SerializeField] private float maxZ = 120f;


    public TransformGizmoManager gizmoManager;
    [SerializeField] private CameraModeManager cameraModeManager;
    [Range(0.5f, 1f)]
    [SerializeField] private float visibleVerticalPercent = 0.75f;
    private const float focusPitchAngle = 60f; // degrees
    
    [Header("UI Panels (for accurate viewport)")]
    [SerializeField] private RectTransform portraitPanel;
    [SerializeField] private RectTransform landscapePanel;

    void Awake()
    {
        //Debug.Log($"[FlyingCameraController] 🔵 Awake — Position: {transform.position}, Rotation: {transform.rotation}");
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        isMobile = Application.isMobilePlatform;
    }

    void Update()
    {
        if (!isActive) return;
        if (isFocusing)
        {
            if (HandleUserInputInterrupt()) isFocusing = false;
            else MoveCameraToFocus();
        }
        if (isMobile) return; // 🚫 Skip keyboard/mouse input on mobile
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

    public void ApplyZoom(float delta)
    {
        float adjustedDelta = delta * zoomSpeed * zoomMultiplier * Time.deltaTime;
        transform.position += transform.forward * adjustedDelta;
        ClampPositionToBounds();
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
        ClampPositionToBounds();
    }
    
    public void ApplyPivot(Vector2 delta)
    {
        Vector3 pivotPoint = transform.position + transform.forward * pivotDistance;

        yaw += delta.x * rotationSpeed * Time.deltaTime;
        pitch -= delta.y * rotationSpeed * Time.deltaTime;

        // Clamp pitch to avoid flipping
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rotation * Vector3.forward;

        transform.position = pivotPoint - direction * pivotDistance;
        transform.LookAt(pivotPoint);
        
        ClampPositionToBounds();
    }
    
    private void ClampPositionToBounds()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }

    void MoveCameraToFocus()
    {
        Vector3 targetFocus;
        float targetDistance;

        if (gizmoManager != null && gizmoManager.HasActiveGizmo)
        {
            targetFocus = gizmoManager.transformGizmo.transform.position;
            targetDistance = 5f;
            Debug.Log($"🎯 Focusing on Gizmo at {targetFocus} with fixed distance {targetDistance}");
        }
        else if (selectedMarchers.Count > 0)
        {
            if (selectedMarchers.Count == 1)
            {
                targetFocus = selectedMarchers[0].transform.position;
                targetDistance = 5f;
                Debug.Log($"🎯 Focusing on single marcher at {targetFocus} with fixed distance {targetDistance}");
            }
            else
            {
                Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
                foreach (var marcher in selectedMarchers)
                    bounds.Encapsulate(marcher.transform.position);

                targetFocus = bounds.center;
                targetDistance = CalculateRequiredDistanceToFit();

                float uiClipPercent = 1f - CalculateVisibleVerticalPercent();
                Vector3 cameraUp = Quaternion.Euler(focusPitchAngle, yaw, 0f) * Vector3.up;
                targetFocus -= cameraUp * bounds.size.z * 0.5f * uiClipPercent;

                Debug.Log($"🎯 Focusing on center of {selectedMarchers.Count} marchers at {targetFocus} with dynamic distance {targetDistance}");
            }
        }
        else
        {
            Debug.LogWarning("❌ No target for camera focus — no gizmo and no marchers selected.");
            isFocusing = false;
            return;
        }

        pitch = focusPitchAngle;
        yaw = 90f;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rot * Vector3.forward;
        Vector3 finalPosition = targetFocus - direction * targetDistance;

        transform.position = Vector3.Lerp(transform.position, finalPosition, focusSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, focusSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, finalPosition) < 0.1f)
        {
            isFocusing = false;
        }
    }
    float CalculateRequiredDistanceToFit()
    {
        if (selectedMarchers.Count == 1)
            return 5f;

        Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
        foreach (var marcher in selectedMarchers)
            bounds.Encapsulate(marcher.transform.position);

        float verticalSize = bounds.size.z;
        float halfHeight = verticalSize * 0.5f;
        float angleRad = focusPitchAngle * Mathf.Deg2Rad;
        float distance = halfHeight / Mathf.Tan(angleRad);

        distance = distance / CalculateVisibleVerticalPercent();
        return distance * additionalDistanceFactor;
    }

    private float CalculateVisibleVerticalPercent()
    {
        float screenHeight = Screen.height;
        float uiHeight = 0f;

        if (portraitPanel != null && portraitPanel.gameObject.activeSelf)
            uiHeight = portraitPanel.rect.height;
        else if (landscapePanel != null && landscapePanel.gameObject.activeSelf)
            uiHeight = landscapePanel.rect.height;

        float visiblePixels = screenHeight - uiHeight;
        float percent = Mathf.Clamp01(visiblePixels / screenHeight);
    
        Debug.Log($"📐 Visible viewport: {percent * 100:F1}% of screen height after subtracting UI panel ({uiHeight}px)");
        return percent;
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
        
        isFocusing = true;

        MoveCameraToFocus();
    }
}
