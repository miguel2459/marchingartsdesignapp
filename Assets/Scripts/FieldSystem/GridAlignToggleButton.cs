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

        if (hasTriggeredHold)
        {
            // ✅ Hold toggles grid lock (ON or OFF)
            bool newLockState = !GridAlignProxy.IsGridLockActive;
            GridAlignProxy.SetGridLock(newLockState);
            Debug.Log(newLockState ? "🔒 Grid lock enabled (via hold)" : "🔓 Grid lock disabled (via hold)");
            return;
        }

        // ✅ Always snap to grid on tap
        Debug.Log("🧲 UI: Snap to grid (tap)");
        inputRouter.TriggerSnapToGridFromUI();
    }


    private void Update()
    {
        if (isPressing && !hasTriggeredHold)
        {
            if (Time.time - pressStartTime >= HOLD_THRESHOLD)
            {
                hasTriggeredHold = true;
                // 👇 No direct lock-on here — we toggle in OnPointerUp
                Debug.Log("⏳ Hold duration met. Will toggle lock on release.");
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