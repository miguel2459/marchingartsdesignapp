// MarcherLifecycleService.cs
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// High-level service to manage full lifecycle of marchers:
/// spawning, confirming, deleting, interpolating, and syncing visuals/UI.
/// </summary>
public class MarcherLifecycleService : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MarcherFactory marcherFactory;
    [SerializeField] private MarcherPositionService positionService;
    [SerializeField] private EnsembleDirector2 director;
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private DashedPathPreviewManager dashedPathPreviewManager;
    [SerializeField] private CameraModeManager cameraMode;

    private Func<int, int> getMaxCountForSet;

    private void Awake()
    {
        getMaxCountForSet = positionService.GetMaxCountForSet;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Confirms a marcher dot at the given set/count with full lifecycle awareness.
    /// </summary>
    public void ConfirmDot(MarcherPositionsManager marcher, int set, int count, Vector3 position)
    {
        Debug.Log($"✅ LifecycleService: Confirming Set {set}, Count {count} for {marcher.name}");

        positionHistory.BeginBatch();
        positionService.ConfirmMarcherPosition(marcher, set, count, position);
        positionHistory.EndBatch();

        RefreshAfterChange(set);
    }

    /// <summary>
    /// Deletes a confirmed dot and refreshes visuals if successful.
    /// </summary>
    public void DeleteDot(MarcherPositionsManager marcher, int set, int count)
    {
        Debug.Log($"🗑 LifecycleService: Deleting Set {set}, Count {count} for {marcher.name}");

        bool success = positionService.DeleteConfirmedPosition(marcher, set, count, out bool hadNext);

        if (!success)
        {
            Debug.LogWarning($"⚠️ Deletion failed for {marcher.name} at Set {set}, Count {count}");
            return;
        }

        RefreshAfterChange(set);

        if (hadNext)
            director.counts?.OnCountButtonClicked(set, count);

        Debug.Log($"✅ Deleted position for {marcher.name} at Set {set}, Count {count}");
    }

    /// <summary>
    /// Spawns and positions new marchers in a staggered grid near back sideline.
    /// </summary>
    public void SpawnNewMarchers(int howMany)
    {
        Debug.Log($"➕ LifecycleService: Spawning {howMany} new marcher(s)...");

        int currentCount = marcherFactory.Marchers.Count;
        int totalSets = director.numberOfSets;

        Vector2 fieldMin = director.snapToGrid.currentFieldMin;
        Vector2 fieldMax = director.snapToGrid.currentFieldMax;

        float centerZ = (fieldMin.y + fieldMax.y) / 2f;
        float backX = fieldMax.x;

        int marchersPerRow = 20;
        float spacing = 2f;
        float centerOffset = (marchersPerRow - 1) / 2f;

        for (int i = 0; i < howMany; i++)
        {
            int index = currentCount + i;
            int row = index / marchersPerRow;
            int col = index % marchersPerRow;

            float zOffset = (col - centerOffset) * spacing;
            float z = centerZ + zOffset;
            float x = backX + 3f - (row * spacing);

            x = Mathf.Clamp(x, fieldMin.x, fieldMax.x);
            z = Mathf.Clamp(z, fieldMin.y, fieldMax.y);

            Vector3 spawnPos = new Vector3(x, 0.76f, z);

            // Debug.Log($"    ↳ Spawned Marcher{index + 1} at ({x:F2}, {z:F2})");

            marcherFactory.Spawn(index, totalSets, spawnPos);
            var newMarcher = marcherFactory.Marchers[marcherFactory.Marchers.Count - 1];

            newMarcher.InitializeSetCount(totalSets);
            newMarcher.transform.position = spawnPos;

            // Always confirm Set 0, Count 0 for JSON integrity
            positionService.ConfirmMarcherPosition(newMarcher, 0, 0, spawnPos);
        }

        RefreshAfterSpawner();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes UI, visuals, anchors after a dot change.
    /// </summary>
    private void RefreshAfterChange(int set)
    {
        director.UpdateInspectorSetProgress();
        director.VisualizePathsForSet(set);
        director.selectedMarchers.ReCacheAnchorsForSelected();

        int updatedCountTotal = director.GetCountTotalForSet(set);
        director.counts?.RenderCounts(set, updatedCountTotal);
        director.counts?.UpdateCountSubtextsForSet(set);
    }

    /// <summary>
    /// Updates core references and visuals after spawning new marchers.
    /// </summary>
    
    public void DeleteMarcher(MarcherPositionsManager marcher)
    {
        string name = marcher.name;

        // 1. Remove from JSON cache
        director.SessionLoader.RuntimeCache.ParsedCountPositions.Remove(name);
        director.SessionLoader.RuntimeCache.ParsedIdentities.Remove(name);

        // 2. Remove from active lists
        marcherFactory.RemoveMarcher(marcher);
        director.marchers.Remove(marcher);
        director.marcherManager.SetMarchersList(marcherFactory.Marchers);

        // 3. Destroy object
        Destroy(marcher.gameObject);

        // 4. Update state
        director.numberOfMarchers--;
        director.SessionLoader.ShowState.NumberOfMarchers = director.numberOfMarchers;
        director.UIController.UpdateNumberOfMarchersUI(director.numberOfMarchers);

        // 5. Refresh visuals
        director.UpdateInspectorSetProgress();
        director.UIController.InitializeCountsBar();

        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        director.VisualizePathsForSet(currentSet);
        dashedPathPreviewManager?.RemoveFromPreview(marcher.gameObject);

        cameraMode.ReapplyActiveCameraMode();

        Debug.Log($"🗑 Deleted marcher {name} and cleaned up references.");
    }


    private void RefreshAfterSpawner()
    {
        director.marchers = new List<MarcherPositionsManager>(marcherFactory.Marchers);
        director.metronome.Marchers = marcherFactory.Marchers;
        director.marcherManager.SetMarchersList(marcherFactory.Marchers);

        director.RefreshProgressTracker();
        director.UIController.InitializeCountsBar();
        director.UpdateInspectorSetProgress();

        director.setBar.OnSetButtonClick(1);
        director.ColorMarchersForSet(1);

        cameraMode.ReapplyActiveCameraMode();
    }
}
