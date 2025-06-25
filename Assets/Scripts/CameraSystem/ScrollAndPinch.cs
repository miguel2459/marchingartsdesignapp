/*
Original Script By: Ditzel
Youtube Video Link: https://www.youtube.com/watch?v=KkYco_7-ULA&ab_channel=DitzelGames
Uses of this script:
 For controlling camera movement: Pan, zoom and rotate
How to use:
Set this on an empty game object positioned at (0,0,0) and attach your active camera.
The script only runs on mobile devices or the remote app.
DecreaseCameraPanSpeed - The more the value, the greater it slows the pan speed
Upper height (Zoom out restriction) - Relative to camera position (not worldspace)
Lower height (Zoom in restriction) - Relative to camera position (not worldspace)
------[CHANGE LOG]------
Edited by Kudoshi : 24/3/2021
- Added Zoom In Out restriction
- Added camera pan speed
*/

using UnityEngine;

public class ScrollAndPinch : MonoBehaviour
{
    public Camera Camera;
    public bool Rotate;
    protected Plane Plane;
    public float DecreaseCameraPanSpeed = 1; //Default speed is 1
    public float CameraUpperHeightBound; //Zoom out
    public float CameraLowerHeightBound; //Zoom in

    private Vector3 cameraStartPosition;
    private void Awake()
    {
        if (Camera == null)
            Camera = Camera.main;

        cameraStartPosition = Camera.transform.position;
    }

    private void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            //Plane.SetNormalAndPosition(transform.up, transform.position);
            //Plane.SetNormalAndPosition(Vector3.up, new Vector3(0, 0, 0));
            //Plane.SetNormalAndPosition(Vector3.up, Camera.transform.position);
            Plane.SetNormalAndPosition(Vector3.up, new Vector3(0, 0.1f, 0)); // Slightly above ground plane


            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            Vector3 pos0 = PlanePosition(touch0.position);
            Vector3 pos1 = PlanePosition(touch1.position);
            Vector3 pos0Prev = PlanePosition(touch0PrevPos);
            Vector3 pos1Prev = PlanePosition(touch1PrevPos);

            // === Pan ===
            Vector3 mid = (pos0 + pos1) * 0.5f;
            Vector3 midPrev = (pos0Prev + pos1Prev) * 0.5f;
            Vector3 panDelta = (midPrev - mid) / DecreaseCameraPanSpeed;
            Camera.transform.Translate(panDelta, Space.World);

            // === Zoom ===
            float prevTouchDeltaMag = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
            float currentTouchDeltaMag = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = currentTouchDeltaMag - prevTouchDeltaMag;

            Vector3 camBeforeZoom = Camera.transform.position;

            // Zoom along camera's forward vector, but only affect vertical distance (Y)
            Camera.transform.position += Camera.transform.forward * (deltaMagnitudeDiff * 0.01f);

            // Clamp to vertical bounds
            float y = Camera.transform.position.y;
            float baseY = cameraStartPosition.y;
            if (y > baseY + CameraUpperHeightBound || y < baseY - CameraLowerHeightBound || y <= 1f)
            {
                Camera.transform.position = camBeforeZoom;
            }

            // Debug (optional)
            Debug.Log($"🔍 Zoom delta: {deltaMagnitudeDiff}, CamY: {y:F2}");


            // === Rotate ===
            if (Rotate && pos1Prev != pos1)
            {
                float angle = Vector3.SignedAngle(pos1 - pos0, pos1Prev - pos0Prev, Plane.normal);
                Camera.transform.RotateAround(mid, Plane.normal, angle);
            }
        }
    }


    //Returns the point between first and final finger position
    protected Vector3 PlanePositionDelta(Touch touch)
    {
        //not moved
        if (touch.phase != TouchPhase.Moved)
            return Vector3.zero;

        
        //delta
        var rayBefore = Camera.ScreenPointToRay(touch.position - touch.deltaPosition);
        var rayNow = Camera.ScreenPointToRay(touch.position);
        if (Plane.Raycast(rayBefore, out var enterBefore) && Plane.Raycast(rayNow, out var enterNow))
            return rayBefore.GetPoint(enterBefore) - rayNow.GetPoint(enterNow);

        //not on plane
        return Vector3.zero;
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
        Plane.SetNormalAndPosition(Vector3.up, Vector3.zero); // Match how Plane is set in Update
        return PlanePosition(screenPos);
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + transform.up);
    }
}