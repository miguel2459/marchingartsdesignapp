using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

[ExecuteInEditMode]
public class UIAuditTool : MonoBehaviour
{
    public string[] canvasNames = {
        "Selector Canvas",
        "Naming Identity UI Canvas",
        "Ensemble UI Canvas",
        "DeleteMarchers Canvas"
    };

    [ContextMenu("Audit UI Canvases")]
    public void AuditUI()
    {
        Debug.Log("🔍 Starting UI Audit...\n");

        foreach (string name in canvasNames)
        {
            GameObject canvasGO = GameObject.Find(name);
            if (canvasGO == null)
            {
                Debug.LogWarning($"❌ Canvas '{name}' not found in scene.");
                continue;
            }

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"⚠️ GameObject '{name}' exists but has no Canvas component.");
                continue;
            }

            var report = new StringBuilder();
            report.AppendLine($"🖼️ Canvas: {name}");
            report.AppendLine($"   - Render Mode: {canvas.renderMode}");
            report.AppendLine($"   - Pixel Perfect: {canvas.pixelPerfect}");
            report.AppendLine($"   - Sorting Layer: {canvas.sortingLayerName} (Order: {canvas.sortingOrder})");

            RectTransform rootRect = canvas.GetComponent<RectTransform>();
            report.AppendLine($"   - Root Rect: {rootRect.rect.width}x{rootRect.rect.height}");

            LayoutGroup lg = canvas.GetComponent<LayoutGroup>();
            if (lg != null)
            {
                report.AppendLine($"   - LayoutGroup: {lg.GetType().Name}");
            }

            report.AppendLine("📦 Children:");
            AuditChildren(canvasGO.transform, report, "   ");

            Debug.Log(report.ToString());
        }

        Debug.Log("✅ UI Audit complete.");
    }

    private void AuditChildren(Transform parent, StringBuilder report, string indent)
    {
        foreach (Transform child in parent)
        {
            RectTransform rt = child.GetComponent<RectTransform>();
            if (rt == null) continue;

            string line = $"{indent}- {child.name}";

            if (child.TryGetComponent(out LayoutGroup lg))
                line += $" | Layout: {lg.GetType().Name}";

            if (child.TryGetComponent(out ContentSizeFitter csf))
                line += $" | Fitter: {csf.horizontalFit}/{csf.verticalFit}";

            if (child.TryGetComponent(out LayoutElement le))
                line += $" | LayoutElem: min({le.minWidth},{le.minHeight}) pref({le.preferredWidth},{le.preferredHeight})";

            line += $" | Anchors: {rt.anchorMin} to {rt.anchorMax} | Pos: {rt.anchoredPosition} | SizeDelta: {rt.sizeDelta}";
            report.AppendLine(line);

            if (child.childCount > 0)
                AuditChildren(child, report, indent + "   ");
        }
    }
}
