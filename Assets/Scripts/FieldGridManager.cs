using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class FieldGridManager : MonoBehaviour
{
    public enum FieldType { FootballField, WinterFloor }
    public enum StepSize { Freeform, SixteenSteps, TwelveSteps, EightSteps, SixSteps, FiveSteps, FourSteps, ThreeAndHalfSteps }

    [Header("Field Settings")]
    public FieldType currentFieldType;
    public StepSize currentStepSize = StepSize.Freeform;
    public float yardLength = 5f;
    public int footballFieldWidthInYards = 53;
    public int footballFieldLengthInYards = 120;
    public Color gridColor = Color.green;
    public float lineWidth = 0.05f;

    [Header("Field References")]
    public GameObject footballField;
    public GameObject footballGrid;
    public GameObject winterFloor;
    public GameObject winterGrid;
    public GameObject Grid8_5;

    private float intervalX;
    private float intervalZ;

    private SnapToGridLines snapToGrid;
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    private List<LineRenderer> fieldGrid8_5 = new List<LineRenderer>();

    void Start()
    {
        snapToGrid = GetComponent<SnapToGridLines>();
        SetFieldType(GetFieldTypeFromSession());
    }

    private FieldType GetFieldTypeFromSession()
    {
        string sessionType = SessionManager.instance.fieldType;
        Debug.Log($"FieldGridManager: Received field type from session: {sessionType}");

        return sessionType switch
        {
            "Football Field" => FieldType.FootballField,
            "Winter Floor" => FieldType.WinterFloor,
            _ => throw new System.Exception($"Could not determine field type from session. Received: {sessionType}")
        };
    }


    public void SetFieldType(FieldType fieldType)
    {
        currentFieldType = fieldType;

        bool isFootball = (fieldType == FieldType.FootballField);
        footballField.SetActive(isFootball);
        footballGrid.SetActive(isFootball);
        winterFloor.SetActive(!isFootball);
        winterGrid.SetActive(!isFootball);

        snapToGrid?.SetFieldBoundaries(fieldType);
    }

    public Vector3 GetFieldCenter()
    {
        if (currentFieldType == FieldGridManager.FieldType.FootballField)
        {
            // Define the center of the football field
            return new Vector3(26.25f, 0.8f, 60);
        }
        else if (currentFieldType == FieldGridManager.FieldType.WinterFloor)
        {
            // Define the center of the winter floor
            return new Vector3(8, 0.8f, 60); // Adjust based on winter floor dimensions
        }
        else
        {
            Debug.LogWarning("FieldType is unrecognized. Defaulting to (0,0,0).");
            return Vector3.zero;
        }
    }

    public void SetStepSize(StepSize stepSize)
    {
        currentStepSize = stepSize;
        intervalX = intervalZ = stepSize switch
        {
            StepSize.SixteenSteps => yardLength / 16,
            StepSize.TwelveSteps => yardLength / 12,
            StepSize.EightSteps => yardLength / 8,
            StepSize.SixSteps => yardLength / 6,
            StepSize.FiveSteps => yardLength / 5,
            StepSize.FourSteps => yardLength / 4,
            StepSize.ThreeAndHalfSteps => yardLength / 3.5f,
            _ => 1f
        };
    }

    public void UpdateGridStepSize()
    {
        FindPreGeneratedGrids();
    }

    private void FindPreGeneratedGrids()
    {
        fieldGrid8_5.Clear();

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
            if (child.TryGetComponent(out LineRenderer lineRenderer))
            {
                lines.Add(lineRenderer);
            }
        }
        return lines;
    }

    public void GenerateGrid()
    {
        ClearExistingGrid();
        float fieldWidthInUnits = footballFieldWidthInYards;
        float fieldLengthInUnits = footballFieldLengthInYards;
        
        int numVerticalLines = Mathf.CeilToInt(fieldWidthInUnits / intervalX);
        int numHorizontalLines = Mathf.CeilToInt(fieldLengthInUnits / intervalZ);

        for (int i = 0; i <= numVerticalLines; i++)
            CreateLine(new Vector3(i * intervalX, 0, 0), new Vector3(i * intervalX, 0, fieldLengthInUnits));

        for (int j = 0; j <= numHorizontalLines; j++)
            CreateLine(new Vector3(0, 0, j * intervalZ), new Vector3(fieldWidthInUnits, 0, j * intervalZ));
    }

    private void ClearExistingGrid()
    {
        foreach (var line in gridLines)
            if (line != null) DestroyImmediate(line.gameObject);
        
        gridLines.Clear();
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
