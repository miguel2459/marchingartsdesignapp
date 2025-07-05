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
        pressStartTime = Time.time;
        isPressing = true;
        hasTriggeredHold = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressing) return;
        isPressing = false;

        // 🔒 Just held to lock — do nothing else
        if (hasTriggeredHold)
        {
            Debug.Log("✅ Hold-to-lock completed. No further action.");
            return;
        }

        if (GridAlignProxy.IsGridLockActive)
        {
            // 🔓 Tap to unlock
            Debug.Log("🔓 UI: Unlocking grid lock");
            GridAlignProxy.SetGridLock(false);
        }
        else
        {
            // 🧲 Tap to snap (only if not locked)
            Debug.Log("🧲 UI: Snap to grid (tap)");
            inputRouter.TriggerSnapToGridFromUI();
        }
    }

    private void Update()
    {
        if (isPressing && !hasTriggeredHold && !GridAlignProxy.IsGridLockActive)
        {
            if (Time.time - pressStartTime >= HOLD_THRESHOLD)
            {
                hasTriggeredHold = true;
                Debug.Log("🔒 UI: Activating grid lock via hold");
                GridAlignProxy.SetGridLock(true);
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