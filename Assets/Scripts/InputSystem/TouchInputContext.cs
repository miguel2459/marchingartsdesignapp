using UnityEngine;
public static class TouchInputContext
{
    public static bool IsCameraGestureActive = false;
    public static float GestureStartTime = 0f;

    public static bool IsRecentGesture => (Time.time - GestureStartTime) < 0.15f;
}