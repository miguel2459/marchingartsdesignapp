using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldGridManager))]
public class FieldGridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();

        // Add an Update button
        FieldGridManager gridManager = (FieldGridManager)target;
        if (GUILayout.Button("Update Grid"))
        {
            gridManager.UpdateGridStepSize();
        }
    }
}
