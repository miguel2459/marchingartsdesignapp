using UnityEngine;

/// <summary>
/// Represents a tagged position for a specific set and count in the marching drill.
/// </summary>
[System.Serializable]
public class PositionEntry
{
    public Vector3 pos;
    public string type; // "march", "hold", "inferred", "unset"

    public PositionEntry()
    {
        pos = Vector3.zero;
        type = "unset";
    }

    public PositionEntry(Vector3 position, string tag)
    {
        pos = position;
        type = tag;
    }

    public SerializablePositionEntry ToSerializable()
    {
        return new SerializablePositionEntry
        {
            x = pos.x,
            y = pos.y,
            z = pos.z,
            type = type
        };
    }

    [System.Serializable]
    public struct SerializablePositionEntry
    {
        public float x, y, z;
        public string type;
    }


    public bool IsConfirmed => type == "march" || type == "hold";
    public bool IsInferred => type == "inferred";
    public bool IsUnset => type == "unset";
}
