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
        if (initialPositions.Count == 0)
        {
            //Debug.LogWarning("[DashedPreview] Skipping update — no initial positions registered.");
            return;
        }

        if (!previewStarted)
        {
            Debug.Log($"[DashedPreview] Running UpdatePreviewCycle | previewStarted={previewStarted}");
            foreach (var kvp in initialPositions)
            {
                float dist = Vector3.Distance(kvp.Key.transform.position, kvp.Value);
                Debug.Log($"[DashedPreview] {kvp.Key.name} moved by {dist:F4}");

                if (dist > 0.01f)
                {
                    Debug.Log($"[DashedPreview] Movement detected — starting preview for {kvp.Key.name}");
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

    public void RemoveFromPreview(GameObject marcher)
    {
        initialPositions.Remove(marcher);

        activeCoordinators.RemoveAll(coord =>
            coord == null || coord.gameObject == null || coord.gameObject == marcher);
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

        Debug.Log("[DashedPreview] StartPreview called — applying preview to active coordinators");
        foreach (var coord in activeCoordinators)
        {
            Debug.Log($"    ↳ {coord.name}: IsSelected={EnsembleDirector2.instance.IsMarcherSelected(coord.gameObject)} | MetronomeRunning={EnsembleDirector2.instance.metronome.IsRunning()}");
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
