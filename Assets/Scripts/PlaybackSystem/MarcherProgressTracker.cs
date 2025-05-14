using System;
using System.Collections.Generic;

public class MarcherProgressTracker
{
    private readonly IReadOnlyList<MarcherPositionsManager> marchers;
    private readonly Dictionary<int, RuntimeCacheSO.SetTimingData> timingMap;

    public event Action<int,float> OnSetPercentChanged = delegate { };

    // cache to avoid needless callbacks every frame
    private readonly Dictionary<int,float> cachedPercent = new Dictionary<int, float>();

    public MarcherProgressTracker(IReadOnlyList<MarcherPositionsManager> marchers,
                                  Dictionary<int, RuntimeCacheSO.SetTimingData> timingMap)
    {
        this.marchers  = marchers;
        this.timingMap = timingMap;
    }

    /// Call whenever any position data changes.
    public void Refresh()
    {
        foreach (var kvp in timingMap)
        {
            int setIndex  = kvp.Key;
            int countsPerSet = kvp.Value.count;
            int totalCounts  = marchers.Count * countsPerSet;

            int completed = 0;
            foreach (var m in marchers)
            {
                if (!m.countPositions.TryGetValue(setIndex, out var countDict)) continue;

                for (int c = 1; c <= countsPerSet; c++)
                {
                    if (countDict.TryGetValue(c, out var entry) &&
                        (entry.IsConfirmed || entry.IsInferred))
                    {
                        completed++;
                    }
                }
            }

            float percent = totalCounts > 0 ? (float)completed / totalCounts : 0f;

            if (!cachedPercent.TryGetValue(setIndex, out float prev) || Math.Abs(prev - percent) > 0.001f)
            {
                cachedPercent[setIndex] = percent;
                OnSetPercentChanged.Invoke(setIndex, percent);
            }
        }
    }

    public float GetPercent(int set) => cachedPercent.TryGetValue(set, out var p) ? p : 0f;
}
