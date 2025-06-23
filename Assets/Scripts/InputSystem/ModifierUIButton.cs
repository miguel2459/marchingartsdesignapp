// ModifierUIButton.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class ModifierUIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ModifierType { Shift, Control }
    public ModifierType modifierKey = ModifierType.Shift;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (modifierKey == ModifierType.Shift)
            MobileModifierKeyProxy.SetShiftHeld(true);
        else if (modifierKey == ModifierType.Control)
            MobileModifierKeyProxy.SetControlHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (modifierKey == ModifierType.Shift)
            MobileModifierKeyProxy.SetShiftHeld(false);
        else if (modifierKey == ModifierType.Control)
            MobileModifierKeyProxy.SetControlHeld(false);
    }
}