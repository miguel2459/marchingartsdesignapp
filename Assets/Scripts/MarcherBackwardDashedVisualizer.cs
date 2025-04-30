using UnityEngine;

/// <summary>
/// Handles rendering a dashed line from the marcher to the previous confirmed position during editing.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MarcherBackwardDashedVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public LineRenderer Renderer => lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    public void SetBackwardPath(Vector3 start, Vector3 end)
    {
        if (start == null || end == null)
        {
            Hide();
            return;
        }

        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Flatten(start));
        lineRenderer.SetPosition(1, Flatten(end));

        AdjustTilingBasedOnDistance(start, end); // 💥 Add this line
    }

    private void AdjustTilingBasedOnDistance(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null || lineRenderer.material == null)
            return;

        float distance = Vector3.Distance(Flatten(start), Flatten(end));
        float tilingX = Mathf.Max(3f, distance * 2f);
        lineRenderer.material.mainTextureScale = new Vector2(tilingX, 1f);
    }

    public void Hide()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private Vector3 Flatten(Vector3 pos)
    {
        return new Vector3(pos.x, 0.01f, pos.z);
    }
}
