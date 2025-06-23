// MobileModifierKeyProxy.cs
using UnityEngine;

public static class MobileModifierKeyProxy
{
    private static bool shiftHeld = false;
    private static bool controlHeld = false;

    public static bool IsShiftHeld => shiftHeld;
    public static bool IsControlHeld => controlHeld;

    public static void SetShiftHeld(bool value) => shiftHeld = value;
    public static void SetControlHeld(bool value) => controlHeld = value;
}