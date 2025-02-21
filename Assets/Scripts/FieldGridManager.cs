using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class FieldGridManager : MonoBehaviour
{
    public StepSize currentStepSize = StepSize.Freeform;
    public float yardLength = 5f; // Yard length in Unity units
    public int footballFieldWidthInYards = 53; // Field width in yards
    public int footballFieldLengthInYards = 120; // Field length in yards (including end zones)
    public Color gridColor = Color.green; // Color for the grid lines
    public float lineWidth = 0.05f;

    private float intervalX; // Interval size along X-axis
    private float intervalZ; // Interval size along Z-axis
    public GameObject Grid8_5;

    public enum FieldType
{
    FootballField,
    WinterFloor
}

    public FieldType currentFieldType = FieldType.FootballField; // Default to football

    public GameObject footballField; // 3D football field object
    public GameObject footballGrid;  // 8-to-5 gridlines for football field

    public GameObject winterFloor; // 3D winter floor object
    public GameObject winterGrid;  // 8-to-5 gridlines for winter floor

    public SnapToGridLines snapToGrid; // Reference to SnapToGridLines

    // Lists to store LineRenderers for different step sizes
    public List<LineRenderer> gridLines = new List<LineRenderer>();
    public List<LineRenderer> fieldGrid8_5 = new List<LineRenderer>();

    void Start()
    {
        snapToGrid = GetComponent<SnapToGridLines>(); // Get reference to SnapToGrid
        SetFieldType(currentFieldType); // Ensure the correct field is enabled
    }

    public void SetFieldType(FieldType fieldType)
    {
        currentFieldType = fieldType;

        if (fieldType == FieldType.FootballField)
        {
            footballField.SetActive(true);
            footballGrid.SetActive(true);
            winterFloor.SetActive(false);
            winterGrid.SetActive(false);
        }
        else if (fieldType == FieldType.WinterFloor)
        {
            footballField.SetActive(false);
            footballGrid.SetActive(false);
            winterFloor.SetActive(true);
            winterGrid.SetActive(true);
        }

        // Inform SnapToGridLines of the new field type
        if (snapToGrid != null)
        {
            snapToGrid.SetFieldBoundaries(currentFieldType);
        }
    }


    public void SetStepSize(StepSize stepSize)
    {
        currentStepSize = stepSize;

        // Calculate interval sizes based on step size
        switch (stepSize)
        {
            case StepSize.SixteenSteps:
                intervalX = yardLength / 16;
                intervalZ = yardLength / 16;
                break;
            case StepSize.TwelveSteps:
                intervalX = yardLength / 12;
                intervalZ = yardLength / 12;
                break;
            case StepSize.EightSteps:
                intervalX = yardLength / 8;
                intervalZ = yardLength / 8;
                break;
            case StepSize.SixSteps:
                intervalX = yardLength / 6;
                intervalZ = yardLength / 6;
                break;
            case StepSize.FiveSteps:
                intervalX = yardLength / 5;
                intervalZ = yardLength / 5;
                break;
            case StepSize.FourSteps:
                intervalX = yardLength / 4;
                intervalZ = yardLength / 4;
                break;
            case StepSize.ThreeAndHalfSteps:
                intervalX = yardLength / 3.5f;
                intervalZ = yardLength / 3.5f;
                break;
            default: // Freeform or unspecified step size
                intervalX = 1f;
                intervalZ = 1f;
                break;
        }
    }

    public void UpdateGridStepSize()
    {
        //SetStepSize(currentStepSize);
        FindPreGeneratedGrids(); // Check for pre-generated grids before generating new ones
        //GenerateGrid(); // Generate new grid if needed
    }

    private void FindPreGeneratedGrids()
    {
        // Clear existing lists
        fieldGrid8_5.Clear();

        // Look for specific child objects with the names "Field Grid 5_5", "Field Grid 6_5", "Field Grid 8_5"
        Transform grid8_5 = transform.Find("Field Grid 8_5");

        if (grid8_5 != null)
        {
            Debug.Log("Found pre-generated Field Grid 8_5.");
            fieldGrid8_5.AddRange(GetLineRenderers(grid8_5));
            Grid8_5 = grid8_5.gameObject;
        }
    }

    private List<LineRenderer> GetLineRenderers(Transform parentTransform)
    {
        List<LineRenderer> lines = new List<LineRenderer>();

        foreach (Transform child in parentTransform)
        {
            LineRenderer lineRenderer = child.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lines.Add(lineRenderer);
            }
        }

        return lines;
    }

    private void GenerateGrid()
    {
        // Clear existing LineRenderers if they exist
        foreach (var line in gridLines)
        {
            if (line != null)
                DestroyImmediate(line.gameObject);
        }
        gridLines.Clear();

        // Calculate total units for the field dimensions based on yard length
        float fieldWidthInUnits = footballFieldWidthInYards;  // Should be 265 for 53 yards with 5 units per yard
        float fieldLengthInUnits = footballFieldLengthInYards; // Should be 600 for 120 yards with 5 units per yard

        // Calculate exact number of intervals within the field boundaries
        int numVerticalLines = Mathf.CeilToInt(fieldWidthInUnits / intervalX);
        int numHorizontalLines = Mathf.CeilToInt(fieldLengthInUnits / intervalZ);

        // Generate vertical grid lines along the width of the field
        for (int i = 0; i <= numVerticalLines; i++)
        {
            float x = i * intervalX;
            if (x > fieldWidthInUnits) x = fieldWidthInUnits; // Snap to boundary if exceeding

            Vector3 startPos = new Vector3(x, 0, 0);
            Vector3 endPos = new Vector3(x, 0, fieldLengthInUnits);
            CreateLine(startPos, endPos);
        }

        // Generate horizontal grid lines along the length of the field
        for (int j = 0; j <= numHorizontalLines; j++)
        {
            float z = j * intervalZ;
            if (z > fieldLengthInUnits) z = fieldLengthInUnits; // Snap to boundary if exceeding

            Vector3 startPos = new Vector3(0, 0, z);
            Vector3 endPos = new Vector3(fieldWidthInUnits, 0, z);
            CreateLine(startPos, endPos);
        }
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = transform;

        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = gridColor;
        lineRenderer.endColor = gridColor;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        gridLines.Add(lineRenderer);
    }
}
