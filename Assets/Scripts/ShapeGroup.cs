using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShapeGroup
{
    public ShapeMarchers.ShapeType shapeType;
    public IntervalManager.IntervalType intervalType;
    public List<GameObject> marchers = new List<GameObject>();
    public float angle = 0;
    public float size = 0;
    public bool isFilled = true; // Only relevant for Box shapes

    public ShapeGroup(ShapeMarchers.ShapeType shapeType, IntervalManager.IntervalType intervalType)
    {
        this.shapeType = shapeType;
        this.intervalType = intervalType;
    }
}
