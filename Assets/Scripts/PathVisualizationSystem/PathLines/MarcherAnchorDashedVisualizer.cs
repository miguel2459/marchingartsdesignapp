using UnityEngine;

/// <summary>
/// Render-only anchor dashed line between current position and anchor (confirmed) dot.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MarcherAnchorDashedVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            Debug.LogError($"[AnchorDashedVisualizer] {gameObject.name} is missing LineRenderer!");
            return;
        }

        lineRenderer.enabled = false;
    }

    /// <summary>
    /// Show the anchor line from marcher to confirmed dot.
    /// </summary>
    public void SetPath(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null) return;

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;

        Vector3[] positions = new Vector3[]
        {
            FlattenY(start),
            FlattenY(end)
        };

        lineRenderer.SetPositions(positions);
        SetColor(); // Uses fixed color for anchors
    }

    /// <summary>
    /// Hide the anchor dashed line.
    /// </summary>
    public void Hide()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Set fixed visual color for anchor line.
    /// </summary>
    private void SetColor()
    {
        if (lineRenderer == null) return;

        Color anchorColor = new Color(0.2f, 0.4f, 0.2f, 1f); // dark earthy green
        lineRenderer.startColor = anchorColor;
        lineRenderer.endColor = anchorColor;
    }

    private Vector3 FlattenY(Vector3 original)
    {
        return new Vector3(original.x, 0.01f, original.z);
    }
}
