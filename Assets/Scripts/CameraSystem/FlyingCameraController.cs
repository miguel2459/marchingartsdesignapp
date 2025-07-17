using UnityEngine;
using System.Collections.Generic;

public class FlyingCameraController : MonoBehaviour, ICameraFocusHandler
{
    [Header("Movement Settings")]
    public float rotationSpeed = 3f;
    public float panSpeed = 0.3f;
    public float pivotDistance = 5f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 50f;
    public float zoomMultiplier = 1f;

    [Header("Focus Settings")]
    public float focusSpeed;
    public float additionalDistanceFactor;

    [Header("Camera Boundaries")]
    [SerializeField] private float minX = -20f;
    [SerializeField] private float maxX = 75f;
    [SerializeField] private float minY = 5f;
    [SerializeField] private float maxY = 40f;
    [SerializeField] private float minZ = 5f;
    [SerializeField] private float maxZ = 120f;

    [Header("UI Panels (for accurate viewport)")]
    [SerializeField] private RectTransform portraitPanel;
    [SerializeField] private RectTransform landscapePanel;

    [Header("References")]
    public TransformGizmoManager gizmoManager;
    [SerializeField] private CameraModeManager cameraModeManager;

    [Range(0.5f, 1f)]
    [SerializeField] private float visibleVerticalPercent = 0.75f;

    private const float focusPitchAngle = 60f;
    private float yaw;
    private float pitch;

    private bool isFocusing = false;
    private bool isActive = true;
    private bool isMobile;

    private List<GameObject> selectedMarchers = new();
    
    [Header("Debug UI")]
    [SerializeField] private TMPro.TextMeshProUGUI debugText;


    private void Awake()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        isMobile = Application.isMobilePlatform;
    }

    private void Update()
    {
        if (!isActive) return;

        if (isFocusing)
        {
            if (UserInterruptedFocus()) isFocusing = false;
            else MoveCameraToFocus();
        }
        //UpdateDebugInfo();
        if (isMobile) return;
    }

    public void Enable() => isActive = true;
    public void Disable() => isActive = false;

    private bool UserInterruptedFocus()
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
        Vector3 pan = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0f);
        transform.Translate(pan, Space.Self);
        ClampPositionToBounds();
    }

    public void ApplyPivot(Vector2 delta)
    {
        Vector3 pivotPoint = transform.position + transform.forward * pivotDistance;

        yaw += delta.x * rotationSpeed * Time.deltaTime;
        pitch -= delta.y * rotationSpeed * Time.deltaTime;
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

    private void MoveCameraToFocus()
    {
        Vector3 targetFocus;
        float targetDistance;
        Bounds bounds = new();

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
                bounds = new Bounds(targetFocus, Vector3.zero);
                Debug.Log($"🎯 Focusing on single marcher at {targetFocus} with fixed distance {targetDistance}");
            }
            else
            {
                bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
                foreach (var m in selectedMarchers) bounds.Encapsulate(m.transform.position);
                targetFocus = bounds.center;
                targetDistance = CalculateRequiredDistanceToFit();
                // Debug.Log($"🎯 Focusing on {selectedMarchers.Count} marchers center at {targetFocus} with dynamic distance {targetDistance}");
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

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 finalPosition = targetFocus - direction * targetDistance;

        float verticalClip = 1f - CalculateVisibleVerticalPercent();
        Vector3 cameraUp = rotation * Vector3.up;
        finalPosition += cameraUp * bounds.size.z * 0.5f * verticalClip;

        float aspect = (float)Screen.width / Screen.height;
        float horizontalClip = Mathf.Clamp01((aspect - 1f) / 0.8f) * verticalClip;
        Vector3 cameraRight = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        finalPosition -= cameraRight * bounds.size.x * 0.5f * horizontalClip;

        if (finalPosition.y < 10f)
        {
            finalPosition.y = 10f;
            Debug.Log("🧱 Clamped camera Y to 10f to prevent underground zoom.");
        }

        transform.position = Vector3.Lerp(transform.position, finalPosition, focusSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, focusSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, finalPosition) < 0.1f)
            isFocusing = false;
    }

    private float CalculateRequiredDistanceToFit()
    {
        if (selectedMarchers.Count == 1) return 5f;

        Bounds bounds = new(selectedMarchers[0].transform.position, Vector3.zero);
        foreach (var m in selectedMarchers) bounds.Encapsulate(m.transform.position);

        float vertical = bounds.size.z;
        float horizontal = bounds.size.x;
        float aspect = (float)Screen.width / Screen.height;

        float shapeRatio = horizontal / vertical;
        float screenRatio = aspect;
        float normalizedShape = Mathf.Atan(shapeRatio);
        float normalizedAspect = Mathf.Atan(screenRatio);
        float difference = Mathf.Abs(normalizedShape - normalizedAspect);

        if (difference < 0.1f)
            additionalDistanceFactor = 1f;
        else if ((shapeRatio > 1f && screenRatio > 1f) || (shapeRatio < 1f && screenRatio < 1f))
            additionalDistanceFactor = 2.5f;
        else
            additionalDistanceFactor = 0.5f;

        float pitchRad = focusPitchAngle * Mathf.Deg2Rad;
        float verticalFOV = pitchRad;
        float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(pitchRad * 0.5f) * aspect);

        float distZ = (vertical * 0.5f) / Mathf.Tan(verticalFOV * 0.5f);
        float distX = (horizontal * 0.5f) / Mathf.Tan(horizontalFOV * 0.5f);
        float baseDistance = Mathf.Max(distX, distZ);

        float visible = CalculateVisibleVerticalPercent();
        return baseDistance / visible * additionalDistanceFactor * 1.1f;
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
        // Debug.Log($"📐 Visible viewport: {percent * 100:F1}% of screen height");
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

        // Debug.Log($"📸 Focus triggered on {selectedMarchers.Count} selected marcher(s). Focal point = {focalPoint}");
        isFocusing = true;
        MoveCameraToFocus();
    }
    
    private void UpdateDebugInfo()
    {
        if (debugText == null) return;

        string boundsInfo = "—";
        if (selectedMarchers != null && selectedMarchers.Count > 0)
        {
            Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
            foreach (var marcher in selectedMarchers)
                bounds.Encapsulate(marcher.transform.position);

            boundsInfo = $"Center: {bounds.center:F2}, Size: {bounds.size:F2}";
        }

        string camPos = transform.position.ToString("F2");
        string camRot = transform.rotation.eulerAngles.ToString("F2");
        string visiblePercent = (CalculateVisibleVerticalPercent() * 100f).ToString("F1");

        debugText.text =
            $"📷 FlyingCamera Debug\n" +
            $"- Pos: {camPos}\n" +
            $"- Rot (Euler): {camRot}\n" +
            $"- Selected: {selectedMarchers.Count}\n" +
            $"- Bounds: {boundsInfo}\n" +
            $"- Visible %: {visiblePercent}%\n" +
            $"- Pitch: {pitch:F2}°, Yaw: {yaw:F2}°\n" +
            $"- Focusing: {isFocusing}";
    }

}
