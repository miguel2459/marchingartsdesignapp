using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/// <summary>
/// Tracks a history of position changes for undo/redo operations.
/// </summary>
public class MarcherPositionHistory : MonoBehaviour
{
    private Stack<List<PositionChange>> undoStack = new Stack<List<PositionChange>>();
    private Stack<List<PositionChange>> redoStack = new Stack<List<PositionChange>>();
    private List<PositionChange> currentBatch = null;
    public MarcherPositionService marcherService;
    public static event System.Action OnHistoryContextChanged;

    private int activeSet = -1;
    private int activeCount = -1;
    
    public int GetUndoCount() => undoStack.Count;
    public int GetRedoCount() => redoStack.Count;

    public void SetActiveEditContext(int set, int count)
    {
        if (set != activeSet || count != activeCount)
        {
            EndBatch();
            undoStack.Clear();
            redoStack.Clear();
            DebugUndo.Clear(); // ✅ Fix: clear debug previews too
            DebugRedo.Clear();
            activeSet = set;
            activeCount = count;

            OnHistoryContextChanged?.Invoke(); // 🧠 Notify editor

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
#endif
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
            DebugUndo.Insert(0, new List<PositionChange>(currentBatch)); // newest on top
            redoStack.Clear();
            DebugRedo.Clear();
        }
        currentBatch = null;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
        #endif
    }

    public void RecordChange(MarcherPositionsManager marcher, int set, int count, PositionEntry before, PositionEntry after, ChangeIntent intent = ChangeIntent.Unknown)
    {
        if (set != activeSet || count != activeCount)
        {
            Debug.LogWarning($"⚠️ Skipped change — outside current edit context (Set {set}, Count {count})");
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
        redoStack.Push(changeSet);

        DebugUndo.RemoveAt(0);
        DebugRedo.Insert(0, changeSet);

        foreach (var change in changeSet)
            ApplyUndo(change.marcher, change.set, change.count, change.before, change.intent);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var changeSet = redoStack.Pop();
        undoStack.Push(changeSet);

        DebugRedo.RemoveAt(0);
        DebugUndo.Insert(0, changeSet);

        foreach (var change in changeSet)
            ApplyRedo(change.marcher, change.set, change.count, change.after, change.intent);
    }

    private void ApplyUndo(MarcherPositionsManager marcher, int set, int count, PositionEntry entry, ChangeIntent intent)
    {
        Debug.Log($"🧭 ApplyEntry invoked for {marcher.name} | Set {set}, Count {count} | Intent: {intent}");

        switch (intent)
        {
            // Re-apply a previously deleted confirmed dot
            case ChangeIntent.ConfirmedDeleteOnInferred:
                UndoPositionCaseHandlers.HandleDeleteOnInferred(marcher, set, count, entry);
                break;
            case ChangeIntent.ConfirmedDeleteOnConfirmed:
                UndoPositionCaseHandlers.HandleDeleteOnConfirmed(marcher, set, count, entry);
                break;

            // Re-delete a previously confirmed dot (revert a confirmation)
            case ChangeIntent.ConfirmedMoveOnRaw:
                UndoPositionCaseHandlers.HandleConfirmedMoveOnRaw(marcher, set, count, entry);
                break;
            case ChangeIntent.ConfirmedMoveOnInferred:
                UndoPositionCaseHandlers.HandleConfirmedMoveOnInferred(marcher, set, count, entry);
                break;
            case ChangeIntent.ConfirmedMoveOnConfirmed:
                UndoPositionCaseHandlers.HandleConfirmedMoveOnConfirmed(marcher, set, count, entry);
                break;

            // Handle raw move reversion manually
            case ChangeIntent.RawMoveOnConfirmed:
                UndoPositionCaseHandlers.HandleRawMoveOnConfirmed(marcher, set, count, entry);
                break;
            case ChangeIntent.RawMoveOnInferred:
                UndoPositionCaseHandlers.HandleRawMoveOnInferred(marcher, set, count, entry);
                break;
            case ChangeIntent.RawMoveOnRaw:
                UndoPositionCaseHandlers.HandleRawMoveOnRaw(marcher, set, count, entry);
                break;

            default:
                Debug.LogWarning($"❓ Unhandled ChangeIntent: {intent}");
                break;
        }

        marcher.SyncInspectorList();
        MarcherPositionsManager.RaisePositionUpdatedEvent();
    }

    private void ApplyRedo(MarcherPositionsManager marcher, int set, int count, PositionEntry after, ChangeIntent intent)
    {
        Debug.Log($"↪️ Redo | Set {set}, Count {count} | Intent: {intent}");

        switch (intent)
        {
            case ChangeIntent.ConfirmedDeleteOnConfirmed:
                RedoPositionCaseHandlers.HandleConfirmedDeleteOnConfirmed(marcher, set, count, after);
                break;
            case ChangeIntent.ConfirmedDeleteOnInferred:
                RedoPositionCaseHandlers.HandleConfirmedDeleteOnInferred(marcher, set, count, after); // re-deletion behaves like confirm-on-raw
                break;

            case ChangeIntent.ConfirmedMoveOnRaw:
                RedoPositionCaseHandlers.HandleConfirmedMoveOnRaw(marcher, set, count, after);
                break;
            case ChangeIntent.ConfirmedMoveOnInferred:
                RedoPositionCaseHandlers.HandleConfirmedMoveOnInferred(marcher, set, count, after);
                break;
            case ChangeIntent.ConfirmedMoveOnConfirmed:
                RedoPositionCaseHandlers.HandleConfirmedMoveOnConfirmed(marcher, set, count, after);
                break;

            case ChangeIntent.RawMoveOnConfirmed:
                RedoPositionCaseHandlers.HandleRawMoveOnConfirmed(marcher, set, count, after);
                break;
            case ChangeIntent.RawMoveOnInferred:
                RedoPositionCaseHandlers.HandleRawMoveOnInferred(marcher, set, count, after);
                break;
            case ChangeIntent.RawMoveOnRaw:
                RedoPositionCaseHandlers.HandleRawMoveOnRaw(marcher, set, count, after);
                break;

            default:
                Debug.LogWarning($"❓ [Redo] Unknown ChangeIntent: {intent}");
                break;
        }

        marcher.SyncInspectorList();
        MarcherPositionsManager.RaisePositionUpdatedEvent();
    }

    public void RecordRawMovement(MarcherPositionsManager marcher, Vector3 before, Vector3 after)
    {
        if (activeSet < 0 || activeCount < 0)
        {
            Debug.LogWarning("⚠️ Cannot record raw movement — active edit context not set.");
            return;
        }

        var intent = MarcherSnapshotUtility.DetermineRawMovementIntent(marcher, activeSet, activeCount, before);

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
        RawMoveOnRaw,
        RawMoveOnConfirmed,
        RawMoveOnInferred,
        ConfirmedMoveOnConfirmed,
        ConfirmedMoveOnInferred,
        ConfirmedMoveOnRaw,
        ConfirmedDeleteOnConfirmed,
        ConfirmedDeleteOnInferred,
        Unknown
    }

    //[SerializeField]
    public List<List<PositionChange>> DebugUndo = new List<List<PositionChange>>();

    //[SerializeField]
    public List<List<PositionChange>> DebugRedo = new List<List<PositionChange>>();

    [System.Serializable]
    public struct PositionChange
    {
        public MarcherPositionsManager marcher;
        public int set;
        public int count;
        public PositionEntry before;
        public PositionEntry after;
        public ChangeIntent intent;
    }
    
    // private void ApplyEntry(MarcherPositionsManager marcher, int set, int count, PositionEntry entry, ChangeIntent intent)
    // {
    //     Debug.Log($"🧭 ApplyEntry invoked for {marcher.name} | Set {set}, Count {count} | Intent: {intent}");

    //     switch (intent)
    //     {
    //         case ChangeIntent.ConfirmedMoveOnConfirmed: 
    //             //Delete after confirmedPosition
    //             //Return marcher to before confirmedPosition
    //             //Reinterpolate path(s) depends on if no next confirmed
    //             //Recache and Reupdate dashed path lines, turn off
    //             //Make sure Unit Selector Ring is attached to marcher and turned on
    //             break;

    //         case ChangeIntent.ConfirmedMoveOnInferred:
    //             //Delte after confirmedPosition
    //             //Return marcher to before inferredPosition
    //             //Reinterpolate path(s) depends on if no next confirmed
    //             //Recache and Reupdate dashed path lines, turned off
    //             //Make sure unit Selector is attached, but turned off
    //             break;

    //         case ChangeIntent.ConfirmedMoveOnRaw:
    //             //Delete after confirmedPosition
    //             //Return marcher to before rawPosition
    //             //Remove interpolated path(s) depends on if no next confirmed
    //             //Recache and Reupdate dashed path lines, turned on
    //             //Make sure unit Selector is attached, but turned off
    //             break;

    //         case ChangeIntent.ConfirmedDeleteOnInferred:
    //             //Reconfirm before confirmedPosition
    //             //Reinterpolate path(s) depends on if no next confirmed
    //             //Recache and Reupdate dashed path lines, turned off
    //             //Make sure Unit Selector Ring is attached to marcher and turned on
    //             break;

    //         case ChangeIntent.ConfirmedDeleteOnRaw:
    //             //Reconfirm before confirmedPosition
    //             //Reinterpolate path(s) depends on if no next confirmed
    //             //Recache and Reupdate dashed path lines, turned off
    //             //Make sure Unit Selector Ring is attached to marcher and turned on
    //             break;

    //         case ChangeIntent.RawMoveOnConfirmed:
    //             //Return marcher to before confirmedPosition
    //             //Do not interpolate
    //             //Recache and Reupdate dashed path lines, turned off
    //             //Make sure Unit Selector Ring is not attached to marcher and turned on at confirmed position
    //             break;

    //         case ChangeIntent.RawMoveOnInferred:
    //             //Return marcher to before inferredPosition
    //             //Recache and Reupdate dashed path lines, turned off
    //             //Make sure Unit Selector Ring is attached to marcher and turned off
    //             break;

    //         case ChangeIntent.RawMoveOnRaw:
    //             //Return marcher to before rawPosition
    //             //Recache and Reupdate dashed path lines, turned on
    //             //Make sure Unit Selector Ring is attached to marcher and turned off
    //             break;

    //         default:
    //             //Idk how to handle this unkown case just yet.
    //             break;
    //     }

    //     Debug.Log("📦 Syncing inspector list after position update...");
    //     marcher.SyncInspectorList();

    //     Debug.Log("📡 Raising global OnAnyMarcherPositionUpdated event...");
    //     MarcherPositionsManager.RaisePositionUpdatedEvent();
    // }    
}
