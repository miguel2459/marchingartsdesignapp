// ✅ Full refactor of HandleTouchInput() in CameraInputRouter.cs
//    with gesture stabilization, jitter suppression, and delayed lock

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

    private bool isStabilizingAfterTouch = false;
    private int stabilizeStartFrame = -1;
    private const int stabilizationFrames = 6; // ~100ms at 60fps

    private const float initialJitterThreshold = 5f;
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
            isStabilizingAfterTouch = false;
            stabilizeStartFrame = -1;

            if (gestureInitialized && TouchInputContext.IsCameraGestureActive)
            {
                TouchInputContext.IsCameraGestureActive = false;
                TouchInputContext.GestureStartTime = Time.time;
            }

            currentGesture = GestureMode.None;
            gestureInitialized = false;
            gestureStartFrame = -1;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // Ignore touches that haven’t moved
        if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            return;

        // 🔍 Debug Log
        Debug.Log($"📱 Touch Debug: " +
                  $"T0 Δ={touch0.deltaPosition} force={touch0.pressure}, radius={touch0.radius} | " +
                  $"T1 Δ={touch1.deltaPosition} force={touch1.pressure}, radius={touch1.radius}");

        if (!gestureInitialized)
        {
            gestureStartTouch0 = touch0.position;
            gestureStartTouch1 = touch1.position;
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            gestureStartFrame = Time.frameCount;
            gestureInitialized = true;
            currentGesture = GestureMode.None;

            isStabilizingAfterTouch = true;
            stabilizeStartFrame = Time.frameCount;
            Debug.Log($"🛑 Stabilizing gesture for {stabilizationFrames} frames...");
            return;
        }

        int framesHeld = Time.frameCount - gestureStartFrame;
        if (framesHeld <= deadPhaseFrames)
        {
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            return;
        }

        if (isStabilizingAfterTouch)
        {
            int stabilizeFrames = Time.frameCount - stabilizeStartFrame;
            Vector2 delta0 = touch0.position - lastTouch0Pos;
            Vector2 delta1 = touch1.position - lastTouch1Pos;

            if (stabilizeFrames < stabilizationFrames)
            {
                // 🛡️ Filter jitter
                if (delta0.magnitude < initialJitterThreshold && delta1.magnitude < initialJitterThreshold)
                {
                    lastTouch0Pos = touch0.position;
                    lastTouch1Pos = touch1.position;
                    Debug.Log($"⏳ Ignoring jitter during stabilization... Δ0={delta0.magnitude:F2}, Δ1={delta1.magnitude:F2}");
                    return;
                }
            }

            isStabilizingAfterTouch = false;
            Debug.Log($"✅ Gesture stabilized at frame {Time.frameCount}");
        }

        Vector2 delta0Final = touch0.position - lastTouch0Pos;
        Vector2 delta1Final = touch1.position - lastTouch1Pos;

        float minDelta = 2f;
        float maxDelta = 100f;
        bool touch0Valid = delta0Final.magnitude > minDelta && delta0Final.magnitude < maxDelta;
        bool touch1Valid = delta1Final.magnitude > minDelta && delta1Final.magnitude < maxDelta;

        if (!touch0Valid || !touch1Valid)
        {
            Debug.Log($"❌ Ignored gesture due to ghost or outlier delta. Δ0={delta0Final.magnitude:F2}, Δ1={delta1Final.magnitude:F2}");
            return;
        }

        bool touch0Moved = delta0Final.magnitude > 0.5f;
        bool touch1Moved = delta1Final.magnitude > 0.5f;

        float pinchDelta = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(lastTouch0Pos, lastTouch1Pos);
        Vector2 avgMovement = (delta0Final + delta1Final) * 0.5f;
        float angleBetween = Vector2.Angle(delta0Final, delta1Final);
        float angleOpposing = Vector2.Angle(delta0Final, -delta1Final);
        float pinchMagnitude = Mathf.Abs(pinchDelta);
        float deltaRatio = Mathf.Min(delta0Final.magnitude, delta1Final.magnitude) / (Mathf.Max(delta0Final.magnitude, delta1Final.magnitude) + 0.001f);

        bool isZoomIntent = touch0Moved && touch1Moved && angleOpposing < 45f && pinchMagnitude > 1f;
        bool isRotateIntent = (touch0Moved ^ touch1Moved);
        bool isPanIntent = touch0Moved && touch1Moved &&
                           angleBetween < 25f &&
                           pinchMagnitude < 2.5f &&
                           deltaRatio > 0.6f;

        if (currentGesture == GestureMode.None)
        {
            if (isZoomIntent)
            {
                currentGesture = GestureMode.Zoom;
                Debug.Log($"🔍 ZOOM intent detected: angleOpposing={angleOpposing:F1}, pinch={pinchDelta:F2}");
            }
            else if (isRotateIntent)
            {
                currentGesture = GestureMode.Rotate;
                Debug.Log($"🔄 ROTATE intent detected.");
            }
            else if (isPanIntent)
            {
                currentGesture = GestureMode.Pan;
                Debug.Log($"📦 PAN intent detected: angleBetween={angleBetween:F1}, ratio={deltaRatio:F2}");
            }

            if (currentGesture != GestureMode.None)
            {
                gestureLockedThisFrame = true;
                TouchInputContext.IsCameraGestureActive = true;
                Debug.Log($"🔒 Gesture LOCKED: {currentGesture}");
                lastTouch0Pos = touch0.position;
                lastTouch1Pos = touch1.position;
                return;
            }
        }

        if (currentGesture != GestureMode.None)
        {
            if (gestureLockedThisFrame)
            {
                Debug.Log($"⏸️ Holding camera still — first frame after locking: {currentGesture}");
            }
            else
            {
                Debug.Log($"▶️ Executing {currentGesture}: Δ0={delta0Final}, Δ1={delta1Final}, pinch={pinchDelta:F2}");

                switch (currentGesture)
                {
                    case GestureMode.Zoom:
                        if (touch0Moved && touch1Moved)
                            cameraModeManager?.HandleZoomIntent(pinchDelta * 0.005f);
                        break;
                    case GestureMode.Rotate:
                        cameraModeManager?.HandleRotateIntent(touch0Moved ? delta0Final : delta1Final);
                        break;
                    case GestureMode.Pan:
                        cameraModeManager?.HandlePanIntent(avgMovement);
                        break;
                }
            }
        }

        lastTouch0Pos = touch0.position;
        lastTouch1Pos = touch1.position;
    }

    private void HandleMouseInput()
    {
        // Optional desktop input logic
    }
}
