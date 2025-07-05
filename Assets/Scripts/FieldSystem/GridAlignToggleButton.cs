using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridAlignToggleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image iconImage;
    public Sprite snapIcon;
    public Sprite lockIcon;
    public InputRouter inputRouter;

    private float pressStartTime;
    private bool isPressing = false;
    private bool hasTriggeredHold = false;
    private const float HOLD_THRESHOLD = 0.3f;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GridAlignProxy.IsGridLockActive)
        {
            // Already locked — clicking disables immediately
            Debug.Log("🔓 Unlocking Grid Lock");
            GridAlignProxy.SetGridLock(false);
            inputRouter.TriggerLockToGridFromUI();
            return;
        }

        pressStartTime = Time.time;
        isPressing = true;
        hasTriggeredHold = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressing) return;

        if (!hasTriggeredHold)
        {
            // Regular tap: snap only
            Debug.Log("🧲 Snap to grid (tap)");
            inputRouter.TriggerSnapToGridFromUI();
        }

        isPressing = false;
    }

    private void Update()
    {
        if (isPressing && !hasTriggeredHold)
        {
            if (Time.time - pressStartTime >= HOLD_THRESHOLD)
            {
                hasTriggeredHold = true;
                Debug.Log("🔒 Activated Grid Lock (hold)");
                GridAlignProxy.SetGridLock(true);
                inputRouter.TriggerLockToGridFromUI();
            }
        }
    }

    private void OnEnable()
    {
        GridAlignProxy.OnGridLockStateChanged += RefreshVisual;
        RefreshVisual();
    }

    private void OnDisable()
    {
        GridAlignProxy.OnGridLockStateChanged -= RefreshVisual;
    }

    private void RefreshVisual()
    {
        if (iconImage != null)
        {
            iconImage.sprite = GridAlignProxy.IsGridLockActive ? lockIcon : snapIcon;
        }
    }
}
