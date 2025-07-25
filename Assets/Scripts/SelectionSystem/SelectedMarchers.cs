using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;


/// <summary>
/// Handles user selection of marchers and confirms set positions when spacebar is pressed.
/// </summary>
public class SelectedMarchers : MonoBehaviour
{
    // 🎯 Core References
    [SerializeField] public EnsembleDirector2 director;
    [SerializeField] public TransformGizmoManager transformGizmoManager;
    [SerializeField] public ShapeMarchers shapeMarchers; // Assign in Inspector
    [SerializeField] public DashedPathPreviewManager dashedPathPreviewManager; // Assign in Inspector
    [SerializeField] public CameraModeManager cameraModeManager;
    [SerializeField] public MarcherEditActions marcherEditActions;
    public static bool IsInitialized { get; private set; } = false;

    // 🎥 Camera & Interaction
    public Camera cam;
    public ICameraFocusHandler cameraFocusHandler;

    [Header("Selection Settings")]
    public LayerMask marcherLayer;

    // 🧠 Selection Logic
    private MarcherSelectionManager selectionManager;
    public int SelectedCount => selectionManager != null ? selectionManager.SelectedMarchers.Count : 0;
    public static SelectedMarchers instance;

    private void Awake()
    {
        instance = this;
    }

    public void SelectedMarchersInit()
    {
        selectionManager = new MarcherSelectionManager(director, dashedPathPreviewManager);
        marcherEditActions.SetSelectionManager(selectionManager);
        IsInitialized = true; // ✅ signal ready
        Debug.Log("✅ SelectedMarchers initialized.");

    }
    private void Update()
    {
        if (!IsInitialized || selectionManager == null) return;
        if (SelectedCount > 0 && transformGizmoManager != null && transformGizmoManager.IsGizmoMoving)
        {
            //Debug.Log($"[Update] SelectedCount={SelectedCount}, IsMoving={transformGizmoManager?.isMoving}");
            dashedPathPreviewManager?.UpdatePreviewCycle(forceRefresh: true);
        }        
    }

    
    public List<GameObject> GetSelectionCopy()
    {
        return selectionManager.GetSelectionCopy();
    }

    public void ForEachSelected(System.Action<GameObject> action)
    {
        selectionManager.ForEachSelected(action);
    }

    public bool IsSelected(GameObject marcher)
    {
        return selectionManager.SelectedMarchers.Contains(marcher);
    }
    public void Select(GameObject marcher)
    {
        selectionManager.Select(marcher);
    }

    public void Deselect(GameObject marcher)
    {
        selectionManager.Deselect(marcher);
    }
    
    public void ReCacheAnchorsForSelected()
    {
        selectionManager.ReCacheAnchorsForSelected();
    }

    public void SelectAllMarchers()
    {
        selectionManager.SelectAll(director.MarcherObjects);
    }
    
    public void InvokeSnapToGrid()
    {
        var snapToGrid = this.transformGizmoManager.snapToGrid;

        // ✅ Temporarily unparent marchers if gizmo is active
        if (transformGizmoManager.HasActiveGizmo)
        {
            ForEachSelected(m => m.transform.SetParent(null)); // unparent
        }

        // ✅ Snap selected marchers in world space
        MarcherSnapper.SnapSelectionToGrid(this.selectionManager, snapToGrid);

        // ✅ Snap the gizmo (after marchers are free)
        if (transformGizmoManager.HasActiveGizmo)
        {
            Transform gizmoTransform = transformGizmoManager.transformGizmo.transform;
            Vector3 snapped = snapToGrid.GetSnappedGizmoPosition(gizmoTransform.position);
            gizmoTransform.position = snapped;
            Debug.Log($"🧲 Gizmo snapped to grid at {snapped}");

            // ✅ Reparent marchers under the gizmo again
            ForEachSelected(m => m.transform.SetParent(gizmoTransform));
        }
    }


    public void ClearSelection()
    {
        selectionManager.ClearAndResetVisuals(); // includes ClearAll()

        Debug.Log("SelectedMarchers: 🧹 Selection cleared and marchers recolored to progress state.");
    }

    public void SnapAndRespaceSmartReviewed()
    {
        Vector3 center = SmartReshapeService.GetFocalPoint(selectionManager.SelectedMarchers);
        SmartReshapeService.ApplyBoxAssignment(
            GetSelectionCopy(),
            shapeMarchers,
            transformGizmoManager,
            transformGizmoManager.snapToGrid,
            center
        );
    }
}