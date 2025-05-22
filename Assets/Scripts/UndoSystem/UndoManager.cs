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
    [SerializeField] private EnsembleDirector2 director;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.U))
        {
            if (positionHistory.CanUndo)
            {
                Debug.Log("↩️ Undoing last position change");

                // 🧠 Cache current selection
                List<GameObject> cachedSelection = selectedMarchers?.GetSelectionCopy();

                // 🧹 Clear selection and gizmo BEFORE undo
                selectedMarchers?.ClearSelection();
                if (transformGizmoManager.IsGizmoActive())
                {
                    transformGizmoManager.HideTransformGizmo();
                    Debug.Log("🔧 Gizmo hidden after undo.");
                }

                // 🔄 Perform Undo
                positionHistory.Undo();

                // ✅ Restore selection AFTER undo
                if (cachedSelection != null)
                {
                    foreach (var m in cachedSelection)
                    {
                        selectedMarchers.Select(m);

                        // ✅ Register selection as baseline for dashed preview
                        selectedMarchers?.dashedPathPreviewManager?.RegisterSelectedMarchers(cachedSelection);

                        selectedMarchers.ReCacheAnchorsForSelected();
                        director?.RefreshDashedPreviewForSelected();
                    }

                    // 🔁 Delay anchors + dashed paths until next frame
                    EnsembleDirector2.instance.StartCoroutine(DelayedRefreshDashedPreview());
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
    
    private System.Collections.IEnumerator DelayedRefreshDashedPreview()
    {
        yield return null; // Wait one frame
        selectedMarchers.ReCacheAnchorsForSelected();
        selectedMarchers.dashedPathPreviewManager?.UpdatePreviewCycle();
    }
}
