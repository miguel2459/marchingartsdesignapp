using System;

public static class MobileModifierKeyProxy
{
    private static bool shiftHeld = false;
    private static bool controlHeld = false;

    public static bool IsShiftHeld => shiftHeld;
    public static bool IsControlHeld => controlHeld;
    public static event Action? OnModifierStateChanged;

    public static void ToggleShift()
    {
        if (!shiftHeld)
        {
            shiftHeld = true;
            controlHeld = false;
        }
        else
        {
            shiftHeld = false;
        }
        OnModifierStateChanged?.Invoke();
    }

    public static void ToggleControl()
    {
        if (!controlHeld)
        {
            controlHeld = true;
            shiftHeld = false;
        }
        else
        {
            controlHeld = false;
        }
        OnModifierStateChanged?.Invoke();
    }

    public static void SetShiftHeld(bool value)
    {
        shiftHeld = value;
        if (value) controlHeld = false;
        OnModifierStateChanged?.Invoke();
    }

    public static void SetControlHeld(bool value)
    {
        controlHeld = value;
        if (value) shiftHeld = false;
        OnModifierStateChanged?.Invoke();
    }
}

public static class ModifierInput
{
    public static bool ShiftHeld => MobileModifierKeyProxy.IsShiftHeld;
    public static bool ControlHeld => MobileModifierKeyProxy.IsControlHeld;
    public static bool NoModifier => !ShiftHeld && !ControlHeld;
}

