using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SnapToGridLines : MonoBehaviour
{
    public FieldGridManager gridManager; // Reference to FieldGridManager for grid data
    public EnsembleDirector2 director;
    public MarcherMovement marcherMove;
    public float snapThreshold = 0.1f; // Threshold for snapping (optional)

    // Lists for 8_5 grid positions on X and Z axes
    public List<float> xPositions8_5 = new List<float>();
    public List<float> zPositions8_5 = new List<float>();

    // Define field boundaries based on the selected field type
    private Vector2 footballFieldMin = new Vector2(0, 0);
    private Vector2 footballFieldMax = new Vector2(120, 53);

    private Vector2 winterFieldMin = new Vector2(45, 0);
    private Vector2 winterFieldMax = new Vector2(75, 20);

    // Adjust based on current field type
    public Vector2 currentFieldMin;
    public Vector2 currentFieldMax;

    void Start()
    {
        gridManager = GetComponent<FieldGridManager>();
        director = FindObjectOfType<EnsembleDirector2>();
        marcherMove = FindObjectOfType <MarcherMovement>();
    }

    public void SetFieldBoundaries(FieldGridManager.FieldType fieldType)
    {
        switch (fieldType)
        {
            case FieldGridManager.FieldType.FootballField:
                currentFieldMin = footballFieldMin;
                currentFieldMax = footballFieldMax;
                break;
            case FieldGridManager.FieldType.WinterFloor:
                currentFieldMin = winterFieldMin;
                currentFieldMax = winterFieldMax;
                break;
        }
    }

    // Method to get the closest snapped position based on the selected grid type
    public Vector3 GetSnappedPosition(Vector3 position)
    {
        float closestX = FindClosestXPosition(position.x, xPositions8_5);
        float closestZ = FindClosestZPosition(position.z, zPositions8_5);

        // Clamp values to stay within the selected field's boundaries
        closestX = Mathf.Clamp(closestX, currentFieldMin.x, currentFieldMax.x);
        closestZ = Mathf.Clamp(closestZ, currentFieldMin.y, currentFieldMax.y);

        // Return the snapped position
        return new Vector3(closestX, 0.76f, closestZ);
    }

    private float FindClosestXPosition(float current, List<float> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("No grid positions available for snapping.");
            return current;
        }

        float closest = positions[28];
        float minDistance = Mathf.Abs(current - closest);

        foreach (float pos in positions)
        {
            float distance = Mathf.Abs(current - pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = pos + positions[28];
            }
        }
        return closest;
    }

    private float FindClosestZPosition(float current, List<float> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("No grid positions available for snapping.");
            return current;
        }

        float closest = positions[80];
        Debug.Log("position [80]: " + positions[80] + "current positionz: " + current);
        float minDistance = Mathf.Abs(current - closest);

        foreach (float pos in positions)
        {
            float distance = Mathf.Abs(current - pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = pos + positions[80];
            }
        }
        return closest;
    }

    public Vector3 GetSnappedGizmoPosition(Vector3 position)
    {
        // Find the closest X and Z positions from the selected grid lines
        float closestX = FindClosestXGizmoPosition(position.x, xPositions8_5);
        float closestZ = FindClosestZGizmoPosition(position.z, zPositions8_5);

        // Clamp values to stay within the selected field's boundaries
        closestX = Mathf.Clamp(closestX, currentFieldMin.x, currentFieldMax.x);
        closestZ = Mathf.Clamp(closestZ, currentFieldMin.y, currentFieldMax.y);

        // Return the snapped position
        return new Vector3(closestX, 0.76f, closestZ);
    }

    private float FindClosestXGizmoPosition(float current, List<float> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("No grid positions available for snapping.");
            return current;
        }

        float closest = positions[28];
        float minDistance = Mathf.Abs(current - closest);

        foreach (float pos in positions)
        {
            float distance = Mathf.Abs(current - pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = pos;
            }
        }
        return closest;
    }

    private float FindClosestZGizmoPosition(float current, List<float> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("No grid positions available for snapping.");
            return current;
        }

        float closest = positions[80];
        Debug.Log("position [80]: " + positions[80] + "current positionz: " + current);
        float minDistance = Mathf.Abs(current - closest);

        foreach (float pos in positions)
        {
            float distance = Mathf.Abs(current - pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = pos;
            }
        }
        return closest;
    }
}
