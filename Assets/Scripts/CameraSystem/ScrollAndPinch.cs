using UnityEngine;

public class ScrollAndPinch : MonoBehaviour
{
    public enum ZoomMode { Forward, Vertical }
    public ZoomMode zoomMode = ZoomMode.Forward;

    [SerializeField] private CameraModeManager cameraModeManager;

    [Header("Camera Reference")]
    public Camera Camera;
    public bool Rotate;

    [Header("Touch Pan, Zoom, Rotate")]
    public float touchPanSpeed = 1.5f;
    public float touchZoomSensitivity = .001f;
    public float touchRotateSpeed = .2f;
    
    private float currentPitch = 0f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    private Plane plane;
    private Vector3 cameraStartPosition;

    private void Awake()
    {
        if (Camera == null)
            Camera = Camera.main;

        cameraStartPosition = Camera.transform.position;
        currentPitch = Camera.transform.eulerAngles.x;
    }

    private void Update()
    {
        //Turn on Alt modifier once two or more fingers are on screen. To avoid interacting with SelectedMarchers
        if (Input.touchCount >= 2 && !MobileModifierKeyProxy.IsAltHeld)
        {
            //Debug.Log("🤚 Activating Alt via touch gesture.");
            MobileModifierKeyProxy.SetAltHeld(true);
        }

        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            plane.SetNormalAndPosition(Vector3.up, new Vector3(0, 0.1f, 0));

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            Vector3 pos0 = PlanePosition(touch0.position);
            Vector3 pos1 = PlanePosition(touch1.position);
            Vector3 pos0Prev = PlanePosition(touch0PrevPos);
            Vector3 pos1Prev = PlanePosition(touch1PrevPos);

            // === Pan ===
            Vector3 mid = (pos0 + pos1) * 0.5f;
            Vector3 midPrev = (pos0Prev + pos1Prev) * 0.5f;
            Vector3 panDelta = (midPrev - mid) * touchPanSpeed;
            Camera.transform.Translate(panDelta, Space.World);
            Debug.Log($"📦 Pan applied: {panDelta}");

            // === Zoom ===
            float prevDist = (touch0PrevPos - touch1PrevPos).magnitude;
            float currDist = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = (currDist - prevDist) * touchZoomSensitivity;

            Vector3 camBeforeZoom = Camera.transform.position;

            if (cameraModeManager != null)
            {
                Debug.Log($"📲 Calling HandleZoomIntent with delta: {deltaMagnitudeDiff}");
                cameraModeManager.HandleZoomIntent(deltaMagnitudeDiff);
            }

            Debug.Log($"🔍 Zoom deltaMag: {deltaMagnitudeDiff:F4}, Cam Y: {Camera.transform.position.y:F2}");

            // === Rotate ===
            if (Rotate && pos1Prev != pos1)
            {
                float angle = Vector3.SignedAngle(pos1 - pos0, pos1Prev - pos0Prev, plane.normal);

                if (MobileModifierKeyProxy.IsControlHeld)
                {
                    Vector3 pitchAxis = Camera.transform.right;
                    float intendedPitch = currentPitch + angle * touchRotateSpeed;

                    // Clamp and compute delta
                    float clampedPitch = Mathf.Clamp(intendedPitch, minPitch, maxPitch);
                    float deltaPitch = clampedPitch - currentPitch;

                    Camera.transform.RotateAround(mid, pitchAxis, deltaPitch);
                    currentPitch = clampedPitch;

                    Debug.Log($"🎯 Clamped Pitch Rotate: Δ={deltaPitch:F2}°, CurrentPitch={currentPitch:F2}°");
                }
                else
                {
                    Camera.transform.RotateAround(mid, Vector3.up, angle * touchRotateSpeed);
                    Debug.Log($"🔄 Yaw Rotate (Default): {angle:F2}°");
                }
            }
        }

        if (Input.touchCount == 0 && MobileModifierKeyProxy.IsAltHeld)
        {
            Debug.Log("🧼 Touch ended — scheduling Alt unstick.");
            MobileModifierKeyProxy.ForceAltRelease(delayed: true);
        }
    }

    protected Vector3 PlanePosition(Vector2 screenPos)
    {
        Ray ray = Camera.ScreenPointToRay(screenPos);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Debug.Log($"✅ Ray hit at: {hit}");
            return hit;
        }

        Debug.LogWarning($"❌ Ray did not hit plane at screenPos: {screenPos}");
        return Vector3.zero;
    }

    public Vector3 DebugPlanePosition(Vector2 screenPos)
    {
        plane.SetNormalAndPosition(Vector3.up, new Vector3(0, 0.1f, 0));
        return PlanePosition(screenPos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + transform.up);
    }
}