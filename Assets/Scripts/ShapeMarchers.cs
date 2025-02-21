using UnityEngine;
using System.Collections.Generic;

public class ShapeMarchers : MonoBehaviour
{
    public LinesManager linesManager;
    public CurvesManager curvesManager;
    public BoxManager boxManager;
    public CircleManager circleManager;
    public IntervalManager intervalManager;

    public List<ShapeGroup> shapes = new List<ShapeGroup>();
    public float defaultSpacing = 2.0f;

    void Start()
    {
        if (!linesManager) linesManager = GetComponent<LinesManager>();
        if (!curvesManager) curvesManager = GetComponent<CurvesManager>();
        if (!boxManager) boxManager = GetComponent<BoxManager>();
        if (!circleManager) circleManager = GetComponent<CircleManager>();
        if (!intervalManager) intervalManager = GetComponent<IntervalManager>();
    }

    public void InitializeShapeManagers(GameObject marcherPrefab, GameObject positionSpherePrefab, float marcherSpacing)
    {
        linesManager.Initialize(marcherPrefab, positionSpherePrefab, marcherSpacing);
        curvesManager.Initialize(marcherPrefab, positionSpherePrefab, marcherSpacing);
        boxManager.Initialize(marcherPrefab, positionSpherePrefab, marcherSpacing);
        circleManager.Initialize(marcherPrefab, positionSpherePrefab, marcherSpacing);
    }

    // Main method to set a marching formation and add it to the shapes list
    public void ArrangeFormation(ShapeGroup shapeGroup)
    {
        Debug.Log($"Arranging Formation - ShapeType: {shapeGroup.shapeType}, Interval: {shapeGroup.intervalType}");

        // Arrange the formation based on the shape type
        switch (shapeGroup.shapeType)
        {
            case ShapeType.Line:
                linesManager.CreateLineFormation(shapeGroup.marchers, shapeGroup.intervalType);
                break;
            
            case ShapeType.Curve:
                curvesManager.CreateCurveFormation(shapeGroup.marchers, shapeGroup.intervalType);
                break;
            
            case ShapeType.Box:
                boxManager.CreateBoxFormation(shapeGroup.marchers, shapeGroup.intervalType, shapeGroup.isFilled);
                break;
            
            case ShapeType.Circle:
                circleManager.CreateCircleFormation(shapeGroup.marchers, shapeGroup.intervalType);
                break;
            
            default:
                Debug.LogWarning("Unknown ShapeType selected.");
                break;
        }

        // Log completion and add the shape group to shapes list
        Debug.Log("ArrangeFormation completed for shape: " + shapeGroup.shapeType);
        //shapes.Add(shapeGroup);
    }

    // Overload to create a new shape group and arrange it
    public void ArrangeFormation(ShapeType shapeType, IntervalManager.IntervalType intervalType, List<GameObject> marchers)
    {
        ShapeGroup newGroup = new ShapeGroup(shapeType, intervalType);
        newGroup.marchers = new List<GameObject>(marchers); // Assign marchers to the new group
        ArrangeFormation(newGroup);
    }

    public enum ShapeType { Line, Curve, Box, Circle }
}
