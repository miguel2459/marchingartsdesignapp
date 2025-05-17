using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks a history of position changes for undo/redo operations.
/// </summary>
public class MarcherPositionHistory : MonoBehaviour
{
    private Stack<List<PositionChange>> undoStack = new Stack<List<PositionChange>>();
    private Stack<List<PositionChange>> redoStack = new Stack<List<PositionChange>>();
    private List<PositionChange> currentBatch = null;
    public MarcherPositionService marcherService;

    private int activeSet = -1;
    private int activeCount = -1;

    public void SetActiveEditContext(int set, int count)
    {
        if (set != activeSet || count != activeCount)
        {
            EndBatch(); // Finalize any pending edits
            undoStack.Clear(); // 🔥 Clear undo history for prior context
            redoStack.Clear(); // 🔁 Clear redo history too
            activeSet = set;
            activeCount = count;
            Debug.Log($"🔁 Switched active edit context → Set {set}, Count {count}. Cleared previous undo history.");
        }
    }


    public void BeginBatch()
    {
        currentBatch = new List<PositionChange>();
    }

    public void EndBatch()
    {
        if (currentBatch != null && currentBatch.Count > 0)
        {
            undoStack.Push(currentBatch);
            redoStack.Clear();
        }
        currentBatch = null;
    }

    public void RecordChange(MarcherPositionsManager marcher, int set, int count,
                         PositionEntry before, PositionEntry after,
                         ChangeIntent intent = ChangeIntent.Unknown)
    {
        if (set != activeSet || count != activeCount)
        {
            //Debug.LogWarning($"⚠️ Skipped change — outside current edit context (Set {set}, Count {count})");
            return;
        }

        var change = new PositionChange
        {
            marcher = marcher,
            set = set,
            count = count,
            before = before,
            after = after,
            intent = intent
        };

        if (currentBatch != null)
            currentBatch.Add(change);
        else
            undoStack.Push(new List<PositionChange> { change });

        redoStack.Clear();
    }



    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void Undo()
    {
        if (!CanUndo) return;
        var changeSet = undoStack.Pop();
        foreach (var change in changeSet)
            ApplyEntry(change.marcher, change.set, change.count, change.before, change.intent);
        redoStack.Push(changeSet);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var changeSet = redoStack.Pop();
        foreach (var change in changeSet)
            ApplyEntry(change.marcher, change.set, change.count, change.after, change.intent);
        undoStack.Push(changeSet);
    }


    private void ApplyEntry(MarcherPositionsManager marcher, int set, int count, PositionEntry entry, ChangeIntent intent)
    {
        Debug.Log($"🧭 ApplyEntry invoked for {marcher.name} | Set {set}, Count {count} | Intent: {intent}");

        switch (intent)
        {
            case ChangeIntent.RawMoveOnConfirmed:
                marcher.transform.position = entry.pos;
                Debug.Log($"↩️ Raw transform-only undo: moved {marcher.name} to {entry.pos} at Set {set}, Count {count}");
                break;

            case ChangeIntent.ConfirmedMove:
            case ChangeIntent.Reconfirm:
                if (entry.IsConfirmed)
                {
                    Debug.Log($"✅ Reapplying confirmed entry at Set {set}, Count {count} | Type: {entry.type}");

                    marcher.countPositions[set][count] = entry;
                    marcher.transform.position = entry.pos;

                    Debug.Log("🔄 Calling ReinterpolateAround() after confirmed reentry...");
                    ReinterpolateAround(marcher, set, count);
                    if (marcher.TryGetComponent(out Unit unit))
                    {
                        bool isHolding = entry.type == "hold";
                        unit.SetSelector(true, isHolding);
                    }
                }
                else
                {
                    Debug.Log($"❌ Entry is no longer confirmed, removing and recalculating inferred positions at Set {set}, Count {count}");

                    if (marcher.countPositions.ContainsKey(set))
                    {
                        marcher.countPositions[set].Remove(count);
                        Debug.Log("🗑️ Removed unconfirmed dot from countPositions");
                    }

                    Debug.Log("🔁 Calling HandlePostConfirmedUndo()...");
                    HandlePostConfirmedUndo(marcher, set, count);
                }

                Debug.Log("📦 Syncing inspector list after position update...");
                marcher.SyncInspectorList();

                Debug.Log("📡 Raising global OnAnyMarcherPositionUpdated event...");
                MarcherPositionsManager.RaisePositionUpdatedEvent();
                break;

            case ChangeIntent.ConfirmedDelete:
                if (marcherService != null)
                {
                    Debug.Log($"🗑️ ConfirmedDelete — attempting to delete {marcher.name} at Set {set}, Count {count}");

                    bool hadNext;
                    bool success = marcherService.DeleteConfirmedPosition(marcher, set, count, out hadNext);

                    Debug.Log($"🧹 Result of DeleteConfirmedPosition → Success: {success}, Had Next Dot: {hadNext}");
                }
                break;

            default:
                Debug.LogWarning($"❓ Unknown ChangeIntent '{intent}' — falling back to raw application.");
                marcher.countPositions[set][count] = entry;
                marcher.transform.position = entry.pos;

                marcher.SyncInspectorList();
                MarcherPositionsManager.RaisePositionUpdatedEvent();
                break;
        }
        // 🔕 Disable any dashed previews before state re-application
        if (marcher.TryGetComponent(out MarcherDashedPathCoordinator coord))
        {
            coord.StopDashedPreview();
            //coord.DisableDashedPreview();
        }
    }


    private void HandlePostConfirmedUndo(MarcherPositionsManager marcher, int set, int count)
    {
        var interpolator = new MarcherInterpolator(marcher, marcherService.GetMaxCountForSet);

        bool hasPrev = interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos);
        bool hasNext = interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos);

        if (hasPrev && hasNext)
        {
            var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, nextPos, steps);
            Debug.Log($"🔁 Reinterpolated after undo for Set {set}, Count {count} between {prevSet}:{prevCount} and {nextSet}:{nextCount}");

            if (marcher.HasPositionAtCount(set, count))
                marcher.transform.position = marcher.GetPositionAtCount(set, count);
        }
        else if (hasPrev && !hasNext)
        {
            // ✅ No next — just clean interpolated values between this and previous
            var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);
            foreach (var (s, c) in steps)
            {
                if (marcher.countPositions.TryGetValue(s, out var map) && map.TryGetValue(c, out var entry) && entry.IsInferred)
                {
                    map.Remove(c);
                    Debug.Log($"❌ Removed inferred after undo — Set {s}, Count {c}");
                }
            }

            marcher.transform.position = prevPos;
        }
        else
        {
            Debug.Log("🛑 No previous or next confirmed position. Leaving marcher where they are.");
        }
    }

    private void ReinterpolateAround(MarcherPositionsManager marcher, int set, int count)
    {
        var interpolator = new MarcherInterpolator(marcher, marcherService.GetMaxCountForSet);

        bool hasPrev = interpolator.TryFindLastConfirmedPosition(set, count, out int prevSet, out int prevCount, out Vector3 prevPos);
        bool hasNext = interpolator.TryFindNextConfirmedPosition(set, count, out int nextSet, out int nextCount, out Vector3 nextPos);

        Vector3 current = marcher.countPositions[set][count].pos;

        if (hasPrev)
        {
            var steps = interpolator.GetInterpolationSteps(prevSet, prevCount + 1, set, count - 1);
            interpolator.ApplyInterpolatedPositions(prevPos, current, steps);
        }

        if (hasNext)
        {
            var steps = interpolator.GetInterpolationSteps(set, count + 1, nextSet, nextCount - 1);
            interpolator.ApplyInterpolatedPositions(current, nextPos, steps);
        }
    }

    public void RecordRawMovement(MarcherPositionsManager marcher, Vector3 before, Vector3 after)
    {
        if (activeSet < 0 || activeCount < 0)
        {
            Debug.LogWarning("⚠️ Cannot record raw movement — active edit context not set.");
            return;
        }
        bool isConfirmedAtCount = marcher.TryGetConfirmedPosition(activeSet, activeCount, out _);                                

        var intent = isConfirmedAtCount ? ChangeIntent.RawMoveOnConfirmed : ChangeIntent.RawMove;

        RecordChange(
            marcher,
            activeSet,
            activeCount,
            new PositionEntry(before, "transform_only"),
            new PositionEntry(after, "transform_only"),
            intent
        );
    }


    public enum ChangeIntent
    {
        RawMove,
        RawMoveOnConfirmed,
        ConfirmedMove,
        ConfirmedDelete,
        Reconfirm,
        Unknown
    }

    private struct PositionChange
    {
        public MarcherPositionsManager marcher;
        public int set;
        public int count;
        public PositionEntry before;
        public PositionEntry after;
        public ChangeIntent intent;
    }
}
