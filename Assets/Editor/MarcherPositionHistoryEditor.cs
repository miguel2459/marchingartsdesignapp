using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MarcherPositionHistory))]
public class MarcherPositionHistoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var history = (MarcherPositionHistory)target;

        GUILayout.Space(10);
        GUILayout.Label("🧠 Undo Stack (Preview)", EditorStyles.boldLabel);

        if (history.DebugUndo != null)
        {
            for (int i = 0; i < Mathf.Min(5, history.DebugUndo.Count); i++)
            {
                var batch = history.DebugUndo[i];
                GUILayout.Label($"Batch {i} ({batch.Count} changes):");

                foreach (var change in batch)
                {
                    GUILayout.Label($"• {change.marcher?.name} | Set {change.set}, Count {change.count} | Intent: {change.intent}");
                }
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("🔁 Redo Stack (Preview)", EditorStyles.boldLabel);

        if (history.DebugRedo != null)
        {
            for (int i = 0; i < Mathf.Min(5, history.DebugRedo.Count); i++)
            {
                var batch = history.DebugRedo[i];
                GUILayout.Label($"Batch {i} ({batch.Count} changes):");

                foreach (var change in batch)
                {
                    GUILayout.Label($"• {change.marcher?.name} | Set {change.set}, Count {change.count} | Intent: {change.intent}");
                }
            }
        }
    }
}
