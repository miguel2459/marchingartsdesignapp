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
        foreach (var coord in activeCoordinators)
        {
            coord.EnableDashedPreview(); // ✅ allow rendering
            coord.StartPreview();        // ✅ show the visuals
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
