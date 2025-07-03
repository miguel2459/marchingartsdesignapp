using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class CameraTunerUIBuilder : EditorWindow
{
    private GameObject panelRoot;
    private GameObject rowPrefab;
    private FlyingCameraController flyingCam;
    private TopDownCameraController topDownCam;
    private ScrollAndPinch scrollAndPinch;

    [MenuItem("Tools/Mobile Camera Tuner Builder")]
    public static void ShowWindow()
    {
        GetWindow<CameraTunerUIBuilder>("Camera Tuner Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("⚙️ Camera Tuner UI Builder", EditorStyles.boldLabel);

        panelRoot = (GameObject)EditorGUILayout.ObjectField("Panel Root (VerticalLayoutGroup)", panelRoot, typeof(GameObject), true);
        rowPrefab = (GameObject)EditorGUILayout.ObjectField("Input Row Prefab", rowPrefab, typeof(GameObject), false);
        flyingCam = (FlyingCameraController)EditorGUILayout.ObjectField("FlyingCameraController", flyingCam, typeof(FlyingCameraController), true);
        topDownCam = (TopDownCameraController)EditorGUILayout.ObjectField("TopDownCameraController", topDownCam, typeof(TopDownCameraController), true);
        scrollAndPinch = (ScrollAndPinch)EditorGUILayout.ObjectField("ScrollAndPinch", scrollAndPinch, typeof(ScrollAndPinch), true);

        if (GUILayout.Button("📦 Generate Input Fields"))
        {
            if (panelRoot == null || rowPrefab == null)
            {
                Debug.LogError("Missing required references.");
                return;
            }

            GenerateFields();
        }
    }

    private void GenerateFields()
    {
        Undo.RegisterFullObjectHierarchyUndo(panelRoot, "Generate Camera Tuner Fields");

        // ✈️ Flying Camera (Desktop)
        AddField("FlyingCam: rotationSpeed", flyingCam.rotationSpeed.ToString("F2"));
        AddField("FlyingCam: zoomSpeed", flyingCam.zoomSpeed.ToString("F2"));
        AddField("FlyingCam: zoomMultiplier", flyingCam.zoomMultiplier.ToString("F2"));
        AddField("FlyingCam: panSpeed", flyingCam.panSpeed.ToString("F2"));

        // 🛰 TopDown Camera (Desktop)
        AddField("TopDownCam: zoomSpeed", topDownCam.zoomSpeed.ToString("F2"));
        AddField("TopDownCam: zoomMultiplier", topDownCam.zoomMultiplier.ToString("F2"));
        AddField("TopDownCam: panSpeed", topDownCam.panSpeed.ToString("F2"));

        // 🤏 Scroll & Pinch (Touch Controls)
        AddField("Touch: Pan Speed", scrollAndPinch.touchPanSpeed.ToString("F2"));
        AddField("Touch: Zoom Sensitivity", scrollAndPinch.touchZoomSensitivity.ToString("F2"));
        
        Debug.Log("✅ Camera tuning fields generated in UI panel.");
    }

    private void AddField(string labelText, string defaultValue)
    {
        GameObject newRow = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, panelRoot.transform);
        newRow.name = labelText.Replace(":", "").Replace(" ", "");

        TMP_Text label = newRow.GetComponentInChildren<TMP_Text>();
        TMP_InputField input = newRow.GetComponentInChildren<TMP_InputField>();

        if (label != null) label.text = labelText;
        if (input != null) input.text = defaultValue;
    }
}
