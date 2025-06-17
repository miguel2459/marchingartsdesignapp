using UnityEngine;

/// <summary>
/// Detects mouse or touch input and routes camera intent commands
/// to the active camera via CameraModeManager.
/// </summary>
public class CameraInputRouter : MonoBehaviour
{
    private bool isMobile;
    [SerializeField] private CameraModeManager cameraModeManager;

    private enum GestureMode { None, Zoom, Pan, Rotate }
    private GestureMode currentGesture = GestureMode.None;

    private Vector2 lastTouch0Pos;
    private Vector2 lastTouch1Pos;
    private bool gestureInitialized = false;

    void Awake()
    {
        isMobile = Application.isMobilePlatform || Input.touchSupported;
        Debug.Log($"📱 CameraInputRouter Awake → isMobile = {isMobile}");
    }

    void Start()
    {
        if (cameraModeManager == null)
        {
            Debug.LogError("CameraInputRouter: ❌ CameraModeManager not found in scene.");
            enabled = false;
        }
    }

    void Update()
    {
        if (isMobile)
            HandleTouchInput();
        else
            HandleMouseInput();
    }

    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        // 🔁 Reset on lift
        if (touchCount != 2)
        {
            TouchInputContext.IsCameraGestureActive = false;
            currentGesture = GestureMode.None;
            gestureInitialized = false;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // 🕓 Wait for movement before interpreting
        if (!gestureInitialized)
        {
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            gestureInitialized = true;
            TouchInputContext.IsCameraGestureActive = true;
            return; // Do NOT move camera on first frame of touch
        }

        // Calculate deltas
        Vector2 delta0 = touch0.position - lastTouch0Pos;
        Vector2 delta1 = touch1.position - lastTouch1Pos;
        Vector2 avgDelta = (delta0 + delta1) * 0.5f;
        float pinchDelta = (Vector2.Distance(touch0.position, touch1.position) -
                            Vector2.Distance(lastTouch0Pos, lastTouch1Pos));
        float deltaMagnitudeDiff = Mathf.Abs(delta0.magnitude - delta1.magnitude);

        // 🔐 Lock gesture once based on early motion
        if (currentGesture == GestureMode.None)
        {
            if (Mathf.Abs(pinchDelta) > 2f)
                currentGesture = GestureMode.Zoom;
            else if (deltaMagnitudeDiff > 4f)
                currentGesture = GestureMode.Rotate;
            else if (avgDelta.magnitude > 2f)
                currentGesture = GestureMode.Pan;
            else
                return; // Not enough motion yet
            Debug.Log($"🔒 Gesture locked: {currentGesture}");
        }

        // 🎯 Perform locked gesture only
        switch (currentGesture)
        {
            case GestureMode.Zoom:
                cameraModeManager?.HandleZoomIntent(pinchDelta * 0.005f); // softened
                break;
            case GestureMode.Pan:
                cameraModeManager?.HandlePanIntent(avgDelta);
                break;
            case GestureMode.Rotate:
                cameraModeManager?.HandleRotateIntent(avgDelta);
                break;
        }

        // Update last positions
        lastTouch0Pos = touch0.position;
        lastTouch1Pos = touch1.position;
    }

    private void HandleMouseInput()
    {
        // ⏳ Existing camera logic handles mouse input directly
    }
}
