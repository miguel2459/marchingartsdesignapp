using UnityEngine;

public class MarcherAnchorDashedVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public LineRenderer Renderer => lineRenderer;
    private Vector3? activeConfirmedPosition = null;
    private Vector3 lastUpdatePosition;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError($"[AnchorDashedVisualizer] {gameObject.name} is missing LineRenderer component!");
        }

        lineRenderer.enabled = false;
    }

    /// <summary>
    /// Cache the active confirmed position for live updating (optional).
    /// </summary>
    public void CacheActiveConfirmedPosition(Vector3? confirmedPos)
    {
        activeConfirmedPosition = confirmedPos;
    }

    /// <summary>
    /// Directly set the anchor dashed line between current and active confirmed position.
    /// </summary>
    public void SetAnchorPath(Vector3 start, Vector3 end)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;

        Vector3[] positions = new Vector3[]
        {
            FlattenY(start),
            FlattenY(end)
        };

        lineRenderer.SetPositions(positions);
        SetAnchorLineColor();
    }

    /// <summary>
    /// Updates the anchor line live if marcher moves (slow update mode).
    /// </summary>
    public void UpdateAnchorLine(Vector3 currentMarcherPosition)
    {
        if (!activeConfirmedPosition.HasValue)
        {
            lineRenderer.enabled = false;
            return;
        }

        float movedDistance = Vector3.Distance(lastUpdatePosition, currentMarcherPosition);

        if (movedDistance < 0.01f)
            return;

        lastUpdatePosition = currentMarcherPosition;

        SetAnchorPath(currentMarcherPosition, activeConfirmedPosition.Value);
    }

    private void SetAnchorLineColor()
    {
        if (lineRenderer == null)
            return;

        // Earthy dark green
        Color darkEarthGreen = new Color(0.2f, 0.4f, 0.2f, 1f); 

        lineRenderer.startColor = darkEarthGreen;
        lineRenderer.endColor = darkEarthGreen;
    }


    /// <summary>
    /// Hide the anchor dashed line.
    /// </summary>
    public void Hide()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }
    }

    private Vector3 FlattenY(Vector3 original)
    {
        return new Vector3(original.x, 0.01f, original.z);
    }
}
