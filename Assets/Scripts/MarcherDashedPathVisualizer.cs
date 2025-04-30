using UnityEngine;

/// <summary>
/// Handles rendering a dynamic dashed preview path for marcher edits.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MarcherDashedPathVisualizer : MonoBehaviour
{
    private LineRenderer dashedLineRenderer;
    [SerializeField] private Vector3? previousConfirmedPosition = null;
    [SerializeField] private Vector3? nextConfirmedPosition = null;

    [SerializeField] private Material dashedMaterial;

    private bool movedAwayFromAnchor = false;
    private const float movementAwayThreshold = 0.2f;  // Minimum distance to count as "moved away"
    private const float returnThreshold = 0.05f;        // Threshold to count as "returned"
    private Vector3 lastUpdatePosition;
    private bool isActive = false;
    [SerializeField] private MarcherAnchorDashedVisualizer anchorVisualizer;


    private void Awake()
    {
        dashedLineRenderer = GetComponent<LineRenderer>();
        dashedLineRenderer.enabled = false;

        if (dashedMaterial != null)
        {
            dashedLineRenderer.material = dashedMaterial;
            Debug.Log($"✅ Dashed material assigned to {gameObject.name}");
        }
        else
        {
            Debug.LogError($"❌ Dashed material is MISSING on {gameObject.name}!");
        }

        dashedLineRenderer.widthMultiplier = 0.05f;
        dashedLineRenderer.positionCount = 0;
        dashedLineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Update the dashed preview path while moving.
    /// </summary>
    public void UpdatePreviewAnchors(Vector3 currentPosition)
    {
        if (!previousConfirmedPosition.HasValue)
            return;

        float movedDistance = Vector3.Distance(lastUpdatePosition, currentPosition);

        if (movedDistance < 0.01f) // 🚫 No meaningful move ➔ skip updates
            return;

        lastUpdatePosition = currentPosition; 

        Vector3[] points;

        if (nextConfirmedPosition.HasValue)
        {
            points = new Vector3[]
            {
                FlattenY(previousConfirmedPosition.Value),
                FlattenY(currentPosition),
                FlattenY(nextConfirmedPosition.Value)
            };

            dashedLineRenderer.positionCount = 3;
        }
        else
        {
            points = new Vector3[]
            {
                FlattenY(previousConfirmedPosition.Value),
                FlattenY(currentPosition)
            };

            dashedLineRenderer.positionCount = 2;
        }

        dashedLineRenderer.SetPositions(points);

        AdjustTilingBasedOnPath(points);
        UpdateColorBasedOnStepSize(currentPosition);

        if (anchorVisualizer != null)
        {
            anchorVisualizer.UpdateAnchorLine(currentPosition);
        }
    }

    /// <summary>
    /// Hide the dashed preview path.
    /// </summary>
    public void StopPreview()
    {
        isActive = false;
        dashedLineRenderer.enabled = false;
        movedAwayFromAnchor = false;
        dashedLineRenderer.positionCount = 0;

        // 🧠 Hide the anchor dashed line too
        if (anchorVisualizer != null)
            anchorVisualizer.Hide();
    }


    /// <summary>
    /// Determine color based on step size.
    /// </summary>
    private void UpdateColorBasedOnStepSize(Vector3 currentPosition)
    {
        if (!previousConfirmedPosition.HasValue)
            return;

        float distance = Vector3.Distance(previousConfirmedPosition.Value, currentPosition);

        // Get interpolated color from StepSizeCalculator (to be implemented separately)
        Color previewColor = StepSizeCalculator.GetStepSizeColor(distance);
        Debug.Log($"🎨 [ColorUpdate] {gameObject.name} Distance: {distance} ➔ Color: {previewColor}");

        dashedLineRenderer.startColor = previewColor;
        dashedLineRenderer.endColor = previewColor;
    }
    public static class StepSizeCalculator
    {
        public static Color GetStepSizeColor(float distance)
        {
            if (distance < 2.25f) // less than 8/5 step (~22.5 inches)
                return new Color(0f, 0.81f, 1f, 1f); // BLUE (normal)
            if (distance < 3.0f)
                return Color.green;
            if (distance < 4.0f)
                return Color.yellow;
            return Color.red;
        }
    }

    /// <summary>
    /// Helper to flatten points onto field plane.
    /// </summary>
    private Vector3 FlattenY(Vector3 original)
    {
        return new Vector3(original.x, 0.01f, original.z);
    }


    public void CacheAnchors(Vector3 previousPos, Vector3? nextPos = null, Vector3? activeConfirmedPos = null)
    {
        previousConfirmedPosition = previousPos;
        nextConfirmedPosition = nextPos;

        if (anchorVisualizer != null)
            anchorVisualizer.CacheActiveConfirmedPosition(activeConfirmedPos);

        Debug.Log("Dash positions cached with active confirmed pos (if any)");
    }



    // StartPreview becomes SUPER simple now:
    public void StartPreview()
    {
        isActive = true;

        if (dashedLineRenderer != null)
        {
            dashedLineRenderer.enabled = true;
            dashedLineRenderer.gameObject.SetActive(true);
            lastUpdatePosition = transform.position;
            UpdatePreviewAnchors(gameObject.transform.position);

            // 🧠 Sync the anchor line immediately
            if (anchorVisualizer != null)
            {
                anchorVisualizer.UpdateAnchorLine(transform.position);
            }

            Debug.Log($"{gameObject.name} 🚨 Dashed LineRenderer is turned on and Preview should be visible!");
        }
        else
        {
            Debug.LogError($"{gameObject.name} 🚨 Dashed LineRenderer is NULL in StartPreview!");
        }
    }


    private void AdjustTilingBasedOnPath(Vector3[] points)
    {
        if (dashedLineRenderer == null || dashedLineRenderer.material == null)
            return;

        float totalLength = 0f;

        for (int i = 0; i < points.Length - 1; i++)
        {
            totalLength += Vector3.Distance(points[i], points[i + 1]);
        }

        float tilingX = Mathf.Max(3f, totalLength * 1.5f); // 📏 Ratio: 1.5 tiles per unit length
        dashedLineRenderer.material.mainTextureScale = new Vector2(tilingX, 1f);

        Debug.Log($"🧵 [DashedPreview] Adjusted tiling: {tilingX} for path length {totalLength}");
    }

}