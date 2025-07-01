using UnityEngine;

public static class GizmoModeExtensions
{
    public static Sprite ToIcon(this GizmoMode mode, TransformGizmoUIButtonManager ui)
    {
        return mode switch
        {
            GizmoMode.Position => ui.positionIcon,
            GizmoMode.Rotate => ui.rotateIcon,
            GizmoMode.Scale => ui.scaleIcon,
            _ => null
        };
    }
}