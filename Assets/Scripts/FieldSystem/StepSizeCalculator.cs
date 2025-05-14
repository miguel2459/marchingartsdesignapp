using UnityEngine;

/// <summary>
/// Provides color feedback based on marcher step size.
/// </summary>
public static class StepSizeCalculator
{
    public static Color GetStepSizeColor(float distance, int countSpan)
    {
        if (countSpan <= 0)
            return Color.magenta; // error fallback

        float stepSize = distance / countSpan; // yards per step

        if (stepSize < 0.625f)
            return new Color(0f, 0.81f, 1f, 1f); // 🔵 Blue = 8/5 or smaller

        if (stepSize < 0.75f)
            return Color.green; // 🟢 Comfortable range

        if (stepSize < 0.9f)
            return Color.yellow; // 🟡 Stretching

        return Color.red; // 🔴 Overextending
    }
}
