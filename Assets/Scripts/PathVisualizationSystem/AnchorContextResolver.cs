// AnchorContextResolver.cs
using UnityEngine;

public class AnchorContextResolver
{
    private readonly MarcherPositionsManager marcherPosManager;
    private readonly Transform marcherTransform;

    public AnchorContextResolver(MarcherPositionsManager marcherPosManager, Transform marcherTransform)
    {
        this.marcherPosManager = marcherPosManager;
        this.marcherTransform = marcherTransform;
    }

    public PathCacheModel ResolveAnchors(int set, int count)
    {
        var cache = new PathCacheModel();

        // 🧠 Fallback to previous set’s last count if no count is selected
        if (count <= 0)
        {
            if (set == 1)
            {
                set = 0;
                count = 0;
            }
            else
            {
                set -= 1;
                count = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(set, out var timing)
                    ? timing.count
                    : 8;
            }
        }

        // 🔄 Create interpolator helper using adjusted context
        var interpolator = new MarcherInterpolator(
            marcherPosManager,
            (setIndex) => SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8,
            () => marcherTransform.position
        );

        // 🔙 Previous confirmed
        if (interpolator.TryFindLastConfirmedPosition(set, count, out _, out _, out var prev))
        {
            cache.PreviousConfirmed = prev;
        }

        // 🔜 Next confirmed
        if (interpolator.TryFindNextConfirmedPosition(set, count, out _, out _, out var next))
        {
            cache.NextConfirmed = next;
        }

        // 🧷 Anchor confirmed
        if (set == 0 && count == 0 && marcherPosManager.HasPositionAtCount(0, 0))
        {
            cache.ActiveConfirmed = marcherPosManager.GetPositionAtCount(0, 0);
        }
        else if (marcherPosManager.HasPositionAtCount(set, count))
        {
            string tag = marcherPosManager.GetTagForCount(set, count);
            if (tag == "march" || tag == "inferred")
            {
                cache.ActiveConfirmed = marcherPosManager.GetPositionAtCount(set, count);
            }
        }

        return cache;
    }
}
