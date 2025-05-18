using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized manager for global undo/redo hotkeys using MarcherPositionHistory.
/// Attach to a persistent scene object.
/// </summary>
public class UndoManager : MonoBehaviour
{
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private TransformGizmoManager transformGizmoManager;
    [SerializeField] private SelectedMarchers selectedMarchers;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.U))
        {
            if (positionHistory.CanUndo)
            {
                // 🧠 1. Cache current selection
                List<GameObject> cachedSelection = selectedMarchers?.GetSelectionCopy();

                // 🧹 2. Clear selection to prevent dashed line redraw
                selectedMarchers?.ClearSelection();

                Debug.Log("↩️ Undoing last position change");
                positionHistory.Undo();

                // 🔁 3. Re-select previous marchers after undo
                if (cachedSelection != null)
                {
                    foreach (var m in cachedSelection)
                    {
                        selectedMarchers.Select(m);
                    }
                    Debug.Log("✅ Re-selected cached marchers after undo.");

                    // 🔁 Fix dashed lines after undo
                    selectedMarchers.ReCacheAnchorsForSelected();
                }

                // 🛠️ 4. Optionally hide gizmo (if you're keeping this behavior)
                if (transformGizmoManager.IsGizmoActive())
                {
                    transformGizmoManager.HideTransformGizmo();
                    Debug.Log("🔧 Gizmo hidden after undo.");
                }
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
