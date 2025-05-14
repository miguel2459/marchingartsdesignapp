using UnityEngine;

/// <summary>
/// Centralized manager for global undo/redo hotkeys using MarcherPositionHistory.
/// Attach to a persistent scene object.
/// </summary>
public class UndoManager : MonoBehaviour
{
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private TransformGizmoManager transformGizmoManager;


    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.U))
        {
            if (positionHistory.CanUndo)
            {
                Debug.Log("↩️ Undoing last position change");
                positionHistory.Undo();
            }
            if (transformGizmoManager.IsGizmoActive())
            {
                transformGizmoManager.HideTransformGizmo();
                Debug.Log("🔧 Gizmo hidden after undo.");
            }
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.U))
        {
            if (positionHistory.CanRedo)
            {
                Debug.Log("↪️ Redoing last undone position change");
                positionHistory.Redo();
            }
            
            if (transformGizmoManager.IsGizmoActive())
            {
                transformGizmoManager.HideTransformGizmo();
                Debug.Log("🔧 Gizmo hidden after redo.");
            }
        }

    }
}
