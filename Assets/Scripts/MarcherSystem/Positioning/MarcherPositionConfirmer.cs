using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcherPositionConfirmer
{
    private readonly EnsembleDirector2 director;
    private readonly MarcherPositionHistory positionHistory;
    private readonly Func<int, int> getMaxCountForSet;

    public MarcherPositionConfirmer(EnsembleDirector2 director, MarcherPositionHistory positionHistory, Func<int, int> getMaxCountForSet)
    {
        this.director = director;
        this.positionHistory = positionHistory;
        this.getMaxCountForSet = getMaxCountForSet;
    }

    public void Confirm(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        EnsureSetExists(marcher, set);

        bool alreadyConfirmed = marcher.HasPositionAtCount(set, count) && marcher.countPositions[set][count].IsConfirmed;
        PositionEntry oldEntry = alreadyConfirmed ? marcher.countPositions[set][count] : new PositionEntry();

        Dictionary<(int, int), PositionEntry> beforeSnapshot = GetSnapshot(marcher, set, count);

        marcher.countPositions[set][count] = new PositionEntry(finalPos, "march");

        InterpolateAround(marcher, set, count, finalPos);

        RecordChanges(marcher, set, count, beforeSnapshot);

        marcher.SyncInspectorList();

        int visualizeSet = (set == 0) ? 1 : set;
        director?.VisualizePathsForSet(visualizeSet);
    }

    private void InterpolateAround(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        var interpolator = new MarcherInterpolator(marcher, getMaxCountForSet, () => marcher.transform.position);

        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos))
        {
            var backSteps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, finalPos, backSteps);
        }

        if (interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos))
        {
            var forwardSteps = interpolator.GetInterpolationSteps(set, count + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(finalPos, nextPos, forwardSteps);
        }
    }

    private Dictionary<(int, int), PositionEntry> GetSnapshot(MarcherPositionsManager marcher, int set, int count)
    {
        var snapshot = new Dictionary<(int, int), PositionEntry>();
        var interpolator = new MarcherInterpolator(marcher, getMaxCountForSet, () => marcher.transform.position);

        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out _) &&
            interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out _))
        {
            var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
            foreach (var (s, c) in steps)
            {
                if (marcher.countPositions.TryGetValue(s, out var counts) &&
                    counts.TryGetValue(c, out var entry))
                {
                    snapshot[(s, c)] = new PositionEntry(entry.pos, entry.type);
                }
            }
        }

        return snapshot;
    }

    private void RecordChanges(MarcherPositionsManager marcher, int set, int count, Dictionary<(int, int), PositionEntry> before)
    {
        var after = GetSnapshot(marcher, set, count);

        foreach (var kvp in before)
        {
            var (s, c) = kvp.Key;
            if (s == set && c == count) continue;

            var beforeEntry = kvp.Value;
            var afterEntry = after.ContainsKey((s, c)) ? after[(s, c)] : new PositionEntry();

            positionHistory.RecordChange(marcher, s, c, beforeEntry, afterEntry, MarcherPositionHistory.ChangeIntent.ConfirmedDelete);
        }
    }

    private void EnsureSetExists(MarcherPositionsManager marcher, int set)
    {
        if (!marcher.countPositions.ContainsKey(set))
            marcher.countPositions[set] = new Dictionary<int, PositionEntry>();
    }
}
