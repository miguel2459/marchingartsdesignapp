using UnityEngine;
using System;

public class InputRouter : MonoBehaviour
{
    private MADAControls controls;

    public static event Action OnConfirmDot;
    public static event Action OnDeleteDot;
    public static event Action OnDeleteMarcher;
    public static event Action OnSelectAll;
    public static event Action OnSnapToGrid;
    public static event Action OnRespaceToBox;
    public static event Action OnFocusCamera;
    public static event Action<float> OnZoom;
    public static event Action<Vector2> OnPan;
    public static event Action<Vector2> OnRotate;
    public static event Action<Vector2> OnPivot;

    private void Awake()
    {
        controls = new MADAControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        controls.Gameplay.ConfirmDot.performed += ctx => OnConfirmDot?.Invoke();
        controls.Gameplay.DeleteDot.performed += ctx => OnDeleteDot?.Invoke();
        controls.Gameplay.DeleteMarcher.performed += ctx => OnDeleteMarcher?.Invoke();
        controls.Gameplay.SelectAll.performed += ctx => OnSelectAll?.Invoke();
        controls.Gameplay.SnapToGrid.performed += ctx => OnSnapToGrid?.Invoke();
        controls.Gameplay.RespaceToBox.performed += ctx => OnRespaceToBox?.Invoke();
        controls.Gameplay.FocusCamera.performed += ctx => OnFocusCamera?.Invoke();
        controls.Gameplay.CameraZoomScroll.performed += ctx =>
        {
            var control = ctx.control.name;
            float zoomDelta = control == "up" ? 1f :
                control == "down" ? -1f : 0f;
            OnZoom?.Invoke(zoomDelta);
        };

        controls.Gameplay.CameraPan.performed += ctx =>
        {
            Vector2 delta = ctx.ReadValue<Vector2>();
            OnPan?.Invoke(delta);
        };

        controls.Gameplay.CameraRotate.performed += ctx =>
        {
            Vector2 delta = ctx.ReadValue<Vector2>();
            OnRotate?.Invoke(delta);
        };

        controls.Gameplay.CameraPivot.performed += ctx =>
        {
            Vector2 delta = ctx.ReadValue<Vector2>();
            OnPivot?.Invoke(delta);
        };
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
    
    public void TriggerConfirmDotFromUI()
    {
        OnConfirmDot?.Invoke(); // behaves like pressing Spacebar
    }
    
    public void TriggerDeleteDotFromUI()
    {
        OnDeleteDot?.Invoke(); // behaves like pressing Backspace
    }
}