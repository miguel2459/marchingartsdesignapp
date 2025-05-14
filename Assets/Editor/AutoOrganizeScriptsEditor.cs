using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AutoOrganizeScriptsEditor : EditorWindow
{
    private const string csvFilePath = "Assets/MADA_ScriptSystemMap_01.csv"; // Ensure this CSV is inside Assets/
    private const string scriptsRoot = "Assets/Scripts";

    private static bool dryRun = true;

    [MenuItem("Tools/Script Organizer/Auto Organize from CSV")]
    public static void ShowWindow()
    {
        GetWindow<AutoOrganizeScriptsEditor>("Script Organizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("📁 Auto Organize Scripts from CSV", EditorStyles.boldLabel);

        dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);

        if (GUILayout.Button("🛠 Run Organization"))
        {
            OrganizeScripts(dryRun);
        }
    }

    public static void OrganizeScripts(bool dryRunMode)
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError("❌ CSV file not found at: " + csvFilePath);
            return;
        }

        var lines = File.ReadAllLines(csvFilePath);
        var scriptToFolder = new Dictionary<string, string>();

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var split = lines[i].Split(',');
            if (split.Length < 2) continue;

            string scriptName = split[0].Trim();
            string folderPath = split[1].Trim();
            scriptToFolder[scriptName] = folderPath;
        }

        int moveCount = 0;
        int skipCount = 0;

        foreach (var kvp in scriptToFolder)
        {
            string scriptName = kvp.Key;
            string relativeFolder = kvp.Value;

            string[] foundPaths = Directory.GetFiles(scriptsRoot, scriptName, SearchOption.AllDirectories);

            foreach (string fullPath in foundPaths)
            {
                string normalizedPath = fullPath.Replace("\\", "/");
                string targetFolderPath = Path.Combine(scriptsRoot, relativeFolder).Replace("\\", "/");
                string targetPath = Path.Combine(targetFolderPath, scriptName).Replace("\\", "/");

                if (normalizedPath == targetPath)
                {
                    skipCount++;
                    continue; // already in the correct place
                }

                if (dryRunMode)
                {
                    Debug.Log($"[DRY RUN] Would move: {scriptName} → {relativeFolder}");
                    continue;
                }

                if (!Directory.Exists(targetFolderPath))
                {
                    Directory.CreateDirectory(targetFolderPath);
                }

                string moveResult = AssetDatabase.MoveAsset(normalizedPath, targetPath);
                if (string.IsNullOrEmpty(moveResult))
                {
                    Debug.Log($"✅ Moved: {scriptName} → {relativeFolder}");
                    moveCount++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not move {scriptName}: {moveResult}");
                }
            }
        }

        AssetDatabase.Refresh();

        if (dryRunMode)
        {
            Debug.Log($"✅ Dry run complete. Previewed {scriptToFolder.Count} script(s).");
        }
        else
        {
            Debug.Log($"✅ Organization complete. {moveCount} script(s) moved. {skipCount} already in place.");
        }
    }
}
