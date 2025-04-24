using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MarcherPathVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;

        // Optional: Set up default styling
        lineRenderer.widthMultiplier = 0.05f; // ✅ Skinny trail
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 1f, 1f, 0.3f); // light white
        lineRenderer.endColor = new Color(1f, 1f, 1f, 0.8f);   // bright white
        lineRenderer.positionCount = 0;
    }

    public void ShowPath(Vector3[] positions)
    {
        if (positions == null || positions.Length < 2)
        {
            HidePath();
            return;
        }

        Vector3[] flattened = new Vector3[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            flattened[i] = new Vector3(positions[i].x, 0.01f, positions[i].z); // 💥 Push onto field
        }

        lineRenderer.positionCount = flattened.Length;
        lineRenderer.SetPositions(flattened);
        lineRenderer.enabled = true;
    }

    public void SetColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }


    public void HidePath()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }
}
