using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MarcherPositionHistory))]
public class MarcherPositionHistoryEditor : Editor
{
    private MarcherPositionHistory history;
    private int lastUndoCount;
    private int lastRedoCount;

    private void OnEnable()
    {
        history = (MarcherPositionHistory)target;
        lastUndoCount = history.GetUndoCount();
        lastRedoCount = history.GetRedoCount();
        EditorApplication.update += OnEditorUpdate;

        MarcherPositionHistory.OnHistoryContextChanged += ForceRepaint; // ✅ subscribe
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        MarcherPositionHistory.OnHistoryContextChanged -= ForceRepaint; // ✅ unsubscribe
    }

    private void ForceRepaint()
    {
        Debug.Log("🌀 [Editor] ForceRepaint triggered via OnHistoryContextChanged");
        lastUndoCount = -1;
        lastRedoCount = -1;
        Repaint();
    }
    
    private void OnEditorUpdate()
    {
        if (history == null || history != target)
            return;


        if (history.GetUndoCount() != lastUndoCount || history.GetRedoCount() != lastRedoCount)
        {
            lastUndoCount = history.GetUndoCount();
            lastRedoCount = history.GetRedoCount();
            Repaint();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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

        if (GUILayout.Button("🔄 Manual Repaint"))
        {
            Repaint();
        }
    }
}
