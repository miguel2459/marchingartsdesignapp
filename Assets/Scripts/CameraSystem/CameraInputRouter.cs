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

    [Header("Touch Gesture Settings")]
    [Tooltip("Number of frames to ignore initial touch movement to prevent false triggers.")]
    public int deadPhaseFrames = 4;
    [Tooltip("Minimum pixel movement to consider a single finger input as a drag for selection.")]
    public float singleFingerDragThreshold = 10f; // For single finger selection (not camera movement)
    [Tooltip("Minimum pixel movement for two fingers to consider it a camera gesture (pan/zoom/rotate).")]
    public float twoFingerMovementThreshold = 8f;
    [Tooltip("Minimum change in pinch distance to detect a zoom gesture.")]
    public float zoomPinchThreshold = 2f;
    [Tooltip("Minimum difference in individual finger movement magnitudes to detect a rotate gesture.")]
    public float rotateMagnitudeDiffThreshold = 4f;

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

        // Log start of touch input handling
        Debug.Log($"[Frame {Time.frameCount}] HandleTouchInput: Touch Count = {touchCount}");
        Debug.Log($"[Frame {Time.frameCount}] IsCameraGestureActive: {TouchInputContext.IsCameraGestureActive}, currentGesture: {currentGesture}, gestureInitialized: {gestureInitialized}");


        if (touchCount != 2)
        {
            // Reset gesture state if not exactly two fingers are down
            if (gestureInitialized)
            {
                Debug.Log($"[Frame {Time.frameCount}] Resetting gesture state. Touch count not 2. Current gesture was {currentGesture}");
                TouchInputContext.IsCameraGestureActive = false;
                TouchInputContext.GestureStartTime = Time.time; // Mark end of gesture for IsRecentGesture
            }
            currentGesture = GestureMode.None;
            gestureInitialized = false;
            gestureStartFrame = -1;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // --- Gesture Initialization ---
        if (!gestureInitialized)
        {
            gestureStartTouch0 = touch0.position;
            gestureStartTouch1 = touch1.position;
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            gestureStartFrame = Time.frameCount;
            gestureInitialized = true;
            currentGesture = GestureMode.None; // Ensure it's None at start
            TouchInputContext.IsCameraGestureActive = true; // Block other inputs immediately
            Debug.Log($"[Frame {Time.frameCount}] Initializing 2-finger gesture. Touch0: {touch0.position}, Touch1: {touch1.position}. Setting TouchInputContext.IsCameraGestureActive = true.");
            return; // Skip processing movement in the very first frame to avoid jolts
        }

        int framesHeld = Time.frameCount - gestureStartFrame;

        // --- Dead Phase ---
        if (framesHeld < deadPhaseFrames)
        {
            Debug.Log($"[Frame {Time.frameCount}] Dead phase: framesHeld = {framesHeld}. Waiting for motion before locking gesture.");
            // Update last positions but don't detect gesture yet
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            return;
        }

        // --- Gesture Detection (after dead phase, if not locked yet) ---
        if (currentGesture == GestureMode.None)
        {
            Vector2 currentTotalMovement = (touch0.position - gestureStartTouch0) + (touch1.position - gestureStartTouch1);
            float currentPinchDelta = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(gestureStartTouch0, gestureStartTouch1);
            float currentDeltaMagnitudeDiff = Mathf.Abs((touch0.position - gestureStartTouch0).magnitude - (touch1.position - gestureStartTouch1).magnitude);

            Debug.Log($"[Frame {Time.frameCount}] Detecting gesture... Total Movement (pixels): {currentTotalMovement.magnitude}, Pinch Delta: {currentPinchDelta}, Delta Magnitude Diff: {currentDeltaMagnitudeDiff}");

            if (currentTotalMovement.magnitude < twoFingerMovementThreshold)
            {
                Debug.Log($"[Frame {Time.frameCount}] Movement below two-finger threshold ({currentTotalMovement.magnitude} < {twoFingerMovementThreshold}). No gesture locked yet.");
            }
            else
            {
                // Prioritize zoom and rotate as they are more distinct
                if (Mathf.Abs(currentPinchDelta) > zoomPinchThreshold)
                {
                    currentGesture = GestureMode.Zoom;
                }
                else if (currentDeltaMagnitudeDiff > rotateMagnitudeDiffThreshold)
                {
                    currentGesture = GestureMode.Rotate;
                }
                else
                {
                    // If not zoom or rotate, it's likely a pan
                    currentGesture = GestureMode.Pan;
                }
                Debug.Log($"🔒 [Frame {Time.frameCount}] Gesture LOCKED: {currentGesture}");
            }
        }

        // --- Execute the locked gesture only ---
        if (currentGesture != GestureMode.None)
        {
            Vector2 movement = (touch0.position - lastTouch0Pos + touch1.position - lastTouch1Pos) * 0.5f;
            float currentPinchDelta = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(lastTouch0Pos, lastTouch1Pos);

            Debug.Log($"[Frame {Time.frameCount}] Executing {currentGesture}. Movement: {movement}, Pinch Delta: {currentPinchDelta}.");

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
        }

        // Update for next frame
        lastTouch0Pos = touch0.position;
        lastTouch1Pos = touch1.position;
    }

    private void HandleMouseInput()
    {
        // Placeholder for mouse input. Your existing mouse input logic should go here.
        // For example:
        // if (Input.GetMouseButton(0))
        // {
        //    if (!TouchInputContext.IsCameraGestureActive && !TouchInputContext.IsRecentGesture)
        //    {
        //        // Handle single-finger-like drag for selection here
        //    }
        // }
    }
}