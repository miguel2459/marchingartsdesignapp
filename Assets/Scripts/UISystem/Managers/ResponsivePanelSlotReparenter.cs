using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class ResponsivePanelSlotReparenter : MonoBehaviour
{
    [Header("🌐 Mirror Containers")]
    public RectTransform landscapeRoot;
    public RectTransform portraitRoot;

    [Tooltip("Threshold: height / width > threshold = portrait")]
    [Range(0.1f, 2f)]
    public float aspectThreshold = 0.75f;

    [System.Serializable]
    public class ReparentEntry
    {
        public RectTransform element;
        public string targetSlotName; // Must exist in both landscape & portrait
    }

    [Header("🎯 Elements to Reparent")]
    public List<ReparentEntry> elements = new List<ReparentEntry>();

    private bool isPortrait;
    private float lastAspect;

    void OnEnable()
    {
        ApplyLayout(force: true);
    }

    public void ApplyLayoutFromOutside(bool debugLog = false)
    {
        ApplyLayout(force: debugLog);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyLayout(force: true);
    }
#endif

    private bool HasOrientationChanged()
    {
        float current = Screen.height / (float)Screen.width;
        bool nowPortrait = current > aspectThreshold;

        if (Mathf.Abs(current - lastAspect) > 0.01f || nowPortrait != isPortrait)
        {
            lastAspect = current;
            isPortrait = nowPortrait;
            return true;
        }
        return false;
    }

    private void ApplyLayout(bool force)
    {
        if (landscapeRoot == null || portraitRoot == null) return;

        float aspectRatio = Screen.height / (float)Screen.width;
        isPortrait = aspectRatio > aspectThreshold;

        RectTransform root = isPortrait ? portraitRoot : landscapeRoot;

        foreach (var entry in elements)
        {
            if (entry.element == null || string.IsNullOrEmpty(entry.targetSlotName)) continue;

            Transform slot = FindChildRecursive(root, entry.targetSlotName);

            if (slot == null)
            {
                Debug.LogWarning($"❌ Slot '{entry.targetSlotName}' not found under {root.name}");
                continue;
            }

            if (entry.element.parent != slot)
            {
                entry.element.SetParent(slot, false);
#if UNITY_EDITOR
                if (force)
#endif
                    Debug.Log($"📦 Reparented '{entry.element.name}' → {slot.name} under {root.name}");
            }
        }
    }
    
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

}
