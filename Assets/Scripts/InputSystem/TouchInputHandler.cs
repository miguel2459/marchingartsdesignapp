using UnityEngine;
using UnityEngine.InputSystem;

public class TouchInputHandler : MonoBehaviour
{
    private MADAControls controls;

    public static event System.Action<Vector2> OnTouchPan;
    public static event System.Action<float> OnTouchZoom;
    public static event System.Action<int> OnTouchCountChanged;
    public static event System.Action<float> OnTouchRotate;
    public static int CurrentTouchCount { get; private set; }

    private Vector2 lastTouch0;
    private Vector2 lastTouch1;
    private int lastTouchCount = 0;

    private void Awake()
    {
        controls = new MADAControls();
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();

        controls.Gameplay.TouchContact0.performed += ctx => lastTouch0 = ctx.ReadValue<Vector2>();
        controls.Gameplay.TouchContact1.performed += ctx => lastTouch1 = ctx.ReadValue<Vector2>();

        controls.Gameplay.TouchDelta0.performed += ctx =>
        {
            if (lastTouchCount == 1)
            {
                Vector2 delta = ctx.ReadValue<Vector2>();
                OnTouchPan?.Invoke(delta);
            }
        };

        controls.Gameplay.TouchDelta1.performed += ctx =>
        {
            if (lastTouchCount >= 2)
            {
                Vector2 delta0 = controls.Gameplay.TouchDelta0.ReadValue<Vector2>();
                Vector2 delta1 = ctx.ReadValue<Vector2>();

                Vector2 prevVector = lastTouch1 - lastTouch0;
                Vector2 newVector = (lastTouch1 + delta1) - (lastTouch0 + delta0);

                float prevAngle = Mathf.Atan2(prevVector.y, prevVector.x) * Mathf.Rad2Deg;
                float newAngle = Mathf.Atan2(newVector.y, newVector.x) * Mathf.Rad2Deg;
                float angleDelta = Mathf.DeltaAngle(prevAngle, newAngle); // gives signed delta

                // Zoom logic
                float prevDist = prevVector.magnitude;
                float newDist = newVector.magnitude;
                float pinchDelta = newDist - prevDist;

                OnTouchZoom?.Invoke(pinchDelta);

                if (Mathf.Abs(angleDelta) > 0.2f) // threshold for stability
                    OnTouchRotate?.Invoke(angleDelta);
            }
        };

        controls.Gameplay.TouchCount.performed += ctx =>
        {
            int count = ctx.ReadValue<int>();
            lastTouchCount = count;
            OnTouchCountChanged?.Invoke(count);
        };
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
}