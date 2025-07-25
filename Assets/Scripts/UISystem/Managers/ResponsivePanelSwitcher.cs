using UnityEngine;

[ExecuteAlways]
public class ResponsivePanelSwitcher : MonoBehaviour
{
    [Tooltip("Panel shown in portrait mode")]
    public GameObject portraitPanel;

    [Tooltip("Panel shown in landscape mode")]
    public GameObject landscapePanel;

    [Tooltip("Threshold: height / width > threshold = portrait")]
    [Range(0.1f, 2f)]
    public float aspectThreshold = 0.75f;

    private bool isPortrait;
    private int lastScreenWidth;
    private int lastScreenHeight;
    public ResponsivePanelSlotReparenter slotReparenter;
    void PanelSwitcherInit()
    {
        UpdateLayout(forceUpdate: true);
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateLayout(forceUpdate: false);
        }
    }

    private void UpdateLayout(bool forceUpdate)
    {
        float aspectRatio = Screen.height / (float)Screen.width;
        bool currentlyPortrait = aspectRatio > aspectThreshold;

        if (!forceUpdate && currentlyPortrait == isPortrait)
            return; // No change

        isPortrait = currentlyPortrait;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (portraitPanel != null)
            portraitPanel.SetActive(isPortrait);

        if (landscapePanel != null)
            landscapePanel.SetActive(!isPortrait);

        Debug.Log($"🔄 UI Layout Switched to: {(isPortrait ? "Portrait" : "Landscape")} | {Screen.width}x{Screen.height} | Aspect: {aspectRatio:F2}");

        // 🧠 New: Trigger slot reparenter now that the active panel is switched
        if (slotReparenter != null)
        {
            slotReparenter.ApplyLayoutFromOutside(debugLog: true);
        }
    }

}