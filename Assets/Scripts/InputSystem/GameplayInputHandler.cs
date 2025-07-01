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
        InputRouter.OnShiftHeldChanged += HandleShiftKey;
        InputRouter.OnControlHeldChanged += HandleControlKey;
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
        InputRouter.OnShiftHeldChanged -= HandleShiftKey;
        InputRouter.OnControlHeldChanged -= HandleControlKey;
    }
    private void HandleTouchPan(Vector2 delta)
    {
        // Only send to camera if no modifier gesture is blocking
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            float mobilePanMultiplier = 4f; // Adjust this value for preferred sensitivity
            Vector2 adjustedDelta = delta * mobilePanMultiplier;
            cameraModeManager.HandlePanIntent(adjustedDelta);
        }
    }

    private void HandleTouchZoom(float delta)
    {
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            float mobileZoomScale = 0.1f; // Smaller values for smooth control
            float adjustedZoom = delta * mobileZoomScale;
            cameraModeManager.HandleZoomIntent(adjustedZoom);
        }
    }

    private void HandleTouchRotate(float angleDelta)
    {
        if (cameraModeManager != null && !UIInteractionBlocker.IsTouchOverUI())
        {
            float rotateSensitivity = 1f; // Keep at 1f unless you want to dampen twist
            Vector2 rotateDelta = new Vector2(angleDelta * rotateSensitivity, 0);
            cameraModeManager.HandleRotateIntent(rotateDelta);
        }
    }
    
    private void HandleShiftKey(bool held)
    {
        if (held)
            MobileModifierKeyProxy.SetShiftHeld(true);
        else if (MobileModifierKeyProxy.IsShiftHeld)
            MobileModifierKeyProxy.SetShiftHeld(false);
    }

    private void HandleControlKey(bool held)
    {
        if (held)
            MobileModifierKeyProxy.SetControlHeld(true);
        else if (MobileModifierKeyProxy.IsControlHeld)
            MobileModifierKeyProxy.SetControlHeld(false);
    }
}