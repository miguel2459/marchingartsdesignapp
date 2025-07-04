using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public abstract class DashedPathVisualizerBase : MonoBehaviour
{
    protected LineRenderer lineRenderer;
    
    // Dynamic color override - if set, this takes precedence over the default colors
    private Color? dynamicColor = null;
    
    // MaterialPropertyBlock for per-renderer properties
    private MaterialPropertyBlock propertyBlock;
    
    // Shader property IDs (cached for performance)
    private static readonly int MainTexPropertyId = Shader.PropertyToID("_MainTex_ST");

    protected virtual void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        propertyBlock = new MaterialPropertyBlock();
    }

    public void SetPath(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null) return;

        Vector3 flatStart = FlattenY(start);
        Vector3 flatEnd = FlattenY(end);

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, flatStart);
        lineRenderer.SetPosition(1, flatEnd);

        ApplyTiling(flatStart, flatEnd);
        ApplyColor();
    }

    public void Hide()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Sets a dynamic color that overrides the default visualizer color
    /// </summary>
    /// <param name="color">Color to use, or null to revert to default</param>
    public void SetDynamicColor(Color? color)
    {
        dynamicColor = color;
        // If line is currently visible, update the color immediately
        if (lineRenderer != null && lineRenderer.enabled)
        {
            ApplyColor();
        }
    }

    protected virtual Vector3 FlattenY(Vector3 v) => new Vector3(v.x, 0.01f, v.z);

    protected virtual void ApplyColor()
    {
        if (dynamicColor.HasValue)
        {
            // Use dynamic color if set
            lineRenderer.startColor = dynamicColor.Value;
            lineRenderer.endColor = dynamicColor.Value;
        }
        else
        {
            // Fall back to default color implementation
            ApplyDefaultColor();
        }
    }

    // Each visualizer implements its default color
    protected abstract void ApplyDefaultColor();
    protected virtual void ApplyTiling(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float tilingX = GetTilingValue(distance);
        
        // Use MaterialPropertyBlock instead of modifying the shared material
        propertyBlock.SetVector(MainTexPropertyId, new Vector4(tilingX, 1f, 0f, 0f));
        lineRenderer.SetPropertyBlock(propertyBlock);
    }
    
    /// <summary>
    /// Each visualizer implements its own tiling calculation
    /// </summary>
    protected abstract float GetTilingValue(float distance);
}