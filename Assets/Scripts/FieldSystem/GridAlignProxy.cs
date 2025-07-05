using System;
using UnityEngine;

public static class GridAlignProxy
{
    private static bool isGridLockActive = false;
    public static bool IsGridLockActive => isGridLockActive;

    public static event Action OnGridLockStateChanged;

    public static void ToggleGridLockWithNotify()
    {
        isGridLockActive = !isGridLockActive;
        Debug.Log($"🔄 Grid lock toggled via keyboard: {isGridLockActive}");
        OnGridLockStateChanged?.Invoke();
    }

    public static void SetGridLock(bool value)
    {
        isGridLockActive = value;
        OnGridLockStateChanged?.Invoke();
    }
}