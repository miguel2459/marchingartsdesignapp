using System;
using UnityEngine;

public static class MobileModifierKeyProxy
{
    private static bool shiftHeld = false;
    private static bool controlHeld = false;
    private static bool altHeld = false;

    public static bool IsShiftHeld => shiftHeld;
    public static bool IsControlHeld => controlHeld;
    public static bool IsAltHeld => altHeld;

    public static event Action? OnModifierStateChanged;

    // Timing logic
    private static float altUnstickTime = -1f;
    private const float ALT_UNSTICK_DELAY_SECONDS = 0.05f;

    private static float altRecentlyHeldUntil = -1f;
    private const float ALT_RECENT_GRACE_SECONDS = 0.1f;

    /// <summary>
    /// Used in selection logic to check if Alt is currently or very recently held.
    /// </summary>
    public static bool WasAltRecentlyHeld => altHeld || Time.time < altRecentlyHeldUntil;

    // === Shift and Control ===
    public static void ToggleShift()
    {
        shiftHeld = !shiftHeld;
        if (shiftHeld) controlHeld = false;
        OnModifierStateChanged?.Invoke();
    }

    public static void ToggleControl()
    {
        controlHeld = !controlHeld;
        if (controlHeld) shiftHeld = false;
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

    // === Alt ===
    public static void ToggleAlt()
    {
        if (!altHeld)
        {
            SetAltHeld(true);
        }
        else
        {
            ForceAltRelease();
        }
    }

    public static void SetAltHeld(bool value)
    {
        if (value && !altHeld)
        {
            altHeld = true;
            altUnstickTime = -1f;
            OnModifierStateChanged?.Invoke();
        }

        // NOTE: setting altHeld = false directly is disallowed; use ForceAltRelease
    }

    public static void ForceAltRelease(bool delayed = false)
    {
        if (!altHeld) return;

        if (delayed)
        {
            altUnstickTime = Time.time + ALT_UNSTICK_DELAY_SECONDS;
        }
        else
        {
            altHeld = false;
            altUnstickTime = -1f;
            altRecentlyHeldUntil = Time.time + ALT_RECENT_GRACE_SECONDS;
            OnModifierStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Call this every frame from GameplayInputHandler to check if delayed Alt release should occur.
    /// </summary>
    public static void TickTime()
    {
        if (altHeld && altUnstickTime > 0f && Time.time >= altUnstickTime && !Input.GetMouseButton(0))
        {
            altHeld = false;
            altUnstickTime = -1f;
            altRecentlyHeldUntil = Time.time + ALT_RECENT_GRACE_SECONDS;
            OnModifierStateChanged?.Invoke();
            Debug.Log("⏱️ Alt released after delay.");
        }
    }
}

public static class ModifierInput
{
    public static bool ShiftHeld => MobileModifierKeyProxy.IsShiftHeld;
    public static bool ControlHeld => MobileModifierKeyProxy.IsControlHeld;
    public static bool NoModifier => !ShiftHeld && !ControlHeld;
    public static bool AltHeld => MobileModifierKeyProxy.WasAltRecentlyHeld;
}
