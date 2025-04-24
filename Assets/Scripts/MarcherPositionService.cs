using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central service to handle confirming, deleting, and interpolating marcher positions.
/// </summary>
public class MarcherPositionService : MonoBehaviour
{
    public event Action OnAnyMarcherPositionUpdated = delegate { };

    [SerializeField] private EnsembleSessionLoader sessionLoader;
    [SerializeField] private MarcherPositionHistory positionHistory;

    /// <summary>
    /// Confirm a "march" tag and interpolate around it.
    /// </summary>
    public void ConfirmMarcherPosition(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        if (set == 0 && count == 0)
        {
            EnsureSetExists(marcher, 0);
            PositionEntry before = marcher.HasPositionAtCount(set, count)
                ? marcher.countPositions[set][count]
                : new PositionEntry();

            positionHistory.RecordChange(marcher, set, count, before, new PositionEntry(finalPos, "march"));

            marcher.countPositions[0][0] = new PositionEntry(finalPos, "march");
            SyncAndNotify(marcher);
            return;
        }

        EnsureSetExists(marcher, set);
        marcher.countPositions[set][count] = new PositionEntry(finalPos, "march");

        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcher,
            GetMaxCountForSet,
            () => marcher.transform.position
        );

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

        SyncAndNotify(marcher);
    }

    /// <summary>
    /// Delete a confirmed dot and reinterpolate the surrounding area.
    /// </summary>
    public void DeleteConfirmedPosition(MarcherPositionsManager marcher, int set, int count)
    {
        int lastCount = GetMaxCountForSet(set);
        if (count == lastCount) return;

        if (!marcher.HasPositionAtCount(set, count) || !marcher.countPositions[set][count].IsConfirmed)
            return;
            
        PositionEntry oldEntry = marcher.countPositions[set][count];
        positionHistory.RecordChange(marcher, set, count, oldEntry, new PositionEntry());

        marcher.countPositions[set].Remove(count);

        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcher,
            GetMaxCountForSet,
            () => marcher.transform.position
        );

        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos) &&
            interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos))
        {
            var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, nextPos, steps);
        }

        SyncAndNotify(marcher);
    }

    /// <summary>
    /// Ensure set exists in the dictionary before writing.
    /// </summary>
    private void EnsureSetExists(MarcherPositionsManager marcher, int set)
    {
        if (!marcher.countPositions.ContainsKey(set))
            marcher.countPositions[set] = new Dictionary<int, PositionEntry>();
    }

    private int GetMaxCountForSet(int setIndex)
    {
        return sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count : 8;
    }

    private void SyncAndNotify(MarcherPositionsManager marcher)
    {
        marcher.SyncInspectorList();
        OnAnyMarcherPositionUpdated.Invoke();
    }
}
