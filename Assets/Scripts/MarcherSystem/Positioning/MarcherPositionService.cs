using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central service to handle confirming, deleting, and interpolating marcher positions.
/// </summary>
public class MarcherPositionService : MonoBehaviour
{
    public event Action OnAnyMarcherPositionUpdated = delegate { };

    [SerializeField] private EnsembleSessionLoader sessionLoader;
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private EnsembleDirector2 director;
    private MarcherPositionConfirmer confirmer;
    private MarcherPositionDeleter deleter;

    private void Awake()
    {
        confirmer = new MarcherPositionConfirmer(director, positionHistory, GetMaxCountForSet);
        deleter   = new MarcherPositionDeleter(director, positionHistory, GetMaxCountForSet);
    }

    /// <summary>
    /// Confirm a "march" tag and interpolate around it.
    /// </summary>
    public void ConfirmMarcherPosition(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        confirmer.isUndoContext = false;
        confirmer.Confirm(marcher, set, count, finalPos);
        OnAnyMarcherPositionUpdated?.Invoke();
    }

    public void ConfirmFromUndo(MarcherPositionsManager marcher, int set, int count, Vector3 finalPos)
    {
        confirmer.isUndoContext = true;
        confirmer.Confirm(marcher, set, count, finalPos);
        confirmer.isUndoContext = false;
        OnAnyMarcherPositionUpdated?.Invoke();
    }

    public bool DeleteConfirmedPosition(MarcherPositionsManager marcher, int set, int count, out bool hadNext)
    {
        deleter.isUndoContext = false;
        bool result = deleter.Delete(marcher, set, count, out hadNext);
        OnAnyMarcherPositionUpdated?.Invoke();
        return result;
    }

    public bool DeleteFromUndo(MarcherPositionsManager marcher, int set, int count, out bool hadNextConfirmed)
    {
        deleter.isUndoContext = true;
        bool result = deleter.Delete(marcher, set, count, out hadNextConfirmed);
        deleter.isUndoContext = false;
        OnAnyMarcherPositionUpdated?.Invoke();
        return result;
    }

    public int GetMaxCountForSet(int setIndex)
    {
        return sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count : 8;
    }
}
