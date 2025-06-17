using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ResponsivePanelHeight : MonoBehaviour
{
    private RectTransform rt;

    [Header("📐 Interpolation Range (height / width for landscape only)")]
    [Tooltip("Aspect ratios below this = minRatio, above this = maxRatio")]
    public float minAspect = 0.4f;   // 5K Ultra-wide
    public float maxAspect = 1.0f;   // Standard desktop & landscape phones

    [Header("📏 Height Ratios (%)")]
    [Range(0.05f, 0.4f)] public float minRatio = 0.10f; // At minAspect
    [Range(0.05f, 0.4f)] public float maxRatio = 0.22f; // At maxAspect

    [Header("🔒 Optional Pixel Clamp")]
    public float minHeight = 150f;
    public float maxHeight = 600f;

    [Header("🧪 Debug Readout")]
    [SerializeField] private float currentAspect;
    [SerializeField] private float currentRatio;
    [SerializeField] private float currentHeight;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (rt == null) return;

        currentAspect = Screen.height / (float)Screen.width;

        if (currentAspect > 1.0f)
        {
#if UNITY_EDITOR
            Debug.Log($"⛔ Portrait mode — skipping landscape panel adjustment (aspect {currentAspect:F2})");
#endif
            return;
        }

        currentRatio = EvaluateHeightRatioFromAspect(currentAspect);
        currentHeight = Mathf.Clamp(Screen.height * currentRatio, minHeight, maxHeight);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

#if UNITY_EDITOR
       // Debug.Log($"🖥️ Aspect {currentAspect:F2} → Ratio {currentRatio:F2} → Height {currentHeight}px");
#endif
    }

    float EvaluateHeightRatioFromAspect(float aspect)
    {
        float t = Mathf.InverseLerp(minAspect, maxAspect, Mathf.Clamp(aspect, minAspect, maxAspect));
        return Mathf.Lerp(minRatio, maxRatio, t);
    }
}
