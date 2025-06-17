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
    private Vector2 gestureStartTouch0;
    private Vector2 gestureStartTouch1;
    private int gestureStartFrame = -1;
    private bool gestureInitialized = false;
    private int deadPhaseFrames = 4;
    private float movementThreshold = 8f;

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

        // 🔁 Reset gesture state when fingers are lifted
        if (touchCount != 2)
        {
            TouchInputContext.IsCameraGestureActive = false;
            currentGesture = GestureMode.None;
            gestureInitialized = false;
            gestureStartFrame = -1;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // 🔒 Lock gesture phase after first contact
        if (!gestureInitialized)
        {
            gestureStartTouch0 = touch0.position;
            gestureStartTouch1 = touch1.position;
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            gestureStartFrame = Time.frameCount;
            gestureInitialized = true;
            TouchInputContext.IsCameraGestureActive = true;
            return;
        }

        int framesHeld = Time.frameCount - gestureStartFrame;
        if (framesHeld < deadPhaseFrames)
            return; // 🧊 Dead phase: wait before responding to motion

        // 🎯 Check if real movement happened since touch began
        float totalMovement =
            (touch0.position - gestureStartTouch0).magnitude +
            (touch1.position - gestureStartTouch1).magnitude;

        if (currentGesture == GestureMode.None)
        {
            if (totalMovement < movementThreshold)
                return; // Not enough user intent yet

            // Detect gesture type
            Vector2 delta0 = touch0.position - lastTouch0Pos;
            Vector2 delta1 = touch1.position - lastTouch1Pos;
            float pinchDelta = Vector2.Distance(touch0.position, touch1.position) -
                               Vector2.Distance(lastTouch0Pos, lastTouch1Pos);
            float deltaMagnitudeDiff = Mathf.Abs(delta0.magnitude - delta1.magnitude);
            Vector2 avgDelta = (delta0 + delta1) * 0.5f;

            if (Mathf.Abs(pinchDelta) > 2f)
                currentGesture = GestureMode.Zoom;
            else if (deltaMagnitudeDiff > 4f)
                currentGesture = GestureMode.Rotate;
            else
                currentGesture = GestureMode.Pan;

            Debug.Log($"🔒 Gesture locked after {framesHeld} frames: {currentGesture}");
        }

        // Execute the locked gesture only
        Vector2 movement = (touch0.position - lastTouch0Pos + touch1.position - lastTouch1Pos) * 0.5f;
        float currentPinchDelta = Vector2.Distance(touch0.position, touch1.position) -
                                  Vector2.Distance(lastTouch0Pos, lastTouch1Pos);

        switch (currentGesture)
        {
            case GestureMode.Zoom:
                cameraModeManager?.HandleZoomIntent(currentPinchDelta * 0.005f);
                break;
            case GestureMode.Pan:
                cameraModeManager?.HandlePanIntent(movement);
                break;
            case GestureMode.Rotate:
                cameraModeManager?.HandleRotateIntent(movement);
                break;
        }

        // Update for next frame
        lastTouch0Pos = touch0.position;
        lastTouch1Pos = touch1.position;
    }

    private void HandleMouseInput()
    {
        // ⏳ Placeholder for mouse-based interaction (handled elsewhere)
    }
}
