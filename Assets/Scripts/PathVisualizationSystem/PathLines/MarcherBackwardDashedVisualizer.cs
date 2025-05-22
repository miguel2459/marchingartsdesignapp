using UnityEngine;

/// <summary>
/// Render-only backward dashed line from current position to previous confirmed.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MarcherBackwardDashedVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// Set and show the dashed line from current to previous confirmed.
    /// </summary>
    public void SetPath(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Flatten(start));
        lineRenderer.SetPosition(1, Flatten(end));
        AdjustTilingBasedOnDistance(start, end);
    }

    /// <summary>
    /// Set the line color (based on step size or style).
    /// </summary>
    public void SetColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    /// <summary>
    /// Hide the dashed line.
    /// </summary>
    public void Hide()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Dynamically adjust texture tiling based on distance.
    /// </summary>
    private void AdjustTilingBasedOnDistance(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null || lineRenderer.material == null)
            return;

        float distance = Vector3.Distance(Flatten(start), Flatten(end));
        float tilingX = Mathf.Max(3f, distance * 2f); // slightly denser than forward
        lineRenderer.material.mainTextureScale = new Vector2(tilingX, 1f);
    }

    /// <summary>
    /// Keeps the dashed line pinned to the ground.
    /// </summary>
    private Vector3 Flatten(Vector3 pos)
    {
        return new Vector3(pos.x, 0.01f, pos.z);
    }
}
