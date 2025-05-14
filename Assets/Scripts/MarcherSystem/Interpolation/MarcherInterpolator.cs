using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles interpolation logic between confirmed marcher positions.
/// Used by MarcherPositionsManager to compute inferred paths.
/// </summary>
public class MarcherInterpolator
{
    private readonly MarcherPositionsManager parent;
    private readonly Func<int, int> getMaxCountForSet;
    private readonly Func<Vector3> getFallbackPosition;

    public MarcherInterpolator(
        MarcherPositionsManager parent,
        Func<int, int> getMaxCountForSet,
        Func<Vector3> getFallbackPosition = null)
    {
        this.parent = parent;
        this.getMaxCountForSet = getMaxCountForSet;
        this.getFallbackPosition = getFallbackPosition ?? (() => parent.transform.position);
    }

    public bool TryFindLastConfirmedPosition(int currentSetIndex, int beforeCountIndex, out int foundSetIndex, out int foundCountIndex, out Vector3 pos)
    {
        foundSetIndex = -1;
        foundCountIndex = -1;
        pos = Vector3.zero;

        if (parent.countPositions.TryGetValue(currentSetIndex, out var currentSet))
        {
            for (int i = beforeCountIndex - 1; i >= 1; i--)
            {
                if (currentSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = currentSetIndex;
                    foundCountIndex = i;
                    pos = entry.pos;
                    return true;
                }
            }
        }

        for (int s = currentSetIndex - 1; s >= 0; s--)
        {
            if (!parent.countPositions.TryGetValue(s, out var previousSet)) continue;

            int maxCount = getMaxCountForSet(s);
            int minCount = (s == 0) ? 0 : 1;

            for (int i = maxCount; i >= minCount; i--)
            {
                if (previousSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = s;
                    foundCountIndex = i;
                    pos = entry.pos;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryFindNextConfirmedPosition(int currentSetIndex, int afterCountIndex, out int foundSetIndex, out int foundCountIndex, out Vector3 pos)
    {
        foundSetIndex = -1;
        foundCountIndex = -1;
        pos = Vector3.zero;

        if (parent.countPositions.TryGetValue(currentSetIndex, out var currentSet))
        {
            for (int i = afterCountIndex + 1; i <= getMaxCountForSet(currentSetIndex); i++)
            {
                if (currentSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = currentSetIndex;
                    foundCountIndex = i;
                    pos = entry.pos;
                    return true;
                }
            }
        }

        for (int s = currentSetIndex + 1; s <= SessionManager.instance.showStateSO.NumberOfSets; s++)
        {
            if (!parent.countPositions.TryGetValue(s, out var futureSet)) continue;

            for (int i = 1; i <= getMaxCountForSet(s); i++)
            {
                if (futureSet.TryGetValue(i, out var entry) && entry.IsConfirmed)
                {
                    foundSetIndex = s;
                    foundCountIndex = i;
                    pos = entry.pos;
                    return true;
                }
            }
        }

        return false;
    }

    public List<(int set, int count)> GetInterpolationSteps(int fromSet, int fromCount, int toSet, int toCount)
    {
        List<(int set, int count)> steps = new List<(int set, int count)>();

        for (int s = fromSet; s <= toSet; s++)
        {
            int startC = (s == fromSet) ? fromCount : 1;
            int endC   = (s == toSet)   ? toCount   : getMaxCountForSet(s);

            if (s == 0 || endC < startC) continue;

            for (int c = startC; c <= endC; c++)
            {
                if (c > getMaxCountForSet(s)) break;
                steps.Add((s, c));
            }
        }

        return steps;
    }

    public void ApplyInterpolatedPositions(Vector3 start, Vector3 end, List<(int set, int count)> steps)
    {
        int totalSteps = steps.Count + 1;

        for (int i = 0; i < steps.Count; i++)
        {
            float t = (i + 1f) / totalSteps;
            Vector3 pos = Vector3.Lerp(start, end, t);
            var (s, c) = steps[i];

            if (!parent.countPositions.ContainsKey(s))
                parent.countPositions[s] = new Dictionary<int, PositionEntry>();

            parent.countPositions[s][c] = new PositionEntry(pos, "inferred");
        }
    }

    public Vector3 GetBestPosition(int setIndex, int countIndex)
    {
        // 1. Direct match
        if (parent.countPositions.TryGetValue(setIndex, out var currentSet))
        {
            if (currentSet.TryGetValue(countIndex, out var direct) && !direct.IsUnset)
                return direct.pos;

            // 2. Fallback within current set
            for (int i = countIndex - 1; i >= 1; i--)
            {
                if (currentSet.TryGetValue(i, out var fallback) && fallback.IsConfirmed)
                    return fallback.pos;
            }
        }

        // 3. Fallback to previous sets (searching backward)
        for (int s = setIndex - 1; s >= 0; s--)
        {
            if (parent.countPositions.TryGetValue(s, out var previousSet))
            {
                int max = getMaxCountForSet(s);
                for (int i = max; i >= 0; i--)
                {
                    if (previousSet.TryGetValue(i, out var prevEntry) && prevEntry.IsConfirmed)
                        return prevEntry.pos;
                }
            }
        }

        // 4. Final fallback
        return getFallbackPosition();
    }

}
