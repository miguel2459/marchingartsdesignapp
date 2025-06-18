// ✅ Full refactor of HandleTouchInput() in CameraInputRouter.cs to use per-finger intent logic

using UnityEngine;

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
    private bool gestureLockedThisFrame = false;

    public int deadPhaseFrames = 4;

    void Awake()
    {
        isMobile = Application.isMobilePlatform || Input.touchSupported;
        Debug.Log($"📱 CameraInputRouter Awake → isMobile = {isMobile}");
    }

    void Start()
    {
        if (cameraModeManager == null)
        {
            Debug.LogError("CameraInputRouter: ❌ CameraModeManager not found in scene. Disabling script.");
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
        gestureLockedThisFrame = false;

        if (touchCount != 2)
        {
            if (gestureInitialized)
            {
                if (TouchInputContext.IsCameraGestureActive)
                {
                    TouchInputContext.IsCameraGestureActive = false;
                    TouchInputContext.GestureStartTime = Time.time;
                }
            }
            currentGesture = GestureMode.None;
            gestureInitialized = false;
            gestureStartFrame = -1;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (!gestureInitialized)
        {
            gestureStartTouch0 = touch0.position;
            gestureStartTouch1 = touch1.position;
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            gestureStartFrame = Time.frameCount;
            gestureInitialized = true;
            currentGesture = GestureMode.None;
            Debug.Log($"[Frame {Time.frameCount}] Initializing 2-finger gesture");
            return;
        }

        int framesHeld = Time.frameCount - gestureStartFrame;
        if (framesHeld <= deadPhaseFrames)
        {
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            return;
        }

        // --- Per-finger movement ---
        Vector2 delta0 = touch0.position - lastTouch0Pos;
        Vector2 delta1 = touch1.position - lastTouch1Pos;
        bool touch0Moved = delta0.magnitude > 0.5f;
        bool touch1Moved = delta1.magnitude > 0.5f;

        float pinchDelta = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(lastTouch0Pos, lastTouch1Pos);
        Vector2 avgMovement = (delta0 + delta1) * 0.5f;
        float angleBetween = Vector2.Angle(delta0, delta1);
        float angleOpposing = Vector2.Angle(delta0, -delta1);

        bool isZoomIntent = touch0Moved && touch1Moved && angleOpposing < 45f;
        bool isRotateIntent = (touch0Moved ^ touch1Moved); // XOR: only one moves
        bool isPanIntent = touch0Moved && touch1Moved && angleBetween < 30f;

        if (currentGesture == GestureMode.None)
        {
            if (isZoomIntent)
                currentGesture = GestureMode.Zoom;
            else if (isRotateIntent)
                currentGesture = GestureMode.Rotate;
            else if (isPanIntent)
                currentGesture = GestureMode.Pan;

            if (currentGesture != GestureMode.None)
            {
                gestureLockedThisFrame = true;
                TouchInputContext.IsCameraGestureActive = true;
                Debug.Log($"🔒 [Frame {Time.frameCount}] Gesture LOCKED: {currentGesture}");
                lastTouch0Pos = touch0.position;
                lastTouch1Pos = touch1.position;
                return;
            }
        }

        if (currentGesture != GestureMode.None && !gestureLockedThisFrame)
        {
            Debug.Log($"[Frame {Time.frameCount}] Executing {currentGesture}. Touch0 Δ={delta0}, Touch1 Δ={delta1}, Pinch Δ={pinchDelta}");

            switch (currentGesture)
            {
                case GestureMode.Zoom:
                    if (touch0Moved && touch1Moved)
                        cameraModeManager?.HandleZoomIntent(pinchDelta * 0.005f);
                    break;
                case GestureMode.Rotate:
                    cameraModeManager?.HandleRotateIntent(touch0Moved ? delta0 : delta1);
                    break;
                case GestureMode.Pan:
                    cameraModeManager?.HandlePanIntent(avgMovement);
                    break;
            }
        }

        lastTouch0Pos = touch0.position;
        lastTouch1Pos = touch1.position;
    }

    private void HandleMouseInput()
    {
        // Placeholder for desktop input (WASD, RMB drag, scrollwheel, etc.)
    }
}
