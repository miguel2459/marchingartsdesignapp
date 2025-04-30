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
    [SerializeField] private EnsembleDirector2 director;

    /// <summary>
    /// Confirm a "march" tag and interpolate around it.
    /// </summary>
    public void ConfirmMarcherPosition(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        EnsureSetExists(marcher, set);

        PositionEntry before = marcher.HasPositionAtCount(set, count)
            ? marcher.countPositions[set][count]
            : new PositionEntry();

        marcher.countPositions[set][count] = new PositionEntry(finalPos, "march");

        positionHistory.RecordChange(marcher, set, count, before, marcher.countPositions[set][count]);

        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcher,
            GetMaxCountForSet,
            () => marcher.transform.position
        );

        // === BACKWARD INTERPOLATION ===
        if (interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos))
        {
            var backSteps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, finalPos, backSteps);
        }

        // === FORWARD INTERPOLATION ===
        if (interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos))
        {
            var forwardSteps = interpolator.GetInterpolationSteps(set, count + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(finalPos, nextPos, forwardSteps);
        }

        SyncAndNotify(marcher);

        // 🔁 Visualize next affected set (nextSet or set+1 fallback)
        int visualizeSet = (set == 0) ? 1 : set;
        if (director != null)
            director.VisualizePathsForSet(visualizeSet);
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
        // ✅ Move marcher to their new interpolated position if one was filled in
        if (marcher.HasPositionAtCount(set, count))
        {
            Vector3 newPos = marcher.GetPositionAtCount(set, count);
            marcher.transform.position = newPos;

            if (marcher.TryGetComponent<MarcherVisualStateController>(out var visual))
                visual.SetSelectorVisible(false); // optional: reset selector
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
