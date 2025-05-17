using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverCameraBlocker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CameraModeManager cameraModeManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        cameraModeManager.BlockInput(true);
        // Debug.Log("🛑 Camera input blocked.");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cameraModeManager.BlockInput(false);
        // Debug.Log("✅ Camera input unblocked.");
    }
}
