using TMPro;
using UnityEngine;

public class DebugPanelManager : MonoBehaviour
{
    public TextMeshProUGUI debugText;
    public ScrollAndPinch scrollAndPinch;
    public GameObject debugPanelRoot;
    [SerializeField] private bool isVisible = true;

    void Update()
    {
        if (!isVisible || debugPanelRoot == null)
        {
            if (debugText != null) debugText.text = "";
            return;
        }
        
        
        if (scrollAndPinch == null || debugText == null) return;

        string debugInfo = $" Touch Count: {Input.touchCount}\n";

        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            Vector2 t0Prev = t0.position - t0.deltaPosition;
            Vector2 t1Prev = t1.position - t1.deltaPosition;

            Vector3 pos0 = scrollAndPinch.DebugPlanePosition(t0.position);
            Vector3 pos1 = scrollAndPinch.DebugPlanePosition(t1.position);
            Vector3 pos0Prev = scrollAndPinch.DebugPlanePosition(t0Prev);
            Vector3 pos1Prev = scrollAndPinch.DebugPlanePosition(t1Prev);

            float prevDist = Vector3.Distance(pos0Prev, pos1Prev);
            float currDist = Vector3.Distance(pos0, pos1);
            float zoomFactor = prevDist > 0 ? currDist / prevDist : 1f;

            debugInfo += $" pos0: {pos0}\n";
            debugInfo += $" pos1: {pos1}\n";
            debugInfo += $" Zoom Factor: {zoomFactor:F3}\n";
        }

        if (scrollAndPinch != null && scrollAndPinch.Camera != null)
        {
            debugInfo += $" Camera Y: {scrollAndPinch.Camera.transform.position.y:F2}";
        }
        else
        {
            debugInfo += " Camera Y: (not available)";
        }

        debugText.text = debugInfo;
    }
}