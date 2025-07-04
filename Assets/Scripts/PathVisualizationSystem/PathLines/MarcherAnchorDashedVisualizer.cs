using UnityEngine;

public class MarcherAnchorDashedVisualizer : DashedPathVisualizerBase
{
    [SerializeField] private Color anchorColor = new Color(0f, 0.2f, 0f, 1f);
    [SerializeField] private float lineWidth = 0.1f; // Thinner line width

    protected override void Awake()
    {
        base.Awake();
        
        // Set the line width for the anchor line
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }
    }

    protected override void ApplyDefaultColor()
    {
        lineRenderer.startColor = anchorColor;
        lineRenderer.endColor = anchorColor;
    }

    protected override float GetTilingValue(float distance)
    {
        return 0.6f; // Fixed tiling value as requested
    }
}