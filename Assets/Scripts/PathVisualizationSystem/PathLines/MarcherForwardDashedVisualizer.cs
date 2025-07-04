using UnityEngine;

public class MarcherForwardDashedVisualizer : DashedPathVisualizerBase
{
    [SerializeField] private Color forwardColor = Color.white;

    protected override void ApplyDefaultColor()
    {
        lineRenderer.startColor = forwardColor;
        lineRenderer.endColor = forwardColor;
    }

    protected override float GetTilingValue(float distance)
    {
        return 3f; // Fixed tiling value as requested
    }
}