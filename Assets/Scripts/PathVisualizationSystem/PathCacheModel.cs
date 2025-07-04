// PathCacheModel.cs
using UnityEngine;

public class PathCacheModel
{
    public Vector3? PreviousConfirmed { get; set; }
    public Vector3? NextConfirmed { get; set; }
    public Vector3? ActiveConfirmed { get; set; }

    public void Clear()
    {
        PreviousConfirmed = null;
        NextConfirmed = null;
        ActiveConfirmed = null;
    }
}