using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ResponsivePanelHeightPortrait : MonoBehaviour
{
    private RectTransform rt;

    [Header("📐 Interpolation Range (height / width for portrait only)")]
    [Tooltip("Aspect ratios below this = minRatio, above this = maxRatio")]
    public float minAspect = 1.0f;   // Smallest valid portrait (wide phone in portrait)
    public float maxAspect = 2.2f;   // Ultra-tall phones

    [Header("📏 Height Ratios (%)")]
    [Range(0.05f, 1.0f)] public float minRatio = 0.28f; // At minAspect
    [Range(0.05f, 1.0f)] public float maxRatio = 0.40f; // At maxAspect

    [Header("🔒 Optional Pixel Clamp")]
    public float minHeight = 200f;
    public float maxHeight = 1000f;

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

        if (currentAspect <= 1.0f)
        {
#if UNITY_EDITOR
            //Debug.Log($"⛔ Landscape mode — skipping portrait panel adjustment (aspect {currentAspect:F2})");
#endif
            return;
        }

        currentRatio = EvaluateHeightRatioFromAspect(currentAspect);
        currentHeight = Mathf.Clamp(Screen.height * currentRatio, minHeight, maxHeight);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

#if UNITY_EDITOR
        Debug.Log($"📱 Aspect {currentAspect:F2} → Ratio {currentRatio:F2} → Height {currentHeight}px");
#endif
    }

    float EvaluateHeightRatioFromAspect(float aspect)
    {
        float t = Mathf.InverseLerp(minAspect, maxAspect, Mathf.Clamp(aspect, minAspect, maxAspect));
        return Mathf.Lerp(minRatio, maxRatio, t);
    }
}