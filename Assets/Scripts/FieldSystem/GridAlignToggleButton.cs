using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
public class GridAlignToggleButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image iconImage;
    public Sprite snapIcon;
    public Sprite lockIcon;
    public InputRouter inputRouter;

    private float pressStartTime;
    private bool isPressing = false;
    private bool hasToggledLock = false;
    private const float HOLD_THRESHOLD = 0.3f;
    
    [Header("Flash Feedback Icons")]
    public Image flashIcon; // Separate icon that flashes briefly
    public Sprite flashSnapIcon;
    public Sprite flashLockIcon;
    public Sprite flashUnlockIcon;
    private Coroutine flashRoutine;
    public void OnPointerDown(PointerEventData eventData)
    {
        pressStartTime = Time.time;
        isPressing = true;
        hasToggledLock = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressing) return;
        isPressing = false;

        if (hasToggledLock)
        {
            // 🛑 Skip snap if this interaction was a lock toggle
            Debug.Log("⏳ Grid lock toggled during hold — skipping snap.");
            return;
        }

        // ✅ Otherwise: Snap to grid
        Debug.Log("🧲 UI: Snap to grid (on PointerUp)");
        FlashFeedback(flashSnapIcon);
        inputRouter.TriggerSnapToGridFromUI();
    }


    private void Update()
    {
        if (isPressing && !hasToggledLock && (Time.time - pressStartTime >= HOLD_THRESHOLD))
        {
            hasToggledLock = true;
            bool newLockState = !GridAlignProxy.IsGridLockActive;
            GridAlignProxy.SetGridLock(newLockState);
            Debug.Log(newLockState ? "🔒 Grid lock enabled (on hold)" : "🔓 Grid lock disabled (on hold)");
            // 🔁 Flash icon
            FlashFeedback(newLockState ? flashLockIcon : flashUnlockIcon);
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
    
    private void FlashFeedback(Sprite iconToShow)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(iconToShow));
    }

    private IEnumerator FlashRoutine(Sprite iconToShow)
    {
        if (flashIcon == null || iconToShow == null)
            yield break;

        flashIcon.sprite = iconToShow;
        flashIcon.color = new Color(1f, 1f, 1f, 1f); // fully visible
        flashIcon.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.4f); // Flash duration

        flashIcon.color = new Color(1f, 1f, 1f, 0f); // fade out
        flashIcon.gameObject.SetActive(false);
    }
}
