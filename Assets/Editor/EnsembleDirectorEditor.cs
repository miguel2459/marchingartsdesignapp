using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnsembleDirector2))]
public class EnsembleDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default inspector

        EnsembleDirector2 ensembleDirector = (EnsembleDirector2)target;

        if (GUILayout.Button("Populate Marchers"))
        {
            ensembleDirector.PopulateMarchers();
        }
    }
}
