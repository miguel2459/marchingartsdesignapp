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
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                cameraModeManager?.HandlePanOrRotateIntent(delta);
            }

            lastTouchPos = touch.position;
        }
        else if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touch1Prev = touch1.position - touch1.deltaPosition;
            Vector2 touch2Prev = touch2.position - touch2.deltaPosition;

            float prevDist = Vector2.Distance(touch1Prev, touch2Prev);
            float currDist = Vector2.Distance(touch1.position, touch2.position);
            float deltaZoom = currDist - prevDist;

            cameraModeManager?.HandleZoomIntent(deltaZoom * 0.02f); // Adjust multiplier as needed
            lastTouchDistance = currDist;
        }
    }

    private void HandleMouseInput()
    {
        // ⏳ We'll preserve existing camera Update() logic here later
    }
}