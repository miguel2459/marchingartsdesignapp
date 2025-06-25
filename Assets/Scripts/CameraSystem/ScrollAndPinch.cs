using UnityEngine;

public class ScrollAndPinch : MonoBehaviour
{
    public enum ZoomMode { Forward, Vertical }
    public ZoomMode zoomMode = ZoomMode.Forward;

    public Camera Camera;
    public bool Rotate;
    protected Plane Plane;

    public float DecreaseCameraPanSpeed = 1f;

    [Header("Zoom Clamping")]
    public float CameraUpperHeightBound = 30f;
    public float CameraLowerHeightBound = 5f;
    public float zoomSensitivity = 10f;

    private Vector3 cameraStartPosition;

    private void Awake()
    {
        if (Camera == null)
            Camera = Camera.main;

        cameraStartPosition = Camera.transform.position;
    }

    private void Update()
    {
        // 🎯 Touch Pinch Input
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Plane.SetNormalAndPosition(Vector3.up, new Vector3(0, 0.1f, 0));

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            Debug.Log($"🖐 Touch0 pos: {touch0.position}, delta: {touch0.deltaPosition}");
            Debug.Log($"🖐 Touch1 pos: {touch1.position}, delta: {touch1.deltaPosition}");

            Vector3 pos0 = PlanePosition(touch0.position);
            Vector3 pos1 = PlanePosition(touch1.position);
            Vector3 pos0Prev = PlanePosition(touch0PrevPos);
            Vector3 pos1Prev = PlanePosition(touch1PrevPos);

            // === Pan ===
            Vector3 mid = (pos0 + pos1) * 0.5f;
            Vector3 midPrev = (pos0Prev + pos1Prev) * 0.5f;
            Vector3 panDelta = (midPrev - mid) / DecreaseCameraPanSpeed;
            Camera.transform.Translate(panDelta, Space.World);
            Debug.Log($"📦 Pan applied: {panDelta}");

            // === Zoom ===
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentTouchDeltaMag = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = currentTouchDeltaMag - prevTouchDeltaMag;

            ApplyZoom(deltaMagnitudeDiff * zoomSensitivity * Time.deltaTime);

            // === Rotate ===
            if (Rotate && pos1Prev != pos1)
            {
                float angle = Vector3.SignedAngle(pos1 - pos0, pos1Prev - pos0Prev, Plane.normal);
                Camera.transform.RotateAround(mid, Plane.normal, angle);
                Debug.Log($"🔄 Rotate angle: {angle:F2} degrees");
            }
        }

#if UNITY_EDITOR
        // 🧪 Desktop scroll wheel test support
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Debug.Log($"🖱 ScrollWheel Zoom: {scroll}");
            ApplyZoom(scroll * zoomSensitivity * 100f * Time.deltaTime);
        }
#endif
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (zoomMode == ZoomMode.Vertical && Camera.orthographic)
        {
            float orthoSize = Camera.orthographicSize;
            orthoSize -= zoomAmount;
            orthoSize = Mathf.Clamp(orthoSize, CameraLowerHeightBound, CameraUpperHeightBound);
            Camera.orthographicSize = orthoSize;
            Debug.Log($"🔍 Ortho Zoom adjusted: {orthoSize:F2}");
        }
        else
        {
            Vector3 camBeforeZoom = Camera.transform.position;
            Vector3 zoomDirection = Camera.transform.forward;
            Camera.transform.position += zoomDirection * zoomAmount;

            float y = Camera.transform.position.y;
            float baseY = cameraStartPosition.y;

            float camDistanceToCenter = Vector3.Distance(Camera.transform.position, new Vector3(26.25f, y, 60f));
            float startDistance = Vector3.Distance(cameraStartPosition, new Vector3(26.25f, cameraStartPosition.y, 60f));

            if (y > baseY + CameraUpperHeightBound || y < baseY - CameraLowerHeightBound || camDistanceToCenter > startDistance + CameraUpperHeightBound || camDistanceToCenter < startDistance - CameraLowerHeightBound)
            {
                Camera.transform.position = camBeforeZoom;
                Debug.LogWarning($"⛔ Zoom clamped: Y={y:F2}, Distance={camDistanceToCenter:F2}");
            }
            else
            {
                Debug.Log($"🔭 Perspective Zoom: Position = {Camera.transform.position}, Distance = {camDistanceToCenter:F2}");
            }
        }
    }

    protected Vector3 PlanePosition(Vector2 screenPos)
    {
        var rayNow = Camera.ScreenPointToRay(screenPos);
        if (Plane.Raycast(rayNow, out var enterNow))
        {
            Vector3 hitPoint = rayNow.GetPoint(enterNow);
            Debug.Log($"✅ Ray hit at: {hitPoint}");
            return hitPoint;
        }

        Debug.LogWarning($"❌ Ray did not hit plane at screenPos: {screenPos}");
        return Vector3.zero;
    }

    public Vector3 DebugPlanePosition(Vector2 screenPos)
    {
        Plane.SetNormalAndPosition(Vector3.up, Vector3.zero);
        return PlanePosition(screenPos);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + transform.up);
    }
}
