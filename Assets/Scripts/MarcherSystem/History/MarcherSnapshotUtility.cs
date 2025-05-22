using System.Collections.Generic;
using UnityEngine;
using System;

public static class MarcherSnapshotUtility
{
    public static PositionEntry CaptureBeforePositionType(MarcherPositionsManager marcher, int centerSet, int centerCount, Func<int, int> getMaxCountForSet)
    {
        if (marcher.countPositions.TryGetValue(centerSet, out var setMap) &&
            setMap.TryGetValue(centerCount, out var centerEntry))
        {
            return new PositionEntry(centerEntry.pos, centerEntry.type);
        }

        // ⛔ Dot is unset — record fallback as "unset"
        Vector3 fallbackPos = marcher.transform.position; // or Vector3.zero
        return new PositionEntry(fallbackPos, "unset");
    }

    public static PositionEntry CaptureAfterPositionType(MarcherPositionsManager marcher, int set, int count, Func<int, int> getMaxCountForSet)
    {
        // Case 1: Entry still exists — use it directly
        if (marcher.countPositions.TryGetValue(set, out var setMap) &&
            setMap.TryGetValue(count, out var currentEntry))
        {
            return new PositionEntry(currentEntry.pos, currentEntry.type);
        }

        // Case 2: Entry was removed — fallback to interpolator result
        var interpolator = new MarcherInterpolator(
            marcher,
            getMaxCountForSet,
            () => marcher.transform.position
        );

        // Attempt to locate the actual entry that GetBestPosition() would fall back to
        if (interpolator.TryFindLastConfirmedPosition(set, count, out int fallbackSet, out int fallbackCount, out Vector3 fallbackPos))
        {
            if (marcher.countPositions.TryGetValue(fallbackSet, out var fallbackMap) &&
                fallbackMap.TryGetValue(fallbackCount, out var fallbackEntry))
            {
                return new PositionEntry(fallbackEntry.pos, fallbackEntry.type); // ✅ Use actual confirmed type
            }
        }

        // Final fallback — no valid position, treat as inferred
        Vector3 inferredPos = interpolator.GetBestPosition(set, count);
        return new PositionEntry(inferredPos, "inferred");
    }

    public static MarcherPositionHistory.ChangeIntent DetermineConfirmedIntentWithLivePosition(Vector3 livePosition, PositionEntry beforeEntry)
    {
        float dist = Vector3.Distance(beforeEntry.pos, livePosition);
        bool isSamePosition = dist < 0.01f;

        if (beforeEntry.IsUnset)
        {
            Debug.Log("✅ [ConfirmedIntent] Raw ➝ Confirmed → ChangeIntent.ConfirmedMoveOnRaw");
            return MarcherPositionHistory.ChangeIntent.ConfirmedMoveOnRaw;
        }

        if (beforeEntry.IsInferred)
        {
            if (isSamePosition)
            {
                Debug.Log("✅ [ConfirmedIntent] Inferred ➝ Confirmed (same position) → ChangeIntent.ConfirmedMoveOnInferred");
                return MarcherPositionHistory.ChangeIntent.ConfirmedMoveOnInferred;
            }
            else
            {
                Debug.Log("✅ [ConfirmedIntent] Inferred ➝ Confirmed (moved) → ChangeIntent.ConfirmedMoveOnRaw");
                return MarcherPositionHistory.ChangeIntent.ConfirmedMoveOnRaw;
            }
        }

        if (beforeEntry.IsConfirmed)
        {
            if (isSamePosition)
            {
                Debug.Log("✅ [ConfirmedIntent] Confirmed ➝ Confirmed (same) → ChangeIntent.ConfirmedMoveOnConfirmed");
                return MarcherPositionHistory.ChangeIntent.ConfirmedMoveOnConfirmed;
            }
            else
            {
                Debug.Log("✅ [ConfirmedIntent] Confirmed ➝ Confirmed (moved) → ChangeIntent.ConfirmedMoveOnRaw");
                return MarcherPositionHistory.ChangeIntent.ConfirmedMoveOnRaw;
            }
        }

        Debug.LogWarning($"❓ [ConfirmedIntent] Unexpected transition: {beforeEntry.type}");
        return MarcherPositionHistory.ChangeIntent.Unknown;
    }


    public static MarcherPositionHistory.ChangeIntent DetermineDeletedIntent(PositionEntry before, PositionEntry after)
    {
        if (before.IsConfirmed && after.IsInferred)
        {
            Debug.Log($"✅ [DeletedIntent] Confirmed ➝ Inferred → ChangeIntent.ConfirmedDeleteOnInferred");
            return MarcherPositionHistory.ChangeIntent.ConfirmedDeleteOnInferred;
        }

        if (before.IsConfirmed && after.IsConfirmed)
        {
            Debug.Log($"✅ [DeletedIntent] Confirmed ➝ Confirmed → ChangeIntent.ConfirmedDeleteOnConfirmed");
            return MarcherPositionHistory.ChangeIntent.ConfirmedDeleteOnConfirmed;
        }

        Debug.LogWarning($"❓ [DeletedIntent] Unexpected transition: {before.type} ➝ {after.type}");
        return MarcherPositionHistory.ChangeIntent.Unknown;
    }
    
    public static MarcherPositionHistory.ChangeIntent DetermineRawMovementIntent(MarcherPositionsManager marcher, int set, int count, Vector3 beforePos)
    {
        PositionEntry entry = default;
        bool hasEntry = marcher.countPositions.TryGetValue(set, out var setMap) &&
                        setMap.TryGetValue(count, out entry);

        if (!hasEntry)
        {
            Debug.Log("🟢 RawMoveOnRaw: no entry at this count.");
            return MarcherPositionHistory.ChangeIntent.RawMoveOnRaw;            
        }
        else if (entry.IsConfirmed && Vector3.Distance(beforePos, entry.pos) < 0.01f)
        {
            Debug.Log("🟢 RawMoveOnConfirmed: position matched confirmed entry.");
            return MarcherPositionHistory.ChangeIntent.RawMoveOnConfirmed;            
        }
        else if (entry.IsInferred && Vector3.Distance(beforePos, entry.pos) < 0.01f)
        {
            Debug.Log("🟢 RawMoveOnInferred: position matched inferred entry.");
            return MarcherPositionHistory.ChangeIntent.RawMoveOnInferred;            
        }
        else
        {
            Debug.Log("🟢 RawMoveOnRaw: position differs from any known dot.");
            return MarcherPositionHistory.ChangeIntent.RawMoveOnRaw;            
        }
    }
}
