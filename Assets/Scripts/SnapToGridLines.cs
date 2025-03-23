using UnityEngine;
using System;
using System.Collections.Generic;
public class SnapToGridLines : MonoBehaviour
{
    public FieldGridManager gridManager; // Reference to FieldGridManager for field boundaries
    public float snapThreshold = 0.1f; // Threshold for snapping (optional)

    // Lists for 8_5 grid positions on X and Z axes
    public List<float> xPositions8_5 = new List<float>();
    public List<float> zPositions8_5 = new List<float>();

    // Define field boundaries based on the selected field type
    public Vector2 currentFieldMin;
    public Vector2 currentFieldMax;

    public static event Action OnGridReady; // Event to notify when the grid is ready

    void Start()
    {
        //SetFieldBoundaries(gridManager.currentFieldType);
    }

    /// <summary>
    /// Retrieves and sets field boundaries based on the selected field type from FieldGridManager.
    /// </summary>
    public void SetFieldBoundaries(FieldGridManager.FieldType fieldType)
    {
        switch (fieldType)
        {
            case FieldGridManager.FieldType.FootballField:
                currentFieldMin = new Vector2(0, 0);
                currentFieldMax = new Vector2(53, 120);
                break;

            case FieldGridManager.FieldType.WinterFloor:
                currentFieldMin = new Vector2(0, 45);
                currentFieldMax = new Vector2(20, 75);
                break;

            default:
                Debug.LogWarning("SnapToGridLines: Unrecognized field type.");
                currentFieldMin = Vector2.zero;
                currentFieldMax = Vector2.zero;
                break;
        }
        Debug.Log($"SnapToGridLines: Field boundaries updated: Min {currentFieldMin}, Max {currentFieldMax}");
        GenerateGridPositions();       
    }


    /// <summary>
    /// Snaps a given position to the nearest 8-to-5 grid point while considering field boundaries.
    /// </summary>
    public Vector3 GetSnappedPosition(Vector3 position)
    {
        float closestX = FindClosestPosition(position.x, xPositions8_5);
        float closestZ = FindClosestPosition(position.z, zPositions8_5);

        // Clamp snapped values within the current field boundaries
        closestX = Mathf.Clamp(closestX, currentFieldMin.x, currentFieldMax.x);
        closestZ = Mathf.Clamp(closestZ, currentFieldMin.y, currentFieldMax.y);

        return new Vector3(closestX, 0.76f, closestZ);
    }

    /// <summary>
    /// Snaps a given gizmo position to the nearest valid grid point.
    /// </summary>
    public Vector3 GetSnappedGizmoPosition(Vector3 position)
    {
        float closestX = FindClosestPosition(position.x, xPositions8_5);
        float closestZ = FindClosestPosition(position.z, zPositions8_5);

        closestX = Mathf.Clamp(closestX, currentFieldMin.x, currentFieldMax.x);
        closestZ = Mathf.Clamp(closestZ, currentFieldMin.y, currentFieldMax.y);

        return new Vector3(closestX, 0.76f, closestZ);
    }

    /// <summary>
    /// Finds the closest grid-aligned position for X or Z values.
    /// </summary>
    private float FindClosestPosition(float current, List<float> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            Debug.LogWarning("SnapToGridLines: No grid positions available for snapping.");
            return current; // Return current if no positions exist
        }

        float closest = positions[0]; // Start with the first value in the list
        float minDistance = Mathf.Abs(current - closest); // Initialize min distance

        // Iterate through positions to find the closest one
        foreach (float pos in positions)
        {
            float distance = Mathf.Abs(current - pos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = pos;
            }
        }

        Debug.Log($"SnapToGridLines: Snapping {current} to closest grid position {closest}");
        return closest;
    }


    /// <summary>
    /// Generates 8-to-5 grid positions for X and Z axes if they are not already defined.
    /// </summary>
    public void GenerateGridPositions()
    {        
        xPositions8_5.Clear();
        zPositions8_5.Clear();
        
        if (xPositions8_5.Count == 0 || zPositions8_5.Count == 0)
        {
            Debug.Log("SnapToGridLines: Generating 8-to-5 grid positions...");


            float stepSize = 5f / 8f; // Standard 8-to-5 step size

            if (gridManager != null)
            {
                Vector2 fieldMin = currentFieldMin;
                Vector2 fieldMax = currentFieldMax;

                // Populate X positions (horizontal lines)
                for (float x = fieldMin.x; x <= fieldMax.x; x += stepSize)
                {
                    xPositions8_5.Add(x);
                }

                // Populate Z positions (vertical lines)
                for (float z = fieldMin.y; z <= fieldMax.y; z += stepSize)
                {
                    zPositions8_5.Add(z);
                }
            }
            else
            {
                Debug.LogError("SnapToGridLines: GridManager reference is missing, cannot generate grid positions.");
            }

            Debug.Log($"SnapToGridLines: Generated {xPositions8_5.Count} X positions and {zPositions8_5.Count} Z positions.");
            OnGridReady?.Invoke(); // Fire event when grid is fully initialized
        }
    }
    
}


   // private Vector2 footballFieldMax = new Vector2(120, 53);