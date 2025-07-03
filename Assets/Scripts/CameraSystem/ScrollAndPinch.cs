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
    public float touchPanSpeed = 1f;
    public float touchZoomSensitivity = 1f;
    public float touchRotateSpeed = 1f;

    [Header("Zoom Clamps (relative to start)")]
    public float maxZoomOutDistance = 30f;
    public float maxZoomInDistance = 10f;

    private Plane plane;
    private Vector3 cameraStartPosition;

    private void Awake()
    {
        if (Camera == null)
            Camera = Camera.main;

        cameraStartPosition = Camera.transform.position;
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

            if (!cameraModeManager.IsTopDown())
            {
                ClampZoom(camBeforeZoom);
            }

            // === Rotate ===
            if (Rotate && pos1Prev != pos1)
            {
                float angle = Vector3.SignedAngle(pos1 - pos0, pos1Prev - pos0Prev, plane.normal);
                Camera.transform.RotateAround(mid, plane.normal, angle * touchRotateSpeed);
                Debug.Log($"🔄 Rotate angle: {angle:F2} degrees");
            }
        }

        if (Input.touchCount == 0 && MobileModifierKeyProxy.IsAltHeld)
        {
            Debug.Log("🧼 Touch ended — scheduling Alt unstick.");
            MobileModifierKeyProxy.ForceAltRelease(delayed: true);
        }
    }

    private void ClampZoom(Vector3 camBeforeZoom)
    {
        float currentY = Camera.transform.position.y;
        float startY = cameraStartPosition.y;

        float deltaY = currentY - startY;
        if (deltaY > maxZoomOutDistance || deltaY < -maxZoomInDistance || currentY <= 1f)
        {
            Debug.LogWarning($"⛔ Zoom clamped: Y={currentY:F2} (allowed: {startY - maxZoomInDistance} to {startY + maxZoomOutDistance})");
            Camera.transform.position = camBeforeZoom;
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
