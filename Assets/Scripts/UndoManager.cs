using UnityEngine;

/// <summary>
/// Centralized manager for global undo/redo hotkeys using MarcherPositionHistory.
/// Attach to a persistent scene object.
/// </summary>
public class UndoManager : MonoBehaviour
{
    [SerializeField] private MarcherPositionHistory positionHistory;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            if (positionHistory.CanUndo)
            {
                Debug.Log("↩️ Undoing last position change");
                positionHistory.Undo();
            }
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
        {
            if (positionHistory.CanRedo)
            {
                Debug.Log("↪️ Redoing last undone position change");
                positionHistory.Redo();
            }
        }
    }
}
