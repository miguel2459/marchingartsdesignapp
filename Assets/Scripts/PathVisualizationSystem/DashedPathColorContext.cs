using UnityEngine;

/// <summary>
/// Determines the appropriate colors for forward and backward dashed lines based on marcher context
/// </summary>
public static class DashedPathColorContext
{
    // Define your color palette here
    public static readonly Color LightGrey = new Color(0.5f, 0.5f, 0.5f, 1f);
    public static readonly Color Blue = new Color(0f, 0.812f, 1f, 1f);  // #00CFFF
    public static readonly Color White = Color.white;

    public enum CountContext
    {
        NoActiveCount,    // At set with no active count
        WithinSet,        // At count within current set
        LastCount         // At last count in current set
    }

    /// <summary>
    /// Determines the count context based on current set and count information
    /// </summary>
    public static CountContext GetCountContext(int currentSet, int currentCount, int totalCountsInSet)
    {
        // No active count (count <= 0)
        if (currentCount <= 0)
        {
            return CountContext.NoActiveCount;
        }

        // At last count in set
        if (currentCount >= totalCountsInSet)
        {
            return CountContext.LastCount;
        }

        // Within the set
        return CountContext.WithinSet;
    }

    /// <summary>
    /// Gets the appropriate colors for forward and backward lines based on context
    /// </summary>
    public static (Color backward, Color forward) GetDashedLineColors(CountContext context)
    {
        return context switch
        {
            CountContext.NoActiveCount => (LightGrey, Blue),     // light grey back, blue forward
            CountContext.WithinSet => (Blue, Blue),             // blue back, blue forward
            CountContext.LastCount => (Blue, White),            // blue back, white forward
            _ => (LightGrey, Blue)                               // fallback
        };
    }

    /// <summary>
    /// Convenience method that combines context determination and color selection
    /// </summary>
    public static (Color backward, Color forward) GetDashedLineColors(int currentSet, int currentCount, int totalCountsInSet)
    {
        var context = GetCountContext(currentSet, currentCount, totalCountsInSet);
        return GetDashedLineColors(context);
    }
}