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
    private Vector2 gestureStartTouch0; // Initial touch positions when 2 fingers first touch
    private Vector2 gestureStartTouch1; // Used for calculating cumulative movement against start
    private int gestureStartFrame = -1;
    private bool gestureInitialized = false;
    private bool gestureLockedThisFrame = false; // New flag to delay first execution

    [Header("Touch Gesture Settings")]
    [Tooltip("Number of frames to ignore initial touch movement to prevent false triggers.")]
    public int deadPhaseFrames = 4;
    [Tooltip("Minimum pixel movement to consider a single finger input as a drag for selection.")]
    public float singleFingerDragThreshold = 10f; // For single finger selection (not camera movement)
    [Tooltip("Minimum pixel movement for two fingers to consider it a camera gesture (pan/zoom/rotate).")]
    public float twoFingerMovementThreshold = 8f; // Tune this to prevent camera from activating on tiny movements
    [Tooltip("Minimum change in pinch distance (pixels) to detect a zoom gesture.")]
    public float zoomPinchThreshold = 2f; // Tune this: higher value makes it harder to accidentally zoom
    [Tooltip("Minimum difference in individual finger movement magnitudes (pixels) to detect a rotate gesture.")]
    public float rotateMagnitudeDiffThreshold = 4f; // Tune this: higher value makes it harder to accidentally rotate

    [Header("Debugging")]
    [Tooltip("If true, logs verbose messages when no touch input is detected.")]
    public bool logNoTouchInput = false; // New toggle for verbose logging


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
        gestureLockedThisFrame = false; // Reset flag at start of frame

        // Conditionally log when no touch input is present
        if (touchCount == 0 && logNoTouchInput)
        {
            Debug.Log($"[Frame {Time.frameCount}] HandleTouchInput: Touch Count = 0");
            Debug.Log($"[Frame {Time.frameCount}] IsCameraGestureActive: {TouchInputContext.IsCameraGestureActive}, IsRecentGesture: {TouchInputContext.IsRecentGesture}, currentGesture: {currentGesture}, gestureInitialized: {gestureInitialized}");
        }

        if (touchCount != 2)
        {
            // Reset gesture state if not exactly two fingers are down
            if (gestureInitialized)
            {
                Debug.Log($"[Frame {Time.frameCount}] Resetting gesture state. Touch count changed from 2 to {touchCount}. Current gesture was {currentGesture}");
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

        // --- RAW TOUCH DATA LOGGING ---
        Debug.Log($"[Frame {Time.frameCount}] Touch 0: Pos={touch0.position}, Phase={touch0.phase}");
        Debug.Log($"[Frame {Time.frameCount}] Touch 1: Pos={touch1.position}, Phase={touch1.phase}");
        // --- END RAW TOUCH DATA LOGGING ---


        // --- Gesture Initialization (first frame with 2 touches) ---
        if (!gestureInitialized)
        {
            gestureStartTouch0 = touch0.position;
            gestureStartTouch1 = touch1.position;
            lastTouch0Pos = touch0.position; // Initialize last positions for subsequent delta calculations
            lastTouch1Pos = touch1.position;
            gestureStartFrame = Time.frameCount;
            gestureInitialized = true;
            currentGesture = GestureMode.None; // Ensure it's None at start
            TouchInputContext.IsCameraGestureActive = true; // Block other inputs immediately
            Debug.Log($"[Frame {Time.frameCount}] Initializing 2-finger gesture. Touch0 start: {touch0.position}, Touch1 start: {touch1.position}. Setting TouchInputContext.IsCameraGestureActive = true.");
            return; // Skip processing movement in the very first frame to avoid using uninitialized deltas
        }

        int framesHeld = Time.frameCount - gestureStartFrame;

        // --- Dead Phase ---
        if (framesHeld <= deadPhaseFrames) // Use <= to include the 'deadPhaseFrames' itself
        {
            Vector2 currentTouch0Offset = touch0.position - gestureStartTouch0;
            Vector2 currentTouch1Offset = touch1.position - gestureStartTouch1;
            Vector2 currentTotalMovement = (currentTouch0Offset + currentTouch1Offset) * 0.5f; // Average movement from start
            float currentPinchDeltaFromStart = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(gestureStartTouch0, gestureStartTouch1);
            float currentDeltaMagnitudeDiffFromStart = Mathf.Abs(currentTouch0Offset.magnitude - currentTouch1Offset.magnitude);

            Debug.Log($"[Frame {Time.frameCount}] Dead phase ({framesHeld}/{deadPhaseFrames}): Total Movement (avg pixels from start): {currentTotalMovement.magnitude}, Pinch Delta (from start): {currentPinchDeltaFromStart}, Delta Magnitude Diff (from start): {currentDeltaMagnitudeDiffFromStart}. Waiting for motion before locking gesture.");
            // Update last positions so deltas are correct after dead phase
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
            return;
        }

        // --- Gesture Detection (after dead phase, if not locked yet) ---
        if (currentGesture == GestureMode.None)
        {
            // Calculate total movement relative to the start of the gesture (after dead phase)
            Vector2 currentTouch0Offset = touch0.position - gestureStartTouch0;
            Vector2 currentTouch1Offset = touch1.position - gestureStartTouch1;
            Vector2 currentTotalMovement = (currentTouch0Offset + currentTouch1Offset) * 0.5f; // Average movement
            float currentPinchDeltaFromStart = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(gestureStartTouch0, gestureStartTouch1);
            float currentDeltaMagnitudeDiffFromStart = Mathf.Abs(currentTouch0Offset.magnitude - currentTouch1Offset.magnitude);

            Debug.Log($"[Frame {Time.frameCount}] Detecting gesture... Total Movement (avg pixels from start): {currentTotalMovement.magnitude}, Pinch Delta (from start): {currentPinchDeltaFromStart}, Delta Magnitude Diff (from start): {currentDeltaMagnitudeDiffFromStart}");

            // Prioritize specific gestures (Zoom, Rotate) if their unique thresholds are met
            if (Mathf.Abs(currentPinchDeltaFromStart) > zoomPinchThreshold)
            {
                currentGesture = GestureMode.Zoom;
            }
            else if (currentDeltaMagnitudeDiffFromStart > rotateMagnitudeDiffThreshold)
            {
                currentGesture = GestureMode.Rotate;
            }
            // Only consider Pan if neither Zoom nor Rotate are strongly detected,
            // and there is enough overall average movement.
            else if (currentTotalMovement.magnitude >= twoFingerMovementThreshold)
            {
                currentGesture = GestureMode.Pan;
            }

            if (currentGesture != GestureMode.None)
            {
                gestureLockedThisFrame = true; // Set flag to delay first execution
                Debug.Log($"🔒 [Frame {Time.frameCount}] Gesture LOCKED: {currentGesture}. Delaying first execution.");

                // IMPORTANT: Reset last positions to current positions AFTER locking
                // This prevents the "jolt" by ensuring the next frame's delta is from this new, locked state.
                lastTouch0Pos = touch0.position;
                lastTouch1Pos = touch1.position;
            }
            else
            {
                Debug.Log($"[Frame {Time.frameCount}] Movement not enough to lock a specific gesture (Total: {currentTotalMovement.magnitude}, Pinch: {currentPinchDeltaFromStart}, Diff: {currentDeltaMagnitudeDiffFromStart}). No gesture locked yet.");
            }
        }

        // --- Execute the locked gesture only (if not locked this frame) ---
        if (currentGesture != GestureMode.None && !gestureLockedThisFrame)
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

        // Always update for next frame (even if gesture was locked this frame, to set up next delta)
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