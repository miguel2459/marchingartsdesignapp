using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PlayModeExitHandler
{
    static PlayModeExitHandler()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ClearSetProgressBarButtons();
        }
    }

    private static void ClearSetProgressBarButtons()
    {
        SetProgressBar setProgressBar = Object.FindObjectOfType<SetProgressBar>();
        if (setProgressBar != null)
        {
            //setProgressBar.ClearButtons();
        }
    }
}
