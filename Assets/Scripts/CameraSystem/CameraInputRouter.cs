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
    private bool gestureLockedThisFrame = false; // Flag to delay first execution of locked gesture

    private bool isStabilizingAfterTouch = false;
    private int stabilizeStartFrame = -1;

    [Header("Touch Gesture Settings")]
    [Tooltip("Number of frames to ignore initial touch movement to prevent false triggers. (~100-200ms)")]
    public int deadPhaseFrames = 10; // Increased from 4
    [Tooltip("Minimum pixel movement to consider a single finger input as a drag for selection. (Not directly used by camera system)")]
    public float singleFingerDragThreshold = 10f;
    [Tooltip("Minimum pixel movement for two fingers to consider it a camera gesture (pan/zoom/rotate).")]
    public float twoFingerMovementThreshold = 20f; // Increased from 8f for less sensitivity
    [Tooltip("Minimum pixel movement for each individual touch to overcome initial jitter and allow gesture locking.")]
    public float initialJitterThreshold = 10f; // Increased from 5f, now public for tuning
    [Tooltip("Number of frames for initial stabilization after two fingers touch down. (~160-200ms)")]
    public int stabilizationFrames = 10; // Increased from 6

    void Awake()
    {
        isMobile = Application.isMobilePlatform || Input.touchSupported;
        // Debug.Log($"📱 CameraInputRouter Awake → isMobile = {isMobile}");
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

        // Reset gesture state when touches end
        if (touchCount == 0)
        {
            if (currentGesture != GestureMode.None || gestureInitialized || isStabilizingAfterTouch)
            {
                TouchInputContext.IsCameraGestureActive = false;
                TouchInputContext.GestureStartTime = Time.time; // Mark recent gesture for single-touch blocking
            }
            currentGesture = GestureMode.None;
            gestureStartFrame = -1;
            gestureInitialized = false;
            gestureLockedThisFrame = false;
            isStabilizingAfterTouch = false;
            stabilizeStartFrame = -1;
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = (touchCount > 1) ? Input.GetTouch(1) : default;

        // Single touch handling (e.g., for selection)
        if (touchCount == 1)
        {
            // Block single touch if a camera gesture was recently active or is still active
            if (TouchInputContext.IsCameraGestureActive || TouchInputContext.IsRecentGesture)
            {
                // Prevents accidental selection/interaction immediately after a camera gesture
                return;
            }

            // Implement your single-finger drag/selection logic here if needed.
            // Example: if (touch0.phase == TouchPhase.Moved && Vector2.Distance(touch0.position, touch0.rawPosition) > singleFingerDragThreshold) { ... }
            return;
        }

        // Two or more touches (camera gestures: Pan, Zoom, Rotate)
        if (touchCount >= 2)
        {
            // Initialize gesture state on the first frame two fingers are detected
            if (currentGesture == GestureMode.None && !gestureInitialized)
            {
                gestureStartTouch0 = touch0.position;
                gestureStartTouch1 = touch1.position;
                lastTouch0Pos = touch0.position; // Initialize last positions to current touch positions
                lastTouch1Pos = touch1.position;
                gestureStartFrame = Time.frameCount;
                gestureInitialized = true;
                gestureLockedThisFrame = false; // Ensure not locked on initialization
                isStabilizingAfterTouch = true; // Enter stabilization phase
                stabilizeStartFrame = Time.frameCount;
                // Debug.Log($"[Frame {Time.frameCount}] 🖖 Two-finger touch detected. Initializing gesture and stabilizing.");
            }

            if (!gestureInitialized) return; // Should not happen after initialization, but for safety

            // --- Stabilization Phase ---
            // During this phase, input is ignored to allow fingers to settle
            if (isStabilizingAfterTouch)
            {
                int stabilizationElapsedFrames = Time.frameCount - stabilizeStartFrame;
                if (stabilizationElapsedFrames < stabilizationFrames)
                {
                    // Update last positions to current, effectively "eating" movement during stabilization
                    lastTouch0Pos = touch0.position;
                    lastTouch1Pos = touch1.position;
                    // Debug.Log($"[Frame {Time.frameCount}] ⏳ Stabilizing... Frames: {stabilizationElapsedFrames}/{stabilizationFrames}");
                    return; // Skip gesture detection and execution during stabilization
                }
                else
                {
                    isStabilizingAfterTouch = false; // Exit stabilization
                    // Re-initialize last positions after stabilization to get clean deltas post-stabilization
                    lastTouch0Pos = touch0.position;
                    lastTouch1Pos = touch1.position;
                    // Debug.Log($"[Frame {Time.frameCount}] ✅ Stabilization complete. Starting gesture detection.");
                }
            }

            // Calculate cumulative movement from initial touch down after stabilization
            Vector2 currentTwoFingerMovement0 = touch0.position - gestureStartTouch0;
            Vector2 currentTwoFingerMovement1 = touch1.position - gestureStartTouch1;

            float cumulativeMovementMagnitude0 = currentTwoFingerMovement0.magnitude;
            float cumulativeMovementMagnitude1 = currentTwoFingerMovement1.magnitude;

            // Only attempt to lock a gesture if no gesture is currently locked
            if (currentGesture == GestureMode.None)
            {
                // Ensure both fingers have moved past their individual jitter thresholds
                bool touch0MovedPastJitter = cumulativeMovementMagnitude0 > initialJitterThreshold;
                bool touch1MovedPastJitter = cumulativeMovementMagnitude1 > initialJitterThreshold;

                // Calculate the overall two-finger movement magnitude (e.g., average displacement)
                float totalCumulativeMovement = (currentTwoFingerMovement0 + currentTwoFingerMovement1).magnitude * 0.5f;

                // Lock gesture if both fingers have moved past jitter and total movement is significant
                if (touch0MovedPastJitter && touch1MovedPastJitter && totalCumulativeMovement > twoFingerMovementThreshold)
                {
                    gestureLockedThisFrame = true; // Mark as locked in this frame to prevent immediate execution
                    TouchInputContext.IsCameraGestureActive = true; // Signal that a camera gesture is active

                    // Determine specific gesture type based on relative finger movements
                    float pinchDeltaFromStart = Vector2.Distance(touch0.position, touch1.position) - Vector2.Distance(gestureStartTouch0, gestureStartTouch1);
                    float deltaMagnitudeDifference = Mathf.Abs(currentTwoFingerMovement0.magnitude - currentTwoFingerMovement1.magnitude);

                    // These thresholds are critical for accurate gesture classification
                    if (Mathf.Abs(pinchDeltaFromStart) > 25f) // Increased from 10f for stricter zoom detection
                        currentGesture = GestureMode.Zoom;
                    else if (deltaMagnitudeDifference > 15f) // Increased from 8f for stricter rotation detection
                        currentGesture = GestureMode.Rotate;
                    else
                        currentGesture = GestureMode.Pan;

                    // Debug.Log($"[Frame {Time.frameCount}] 🔒 Gesture LOCKED: {currentGesture}. Initial Pinch Change: {pinchDeltaFromStart:F2}, Delta Magnitude Diff: {deltaMagnitudeDifference:F2}, Total Cumulative Movement: {totalCumulativeMovement:F2}");

                    // Reset last positions at the moment of locking to ensure the first executed delta
                    // is relative to the "locked" position, preventing an initial jolt.
                    lastTouch0Pos = touch0.position;
                    lastTouch1Pos = touch1.position;

                    return; // Delay actual execution by one frame after locking
                }
            }

            // Execute the locked gesture in subsequent frames (after gestureLockedThisFrame is false)
            if (currentGesture != GestureMode.None && !gestureLockedThisFrame)
            {
                // Calculate current frame's deltas (relative to previous frame)
                Vector2 delta0 = touch0.position - lastTouch0Pos;
                Vector2 delta1 = touch1.position - lastTouch1Pos;

                Vector2 avgMovement = (delta0 + delta1) * 0.5f;
                float currentPinchDelta = Vector2.Distance(touch0.position, touch1.position) -
                                          Vector2.Distance(lastTouch0Pos, lastTouch1Pos);

                // Check if touches actually moved significantly this frame to avoid processing micro-jitters
                bool touch0Moved = delta0.magnitude > 0.1f; // Small epsilon
                bool touch1Moved = delta1.magnitude > 0.1f; // Small epsilon

                // Debug.Log($"[Frame {Time.frameCount}] ▶️ Executing {currentGesture}. Movement: {avgMovement}, Pinch Delta: {currentPinchDelta:F2}");

                switch (currentGesture)
                {
                    case GestureMode.Zoom:
                        // Only apply zoom if both touches are moving significantly to prevent drift
                        if (touch0Moved && touch1Moved)
                            cameraModeManager?.HandleZoomIntent(currentPinchDelta * 0.005f);
                        break;
                    case GestureMode.Rotate:
                        // Apply rotation based on average movement or specific touch if more dominant
                        cameraModeManager?.HandleRotateIntent(avgMovement);
                        break;
                    case GestureMode.Pan:
                        cameraModeManager?.HandlePanIntent(avgMovement);
                        break;
                }
            }

            // Update last positions for the next frame's delta calculation
            lastTouch0Pos = touch0.position;
            lastTouch1Pos = touch1.position;
        }
    }

    private void HandleMouseInput()
    {
        // Placeholder for mouse input. Your existing mouse input logic should go here.
        // This method can be expanded if you have desktop-specific camera controls.
    }
}