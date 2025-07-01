using TMPro;
using UnityEngine;

public class GizmoDebugPanelManager : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public TransformGizmoManager gizmoManager;

    void Update()
    {
        if (debugText == null || gizmoManager == null || !gizmoManager.HasActiveGizmo)
        {
            debugText.text = "Gizmo: Inactive";
            return;
        }

        var gizmo = gizmoManager.transformGizmo.GetComponent<UnifiedGizmoBehavior>();
        if (gizmo == null)
        {
            debugText.text = "Gizmo: Missing Behavior";
            return;
        }

        string info = "🧭 Gizmo Debug Panel\n";

        info += $" Mode: {gizmo.GetCurrentMode()}\n";
        info += $" Dragging: {gizmo.IsDragging()}\n";
        info += $" Reanchoring (Shift): {(gizmoManager.IsFreeDraggingGizmo ? "Yes" : "No")}  |  Held: {(Input.GetKey(KeyCode.LeftShift) ? "Yes" : "No")}\n";
        info += $" Active Axis: {gizmo.GetActiveAxis()}\n";
        info += $" Snap (Q): {(Input.GetKey(KeyCode.Q) ? "Enabled" : "Disabled")}\n";

        debugText.text = info;
    }
}