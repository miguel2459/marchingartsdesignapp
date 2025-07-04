using UnityEngine;

public class MarcherBackwardDashedVisualizer : DashedPathVisualizerBase
{
    [SerializeField] private Color backwardColor = new Color(1f, 0.5f, 0.2f, 1f);

    protected override void ApplyDefaultColor()
    {
        lineRenderer.startColor = backwardColor;
        lineRenderer.endColor = backwardColor;
    }

    protected override float GetTilingValue(float distance)
    {
        return 3f; // Fixed tiling value as requested
    }
}