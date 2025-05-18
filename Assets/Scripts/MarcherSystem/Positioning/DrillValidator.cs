using System.Collections.Generic;
using UnityEngine;

public static class DrillValidator
{
    public static void RunValidation(IEnumerable<MarcherPositionsManager> marchers, Dictionary<int, RuntimeCacheSO.SetTimingData> setTimingMap)
    {
        CheckForMissingSetZero(marchers);
        CheckForEmptySets(marchers, setTimingMap);
        CheckForIsolatedMarchers(marchers);
        CheckForPathCrossings(marchers, setTimingMap);
    }

    private static void CheckForMissingSetZero(IEnumerable<MarcherPositionsManager> marchers)
    {
        foreach (var marcher in marchers)
        {
            if (!marcher.HasPositionAtCount(0, 0))
            {
                Debug.LogWarning($"❗ {marcher.name} is missing Set 0, Count 0 (initial dot).");
            }
        }
    }

    private static void CheckForEmptySets(IEnumerable<MarcherPositionsManager> marchers, Dictionary<int, RuntimeCacheSO.SetTimingData> setTimingMap)
    {
        foreach (var set in setTimingMap.Keys)
        {
            bool anyConfirmed = false;

            foreach (var marcher in marchers)
            {
                if (marcher.HasConfirmedOrInferredAtSet(set)) //GPT: we can use marcher.HasPositionAtCount(int setIndex, int countIndex);
                {
                    anyConfirmed = true;
                    break;
                }
            }

            if (!anyConfirmed)
            {
                Debug.LogWarning($"⚠️ Set {set} has no confirmed or inferred data for any marcher.");
            }
        }
    }
    

    private static void CheckForIsolatedMarchers(IEnumerable<MarcherPositionsManager> marchers)
    {
        foreach (var marcher in marchers)
        {
            bool hasData = false;

            foreach (var setEntry in marcher.countPositions.Values)
            {
                foreach (var entry in setEntry.Values)
                {
                    if (entry.IsConfirmed || entry.IsInferred)
                    {
                        hasData = true;
                        break;
                    }
                }

                if (hasData) break;
            }

            if (!hasData)
            {
                Debug.LogWarning($"🚫 {marcher.name} has no data in any set. Likely isolated.");
            }
        }
    }

    private static void CheckForPathCrossings(IEnumerable<MarcherPositionsManager> marchers, Dictionary<int, RuntimeCacheSO.SetTimingData> setTimingMap)
    {
        foreach (var set in setTimingMap.Keys)
        {
            int count = setTimingMap[set].count;

            // Check paths between set-1 last count and set current
            int fromSet = (set == 1) ? 0 : set - 1;
            int fromCount = (fromSet == 0) ? 0 : setTimingMap[fromSet].count;

            Dictionary<GameObject, Vector3> assignments = new Dictionary<GameObject, Vector3>();

            foreach (var marcher in marchers)
            {
                if (!marcher.HasPositionAtCount(fromSet, fromCount)) continue;
                if (!marcher.HasPositionAtCount(set, count)) continue;

                Vector3 start = marcher.GetPositionAtCount(fromSet, fromCount);
                Vector3 end = marcher.GetPositionAtCount(set, count);

                assignments[marcher.gameObject] = end;
            }

            var crossings = PathCrossingDetector.DetectCrossings(assignments);

            foreach (var pair in crossings)
            {
                Debug.LogWarning($"🔁 Crossing detected in Set {set}: {pair.Item1.name} ↔ {pair.Item2.name}");
            }
        }
    }

}

