using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Centralized input router for AAA-style input context management.
/// Used to gate world input when UI is interacted with.
/// Automatically resets flags each frame.
/// </summary>
public static class UIInteractionBlocker
{
    /// <summary>
    /// Set to true by UI components to block world interaction for this frame.
    /// </summary>
    private static float blockInputUntilTime = -1f;
    private const float BlockDuration = 0.1f; // 100ms

    public static bool BlockSceneInputThisFrame => Time.unscaledTime < blockInputUntilTime;

    public static void FlagUIInteracted()
    {
        blockInputUntilTime = Time.unscaledTime + BlockDuration;
        Debug.Log($"🧱 BlockSceneInput active until: {blockInputUntilTime:F3}");
    }

    public static bool IsTouchOverUI()
    {
        if (Input.touchCount > 0)
        {
            return EventSystem.current != null &&
                   EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        return false;
    }
}
