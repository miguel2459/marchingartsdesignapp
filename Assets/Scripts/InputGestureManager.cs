using UnityEngine;
using System;

public class InputGestureManager : MonoBehaviour
{
    [System.Serializable]
    public struct GestureConfig
    {
        public float dragThreshold;
    }
    [Header("Gesture Settings")]
    public GestureConfig config = new GestureConfig { dragThreshold = 10f };
    public static InputGestureManager Instance;
    private Vector2 mouseDownPos;
    private Vector2 mouseUpPos;
    private bool isMouseDown = false;
    public Vector2 DragStartPos => mouseDownPos;
    public Vector2 DragEndPos => mouseUpPos;

    public enum GestureType { None, Click, Drag }
    public GestureType CurrentGesture { get; private set; } = GestureType.None;

    public event Action<GestureType> OnGestureDetected = delegate { };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        DetectGesture();
    }

    private void DetectGesture()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPos = Input.mousePosition;
            isMouseDown = true;
            CurrentGesture = GestureType.None;
        }

        if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            mouseUpPos = Input.mousePosition;
            float distance = Vector2.Distance(mouseUpPos, mouseDownPos);

            if (distance > config.dragThreshold)
            {
                CurrentGesture = GestureType.Drag;
            }
            else
            {
                CurrentGesture = GestureType.Click;
            }

            isMouseDown = false;
            OnGestureDetected?.Invoke(CurrentGesture);
        }
    }

    public bool IsShiftHeld() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool IsCtrlHeld() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
}
