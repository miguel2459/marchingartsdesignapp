// ModifierUIButton.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModifierUIButton : MonoBehaviour, IPointerClickHandler
{
    public enum ModifierType { Shift, Control }
    public ModifierType modifierKey = ModifierType.Shift;

    [Header("Optional Visuals")]
    public Image iconImage;
    public Color activeColor = Color.yellow;
    public Color inactiveColor = Color.white;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (modifierKey == ModifierType.Shift)
        {
            MobileModifierKeyProxy.ToggleShift();
        }
        else if (modifierKey == ModifierType.Control)
        {
            MobileModifierKeyProxy.ToggleControl();
        }
        // No direct call to UpdateVisual here — OnModifierStateChanged handles it
    }

    private void UpdateVisual(bool isActive)
    {
        if (iconImage != null)
        {
            iconImage.color = isActive ? activeColor : inactiveColor;
        }
    }

    private void Start()
    {
        // Ensure correct initial state
        bool isActive = modifierKey == ModifierType.Shift
            ? MobileModifierKeyProxy.IsShiftHeld
            : MobileModifierKeyProxy.IsControlHeld;

        UpdateVisual(isActive);
    }
    private void OnEnable()
    {
        MobileModifierKeyProxy.OnModifierStateChanged += RefreshVisual;
    }

    private void OnDisable()
    {
        MobileModifierKeyProxy.OnModifierStateChanged -= RefreshVisual;
    }

    private void RefreshVisual()
    {
        bool isActive = modifierKey == ModifierType.Shift
            ? MobileModifierKeyProxy.IsShiftHeld
            : MobileModifierKeyProxy.IsControlHeld;

        UpdateVisual(isActive);
    }

}