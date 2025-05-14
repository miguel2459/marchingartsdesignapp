using UnityEngine;

/// <summary>
/// Centralized input router for AAA-style input context management.
/// Used to gate world input when UI is interacted with.
/// Automatically resets flags each frame.
/// </summary>
public static class InputRouter
{
    /// <summary>
    /// Set to true by UI components to block world interaction for this frame.
    /// </summary>
    private static int frameLastBlocked = -1;
    public static bool BlockSceneInputThisFrame => frameLastBlocked == Time.frameCount;

    /// <summary>
    /// Called by UI elements when clicked to block world input.
    /// </summary>
    public static void FlagUIInteracted()
    {
        frameLastBlocked = Time.frameCount;
    }
}
