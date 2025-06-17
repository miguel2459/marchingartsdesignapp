using UnityEngine;

/// <summary>
/// Detects mouse or touch input and routes camera intent commands
/// to the active camera via CameraModeManager.
/// </summary>
public class CameraInputRouter : MonoBehaviour
{
    private bool isMobile;
    [SerializeField] private CameraModeManager cameraModeManager;
    private Vector2 lastTouchPos;
    private float lastTouchDistance;
    private float initialPinchDistance;
    private bool isPinching = false;
    private bool isTwoFingerPan = false;
    private bool isTwoFingerRotate = false;
    private float gestureStartTime = 0f;

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

        // Reset if no touch
        if (touchCount == 0)
        {
            TouchInputContext.IsCameraGestureActive = false;
            isPinching = false;
            isTwoFingerPan = false;
            isTwoFingerRotate = false;
            return;
        }

        // 2-FINGER LOGIC
        if (touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            TouchInputContext.IsCameraGestureActive = true;
            gestureStartTime = Time.time;

            Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
            Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

            float prevDistance = Vector2.Distance(touch0Prev, touch1Prev);
            float currDistance = Vector2.Distance(touch0.position, touch1.position);
            float deltaZoom = currDistance - prevDistance;

            Vector2 avgMovement = (touch0.deltaPosition + touch1.deltaPosition) * 0.5f;
            float deltaMagnitudeDiff = Mathf.Abs(touch0.deltaPosition.magnitude - touch1.deltaPosition.magnitude);

            bool isZoomGesture = Mathf.Abs(deltaZoom) > 5f;
            bool isRotateGesture = deltaMagnitudeDiff > 3f;
            bool isPanGesture = !isZoomGesture && !isRotateGesture;

            if (isZoomGesture)
            {
                cameraModeManager?.HandleZoomIntent(deltaZoom * 0.005f);
            }
            else if (isRotateGesture)
            {
                cameraModeManager?.HandleRotateIntent(avgMovement);
            }
            else if (isPanGesture)
            {
                cameraModeManager?.HandlePanIntent(avgMovement);
            }
        }

        // 1-FINGER LOGIC
        else if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Only allow 1-finger gestures to pass to selection system
            TouchInputContext.IsCameraGestureActive = false;

            // You don't need to do anything here — clickdrag already works
            // and you're intentionally letting it route to ClickMarcherSelector
        }
    }


    private void HandleMouseInput()
    {
        // ⏳ We'll preserve existing camera Update() logic here later
    }
}