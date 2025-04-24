using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to any UI Button, Panel, or interactive element.
/// On pointer down, it will flag that UI was interacted with.
/// </summary>
public class UIInputFlagSetter : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("🖱️ UI clicked - blocking world input this frame.");
        InputRouter.FlagUIInteracted();
    }
}
