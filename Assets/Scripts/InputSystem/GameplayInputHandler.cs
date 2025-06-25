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
        TouchInputHandler.OnTouchPan += HandleTouchPan;
        TouchInputHandler.OnTouchZoom += HandleTouchZoom;
        TouchInputHandler.OnTouchRotate += HandleTouchRotate;

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
        TouchInputHandler.OnTouchPan -= HandleTouchPan;
        TouchInputHandler.OnTouchZoom -= HandleTouchZoom;
        TouchInputHandler.OnTouchRotate -= HandleTouchRotate;

    }
    private void HandleTouchPan(Vector2 delta)
    {
        // Only send to camera if no modifier gesture is blocking
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            cameraModeManager.HandlePanIntent(delta);
        }
    }
    private void HandleTouchZoom(float delta)
    {
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            cameraModeManager.HandleZoomIntent(delta);
        }
    }
    
    private void HandleTouchRotate(float angleDelta)
    {
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            // Convert angle to Vector2 delta for existing handler
            Vector2 rotateDelta = new Vector2(angleDelta, 0);
            cameraModeManager.HandleRotateIntent(rotateDelta);
        }
    }

}