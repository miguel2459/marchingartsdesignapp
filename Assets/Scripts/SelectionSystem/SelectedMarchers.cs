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
    [SerializeField] public InputGestureManager inputGestureManager;

    // 🎥 Camera & Interaction
    public Camera cam;
    public ICameraFocusHandler cameraFocusHandler;

    [Header("Selection Settings")]
    public LayerMask marcherLayer;

    // 🧠 Selection Logic
    private MarcherSelectionManager selectionManager;
    public int SelectedCount => selectionManager.SelectedMarchers.Count;

    private void Start()
    {
        selectionManager = new MarcherSelectionManager(director, dashedPathPreviewManager);
        marcherEditActions.SetSelectionManager(selectionManager);
        inputGestureManager.OnConfirmDot += marcherEditActions.ConfirmDotViaSpacebar;
        inputGestureManager.OnDeleteDot += marcherEditActions.DeleteConfirmedDot;
        inputGestureManager.OnDeleteMarcher += marcherEditActions.PromptDeleteMarchers;
        inputGestureManager.OnRespaceSmart += SnapAndRespaceSmartReviewed;
        inputGestureManager.OnSelectAll += SelectAllMarchers;
        inputGestureManager.OnSnapToGrid += () =>
        {
            MarcherSnapper.SnapSelectionToGrid(selectionManager, transformGizmoManager.snapToGrid);
        };
        inputGestureManager.OnFocusCamera += () =>
        {
            cameraModeManager?.UpdateCameraFocus();
        };
    }
    private void Update()
    {
        // if (SelectedCount > 0 && transformGizmoManager != null && transformGizmoManager.isMoving)
        // {
        //     Debug.Log($"[Update] SelectedCount={SelectedCount}, IsMoving={transformGizmoManager?.isMoving}");

        // }
        if (SelectedCount > 0 && transformGizmoManager != null && transformGizmoManager.isMoving)
        {
            Debug.Log($"[Update] SelectedCount={SelectedCount}, IsMoving={transformGizmoManager?.isMoving}");
            dashedPathPreviewManager?.UpdatePreviewCycle();
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

    public void ClearSelection()
    {
        selectionManager.ClearAndResetVisuals(); // includes ClearAll()
        transformGizmoManager?.HideTransformGizmo();
        transformGizmoManager.isMoving = false;

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