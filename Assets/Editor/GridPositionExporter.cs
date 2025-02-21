using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class GridPositionExporter : EditorWindow
{
    private SnapToGridLines snapToGrid;
    private string exportPath = "Assets/DrillPositions";

    private const float FIELD_LENGTH = 120f;
    private const float FIELD_WIDTH = 53.3f;
    private const float STEP_SIZE = 5f / 8f; // 8-to-5 step size

    [MenuItem("Marching Band/Export Grid Positions")]
    public static void ShowWindow()
    {
        GetWindow<GridPositionExporter>("Grid Position Exporter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Marching Band Grid Position Exporter", EditorStyles.boldLabel);
        snapToGrid = EditorGUILayout.ObjectField("Snap To Grid Component", snapToGrid, typeof(SnapToGridLines), true) as SnapToGridLines;
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);

        if (GUILayout.Button("Export 8-to-5 Grid"))
        {
            if (snapToGrid == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the SnapToGridLines component!", "OK");
                return;
            }

            ExportGridPositions();
        }
    }

    private void ExportGridPositions()
    {
        if (!Directory.Exists(exportPath))
        {
            Directory.CreateDirectory(exportPath);
        }

        ExportGridType(snapToGrid.xPositions8_5, snapToGrid.zPositions8_5);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", "8-to-5 grid positions exported successfully!", "OK");
    }

    private void ExportGridType(List<float> xPositions, List<float> zPositions)
    {
        string filename = Path.Combine(exportPath, "DrillPositions_8-5.csv");
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("X,Z,Yard Line Reference,Side,Position Description");

        foreach (float x in xPositions)
        {
            foreach (float z in zPositions)
            {
                string positionData = GeneratePositionData(x, z);
                csv.AppendLine(positionData);
            }
        }

        File.WriteAllText(filename, csv.ToString());
    }

    private string GeneratePositionData(float x, float z)
    {
        int yardLine = CalculateYardLine(z);
        int side = (z < 60f) ? 2 : 1;
        string xDescription = DetermineXPositionDescription(x);
        string zDescription = DetermineZPositionDescription(z, yardLine, side);
        string positionDescription = $"S{side}, {zDescription}, {xDescription}";

        return $"{x},{z},{yardLine},{side},\"{positionDescription}\"";
    }

    private int CalculateYardLine(float z)
    {
        // Special case for the 50-yard line at the center of the field
        if (Mathf.Abs(z - 60) <= 2.5f)
        {
            return 50;
        }
        
        if (z < 60) // Side 2
        {
            if (z >= 52.5f && z <= 57.5f) return 45;
            if (z >= 47.5f && z <= 52.5f) return 40;
            if (z >= 42.5f && z <= 47.5f) return 35;
            if (z >= 37.5f && z <= 42.5f) return 30;
            if (z >= 32.5f && z <= 37.5f) return 25;
            if (z >= 27.5f && z <= 32.5f) return 20;
            if (z >= 22.5f && z <= 27.5f) return 15;
            if (z >= 17.5f && z <= 22.5f) return 10;
            if (z >= 12.5f && z <= 17.5f) return 5;
            if (z >= 0f && z <= 12.5f) return 0; // Account for steps outside the 0 yard line for Side 2
        }
        else // Side 1
        {
            if (z >= 62.5f && z <= 67.5f) return 45;
            if (z >= 67.5f && z <= 72.5f) return 40;
            if (z >= 72.5f && z <= 77.5f) return 35;
            if (z >= 77.5f && z <= 82.5f) return 30;
            if (z >= 82.5f && z <= 87.5f) return 25;
            if (z >= 87.5f && z <= 92.5f) return 20;
            if (z >= 92.5f && z <= 97.5f) return 15;
            if (z >= 97.5f && z <= 102.5f) return 10;
            if (z >= 102.5f && z <= 107.5f) return 5;
            if (z >= 107.5f && z <= 120f) return 0; // Account for steps outside the 0 yard line for Side 1
        }

        // If z doesn't match any specified range, return -1 as an invalid result
        return -1;
    }


    private string DetermineXPositionDescription(float x)
    {
        if (Mathf.Approximately(x, 0f))
            return "On front sideline";
        if (x > 0 && x <= 8.75f)
            return $"{Mathf.RoundToInt((x) / STEP_SIZE)} steps behind front sideline";
        if (x > 8.75f && x <= 16.875f)
            return $"{Mathf.RoundToInt((16.875f - x) / STEP_SIZE) + 1} steps in front of front hash";
        if (Mathf.Approximately(x, 17.5f))
            return "On front hash";
        if (x >= 18.125f && x <= 25.625f)
            return $"{Mathf.RoundToInt((x) / STEP_SIZE) - 28} steps behind front hash";
        if (x >= 26.25f && x <= 34.875f)
            return $"{Mathf.RoundToInt((26.25f - x) / STEP_SIZE) + 14} steps in front of back hash";
        if (Mathf.Approximately(x, 35f))
            return "On back hash";
        if (x >= 35.625f && x <= 43.75f)
            return $"{Mathf.RoundToInt((x) / STEP_SIZE) - 56} steps behind back hash";
        if (x >= 43.75f && x <= 51.875f)
            return $"{Mathf.RoundToInt((43.75f - x) / STEP_SIZE) + 14} steps in front of back sideline";
        if (Mathf.Approximately(x, 52.5f))
            return "On back sideline";
        if (Mathf.Approximately(x, 53f))
            return "On TRUE back sideline";

        return $"{Mathf.RoundToInt(x / STEP_SIZE)} steps from sideline";
    }

    private string DetermineZPositionDescription(float z, int yardLine, int side)
    {
        // Handle Side 2 end zone (z from 0 to 10)
        if (side == 2 && z >= 0 && z <= 10)
        {
            int stepsOutside = Mathf.RoundToInt(z / STEP_SIZE);
            return stepsOutside == 16 ? "On the 0 yard line" : $"{16 - stepsOutside} steps outside the 0 yard line";
        }

        // Handle Side 1 end zone (z from 110 to 120)
        if (side == 1 && z >= 110 && z <= 120)
        {
            int stepsOutside = Mathf.RoundToInt((120 - z) / STEP_SIZE);
            return stepsOutside == 16 ? "On the 0 yard line" : $"{16 - stepsOutside} steps outside the 0 yard line";
        }

        // For other yard lines within the field, mirror Side 2's logic to Side 1 by reflecting z across the 50-yard line
        float reflectedZ = (side == 1) ? 120 - z : z;
        int reflectedYardLine = CalculateYardLine(reflectedZ);

        float stepsFromYardLine = Mathf.Abs(reflectedZ - reflectedYardLine) / STEP_SIZE;
        float offset = 10f; // Use offset of 10 for both sides

        // Adjusted range check to ensure mirrored behavior for both sides
        if (reflectedZ >= (reflectedYardLine - 2.5f) + offset && reflectedZ <= (reflectedYardLine + 2.5f) + offset)
        {
            if (Mathf.Approximately(stepsFromYardLine, 16))
            {
                return $"On {reflectedYardLine} yard line";
            }

            // Determine inside/outside based on mirrored logic
            string insideOutside = (side == 2 && reflectedZ < reflectedYardLine + offset) || 
                                (side == 1 && reflectedZ < reflectedYardLine + offset) ? "outside" : "inside";
            return $"{Mathf.Abs(stepsFromYardLine - 16):F0} steps {insideOutside} {reflectedYardLine} yard line";
        }

        return null;
    }
}
