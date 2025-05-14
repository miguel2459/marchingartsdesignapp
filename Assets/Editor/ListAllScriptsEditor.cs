using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class ListAllScriptsEditor : EditorWindow
{
    private List<string> scriptPaths = new List<string>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Script Organizer/List & Export Scripts")]
    public static void ShowWindow()
    {
        GetWindow<ListAllScriptsEditor>("Script Exporter");
    }

    private void OnEnable()
    {
        RefreshScriptList();
    }

    private void OnGUI()
    {
        GUILayout.Label("📜 All Scripts in Assets/Scripts", EditorStyles.boldLabel);

        if (GUILayout.Button("🔄 Refresh Script List"))
        {
            RefreshScriptList();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("📤 Export to CSV"))
        {
            ExportToCSV();
        }

        if (GUILayout.Button("📤 Export to JSON"))
        {
            ExportToJSON();
        }

        GUILayout.Space(10);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        foreach (var scriptPath in scriptPaths)
        {
            GUILayout.Label(scriptPath, EditorStyles.label);
        }
        GUILayout.EndScrollView();
    }

    private void RefreshScriptList()
    {
        scriptPaths.Clear();
        string rootPath = "Assets/Scripts";

        if (Directory.Exists(rootPath))
        {
            string[] files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);
            scriptPaths.AddRange(files.Select(f => f.Replace("\\", "/")));
        }
        else
        {
            Debug.LogWarning("Scripts folder not found at: " + rootPath);
        }
    }

    private void ExportToCSV()
    {
        string path = EditorUtility.SaveFilePanel("Save Script List as CSV", "", "ScriptList.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();
        sb.AppendLine("Script Name,Relative Path");

        foreach (var scriptPath in scriptPaths)
        {
            string fileName = Path.GetFileName(scriptPath);
            sb.AppendLine($"{fileName},{scriptPath}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log("✅ Script list exported to CSV at: " + path);
    }

    private void ExportToJSON()
    {
        string path = EditorUtility.SaveFilePanel("Save Script List as JSON", "", "ScriptList.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        var scriptList = new List<ScriptEntry>();
        foreach (var scriptPath in scriptPaths)
        {
            scriptList.Add(new ScriptEntry
            {
                name = Path.GetFileName(scriptPath),
                relativePath = scriptPath
            });
        }

        string json = JsonUtility.ToJson(new ScriptListWrapper { scripts = scriptList }, true);
        File.WriteAllText(path, json);
        Debug.Log("✅ Script list exported to JSON at: " + path);
    }

    [System.Serializable]
    public class ScriptEntry
    {
        public string name;
        public string relativePath;
    }

    [System.Serializable]
    public class ScriptListWrapper
    {
        public List<ScriptEntry> scripts;
    }
}
