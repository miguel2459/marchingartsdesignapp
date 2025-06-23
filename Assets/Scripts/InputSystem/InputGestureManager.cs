using System;
using UnityEngine;

/// <summary>
/// Centralized input manager that interprets raw key inputs and raises intent-based events.
/// This decouples input detection from gameplay logic.
/// </summary>
public class InputGestureManager : MonoBehaviour
{
    // Core action events
    public event Action OnConfirmDot;
    public event Action OnDeleteDot;
    public event Action OnDeleteMarcher;
    public event Action OnSnapToGrid;
    public event Action OnRespaceSmart;
    public event Action OnFocusCamera;
    public event Action OnSelectAll;

    private void Update()
    {
        // Confirm marcher position
        if (Input.GetKeyDown(KeyCode.Space))
            OnConfirmDot?.Invoke();

        // Delete marcher position or entire marcher
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (Input.GetKey(KeyCode.LeftControl))
                OnDeleteMarcher?.Invoke();
            else
                OnDeleteDot?.Invoke();
        }

        //Select all marchers
        if (Input.GetKeyDown(KeyCode.A))
            OnSelectAll?.Invoke();

        // Snap selected marchers to grid
        if (Input.GetKeyDown(KeyCode.G))
            OnSnapToGrid?.Invoke();

        // Apply smart box spacing + reshaping
        if (Input.GetKeyDown(KeyCode.B))
            OnRespaceSmart?.Invoke();

        // Refocus camera to current selection
        if (Input.GetKeyDown(KeyCode.F))
            OnFocusCamera?.Invoke();
    }
    
    // InputGestureManager.cs

    public void TriggerConfirmDotFromUI()
    {
        OnConfirmDot?.Invoke(); // behaves like pressing Spacebar
    }

    public void TriggerDeleteDotFromUI()
    {
        OnDeleteDot?.Invoke(); // behaves like pressing Backspace
    }

}
