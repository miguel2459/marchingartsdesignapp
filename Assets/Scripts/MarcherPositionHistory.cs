using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks a history of position changes for undo/redo operations.
/// </summary>
public class MarcherPositionHistory : MonoBehaviour
{
    private Stack<PositionChange> undoStack = new Stack<PositionChange>();
    private Stack<PositionChange> redoStack = new Stack<PositionChange>();

    public void RecordChange(MarcherPositionsManager marcher, int set, int count, PositionEntry before, PositionEntry after)
    {
        var change = new PositionChange
        {
            marcher = marcher,
            set = set,
            count = count,
            before = before,
            after = after
        };

        undoStack.Push(change);
        redoStack.Clear(); // Clear redo stack on new change
    }

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void Undo()
    {
        if (!CanUndo) return;

        var change = undoStack.Pop();
        ApplyEntry(change.marcher, change.set, change.count, change.before);
        redoStack.Push(change);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var change = redoStack.Pop();
        ApplyEntry(change.marcher, change.set, change.count, change.after);
        undoStack.Push(change);
    }

    private void ApplyEntry(MarcherPositionsManager marcher, int set, int count, PositionEntry entry)
    {
        if (!marcher.countPositions.ContainsKey(set))
            marcher.countPositions[set] = new Dictionary<int, PositionEntry>();

        marcher.countPositions[set][count] = entry;
        marcher.SyncInspectorList();
        MarcherPositionsManager.RaisePositionUpdatedEvent();
    }

    private struct PositionChange
    {
        public MarcherPositionsManager marcher;
        public int set;
        public int count;
        public PositionEntry before;
        public PositionEntry after;
    }
}
