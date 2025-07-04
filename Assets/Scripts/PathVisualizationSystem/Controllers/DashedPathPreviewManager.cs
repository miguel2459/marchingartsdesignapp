using System.Collections.Generic;
using UnityEngine;

public class DashedPathPreviewManager : MonoBehaviour
{
    private readonly List<MarcherDashedPathCoordinator> activeCoordinators = new List<MarcherDashedPathCoordinator>();

    private bool previewStarted = false;

    public void RegisterSelectedMarchers(List<GameObject> selected)
    {
        activeCoordinators.Clear();
        previewStarted = false;

        int set = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int countIndex = EnsembleDirector2.instance.counts?.GetActiveCountIndex() ?? -1;
        int count = (countIndex >= 0) ? countIndex + 1 : 0;

        foreach (var marcher in selected)
        {
            if (marcher.TryGetComponent(out MarcherDashedPathCoordinator coord))
            {
                activeCoordinators.Add(coord);
                coord.SetInitialPosition(marcher.transform.position);
                coord.SetAnchorContext(set, count); // Optionally inject set/count context again here if needed
                coord.EnableDashedPreview(); // ✅ KEY LINE
                coord.StartPreview();        // ✅ Show anchor + lines immediately
            }
        }
    }

    public void UpdatePreviewCycle(bool forceRefresh = false)
    {
        if (activeCoordinators.Count == 0)
        {
            Debug.LogWarning("[DashedPreview] Skipping update — no active coordinators.");
            return;
        }

        foreach (var coord in activeCoordinators)
        {
            coord.UpdateDashedPreview(coord.transform.position, forceRefresh);
        }
    }

    public void RemoveFromPreview(GameObject marcher)
    {
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

    public void StopAllPreviews()
    {
        foreach (var coord in activeCoordinators)
        {
            coord.StopDashedPreview();
            coord.SetInitialPosition(coord.transform.position); // optional reset to current position
        }

        activeCoordinators.Clear();
        previewStarted = false;
    }
}
