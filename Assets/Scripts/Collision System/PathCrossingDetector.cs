using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathCrossingDetector
{
    /// <summary>
    /// Returns all (A, B) marcher pairs whose paths would cross between current and assigned positions.
    /// </summary>
    public static List<(GameObject, GameObject)> DetectCrossings(Dictionary<GameObject, Vector3> assignments)
    {
        List<(GameObject, GameObject)> crossings = new List<(GameObject, GameObject)>();
        var pairs = assignments.ToList();

        for (int i = 0; i < pairs.Count; i++)
        {
            Vector3 aStart = pairs[i].Key.transform.position;
            Vector3 aEnd = pairs[i].Value;

            for (int j = i + 1; j < pairs.Count; j++)
            {
                Vector3 bStart = pairs[j].Key.transform.position;
                Vector3 bEnd = pairs[j].Value;

                if (SegmentsCross2D(aStart, aEnd, bStart, bEnd))
                    crossings.Add((pairs[i].Key, pairs[j].Key));
            }
        }

        return crossings;
    }

    private static bool SegmentsCross2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        Vector2 A1 = new Vector2(a1.x, a1.z);
        Vector2 A2 = new Vector2(a2.x, a2.z);
        Vector2 B1 = new Vector2(b1.x, b1.z);
        Vector2 B2 = new Vector2(b2.x, b2.z);

        return DoLinesIntersect(A1, A2, B1, B2);
    }

    private static bool DoLinesIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float o1 = Orientation(p1, p2, q1);
        float o2 = Orientation(p1, p2, q2);
        float o3 = Orientation(q1, q2, p1);
        float o4 = Orientation(q1, q2, p2);

        return o1 != o2 && o3 != o4;
    }

    private static float Orientation(Vector2 a, Vector2 b, Vector2 c)
    {
        return Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
    }
}
