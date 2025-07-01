using UnityEngine;
using UnityEngine.InputSystem;

public static class GizmoInputHandler
{
    private static MADAControls input;

    static GizmoInputHandler()
    {
        input = new MADAControls();
        input.Enable();
    }

    public static bool IsPositionKeyPressed()
    {
        return input.Gameplay.GizmoPosition.WasPressedThisFrame();
    }

    public static bool IsRotateKeyPressed()
    {
        return input.Gameplay.GizmoRotate.WasPressedThisFrame();
    }

    public static bool IsScaleKeyPressed()
    {
        return input.Gameplay.GizmoScale.WasPressedThisFrame();
    }
    
    public static float GetMouseDeltaX()
    {
        return Mouse.current != null ? Mouse.current.delta.x.ReadValue() : 0f;
    }

}