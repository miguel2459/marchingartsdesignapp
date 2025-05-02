using UnityEngine;

/// <summary>
/// Rotates label to face camera differently depending on camera mode.
/// </summary>
public class MarcherLabelBillboard : MonoBehaviour
{
    private Camera activeCam;
    private bool isTopDown = false;

    void Update()
    {
        if (activeCam == null) return;

        if (isTopDown)
        {
            // Lock to clean, static global top-down rotation
            transform.rotation = Quaternion.Euler(90f, 90f, 0f);
        }
        else
        {
            // Use global camera Y rotation for uniform label facing
            float camY = activeCam.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(90f, camY, 0f);
        }
    }

    public void SetCamera(Camera cam)
    {
        activeCam = cam;
    }

    public void SetMode(bool useTopDown)
    {
        isTopDown = useTopDown;
    }
}
