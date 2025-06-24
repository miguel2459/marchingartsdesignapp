using UnityEngine;

public class GameplayInputHandler : MonoBehaviour
{
    [SerializeField] private MarcherEditActions marcherEditActions;
    [SerializeField] private CameraModeManager cameraModeManager;
    [SerializeField] private SelectedMarchers selectedMarchers;

    private void OnEnable()
    {
        InputRouter.OnConfirmDot += marcherEditActions.ConfirmDotViaSpacebar;
        InputRouter.OnDeleteDot += marcherEditActions.DeleteConfirmedDot;
        InputRouter.OnDeleteMarcher += marcherEditActions.PromptDeleteMarchers;
        InputRouter.OnFocusCamera += cameraModeManager.UpdateCameraFocus;
        InputRouter.OnSnapToGrid += selectedMarchers.InvokeSnapToGrid;
        InputRouter.OnRespaceToBox += selectedMarchers.SnapAndRespaceSmartReviewed;
        InputRouter.OnSelectAll += selectedMarchers.SelectAllMarchers;
        InputRouter.OnZoom += cameraModeManager.HandleZoomIntent;
        InputRouter.OnPan += cameraModeManager.HandlePanIntent;
        InputRouter.OnRotate += cameraModeManager.HandleRotateIntent;
        InputRouter.OnPivot += cameraModeManager.HandlePivotIntent; // reusing rotate for pivot

    }

    private void OnDisable()
    {
        InputRouter.OnConfirmDot -= marcherEditActions.ConfirmDotViaSpacebar;
        InputRouter.OnDeleteDot -= marcherEditActions.DeleteConfirmedDot;
        InputRouter.OnDeleteMarcher -= marcherEditActions.PromptDeleteMarchers;
        InputRouter.OnFocusCamera -= cameraModeManager.UpdateCameraFocus;
        InputRouter.OnSnapToGrid -= selectedMarchers.InvokeSnapToGrid;
        InputRouter.OnRespaceToBox -= selectedMarchers.SnapAndRespaceSmartReviewed;
        InputRouter.OnSelectAll -= selectedMarchers.SelectAllMarchers;
        InputRouter.OnZoom -= cameraModeManager.HandleZoomIntent;
        InputRouter.OnPan -= cameraModeManager.HandlePanIntent;
        InputRouter.OnRotate -= cameraModeManager.HandleRotateIntent;
        InputRouter.OnPivot -= cameraModeManager.HandlePivotIntent;

    }
}