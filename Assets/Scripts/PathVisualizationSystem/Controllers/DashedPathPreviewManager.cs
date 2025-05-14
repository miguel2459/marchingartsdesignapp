using System.Collections.Generic;
using UnityEngine;

public class DashedPathPreviewManager : MonoBehaviour
{
    private readonly List<MarcherDashedPathCoordinator> activeCoordinators = new List<MarcherDashedPathCoordinator>();
    private readonly Dictionary<GameObject, Vector3> initialPositions = new Dictionary<GameObject, Vector3>();

    private bool previewStarted = false;

    public void RegisterSelectedMarchers(List<GameObject> selected)
    {
        activeCoordinators.Clear();
        initialPositions.Clear();
        previewStarted = false;

        foreach (var marcher in selected)
        {
            if (marcher.TryGetComponent(out MarcherDashedPathCoordinator coord))
            {
                activeCoordinators.Add(coord);
                initialPositions[marcher] = marcher.transform.position;
            }
        }
    }

    public void UpdatePreviewCycle()
    {
        if (!previewStarted)
        {
            foreach (var kvp in initialPositions)
            {
                if (Vector3.Distance(kvp.Key.transform.position, kvp.Value) > 0.01f)
                {
                    StartPreview();
                    break;
                }
            }
        }
        else
        {
            foreach (var coord in activeCoordinators)
            {
                coord.UpdateDashedPreview(coord.transform.position);
            }
        }
    }

    public void DisableAllPreviews()
    {
        foreach (var coord in activeCoordinators)
        {
            coord.DisableDashedPreview();
            coord.StopDashedPreview(); // hides visuals
        }

        previewStarted = false;
    }

    private void StartPreview()
    {
        previewStarted = true;

        bool metronomeRunning = EnsembleDirector2.instance.metronome.IsRunning();

        foreach (var coord in activeCoordinators)
        {
            bool shouldAllowPreview =
                !metronomeRunning || // metronome is NOT running
                !EnsembleDirector2.instance.IsMarcherSelected(coord.gameObject); // OR marcher is NOT selected

            if (shouldAllowPreview)
            {
                coord.EnableDashedPreview();
                coord.StartPreview();
            }
            else
            {
                coord.DisableDashedPreview();
                coord.StopDashedPreview(); // make sure visuals are hidden
            }
        }
    }

    public void StopAllPreviews()
    {
        foreach (var coord in activeCoordinators)
            coord.StopDashedPreview();

        activeCoordinators.Clear();
        initialPositions.Clear();
        previewStarted = false;
    }
}
