using System.Collections.Generic;
using UnityEngine;

public class MarcherEditActions : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MarcherSelectionManager selectionManager;
    [SerializeField] private MarcherLifecycleService marcherLifecycleService;
    [SerializeField] private CountsProgressBar countsProgressBar;
    [SerializeField] private MarcherDeleteConfirmationPanel deleteConfirmationPanel;
    [SerializeField] private DashedPathPreviewManager dashedPathPreviewManager;

    public void SetSelectionManager(MarcherSelectionManager manager)
    {
        selectionManager = manager;
    }

    public void ConfirmDotViaSpacebar()
    {
        if (selectionManager.SelectedMarchers == null || selectionManager.SelectedMarchers.Count == 0)
            return;

        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int setToUse, countToUse;

        int activeCountIndex = countsProgressBar?.GetActiveCountIndex() ?? -1;

        if (activeCountIndex >= 0)
        {
            setToUse = currentSet;
            countToUse = activeCountIndex + 1;
            Debug.Log($"MarcherEditActions: ⏺️ Setting positions at Set {setToUse}, Count {countToUse}");
        }
        else
        {
            setToUse = (currentSet == 1) ? 0 : currentSet - 1;
            countToUse = (currentSet == 1)
                ? 0
                : SessionManager.instance.runtimeCacheSO.SetTimingMap[setToUse].count;

            Debug.Log($"MarcherEditActions: ⏺️ Fallback to Set {setToUse}, Count {countToUse}");
        }

        selectionManager.ForEachSelected(marcher =>
        {
            if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
            {
                marcherLifecycleService.ConfirmDot(posManager, setToUse, countToUse, marcher.transform.position);

                bool isHolding = posManager.IsHoldingAtCount(setToUse, countToUse);

                if (marcher.TryGetComponent(out Unit unit))
                {
                    unit.AttachSelectorToConfirmedPosition(marcher.transform.position, isHolding);
                }
            }
        });


        int renderSet = (setToUse == 0) ? 1 : setToUse;
        int countsInSet = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(renderSet, out var timing)
            ? timing.count : 0;

        countsProgressBar?.RenderCounts(renderSet, countsInSet);
        countsProgressBar?.UpdateCountProgressColors(renderSet);
        countsProgressBar?.UpdateCountSubtextsForSet(renderSet);

        if (activeCountIndex >= 0)
            countsProgressBar?.HighlightCount(activeCountIndex);
        else
            countsProgressBar?.ResetHighlight();

        dashedPathPreviewManager?.DisableAllPreviews();
    }

    public void DeleteConfirmedDot()
    {
        if (selectionManager.SelectedMarchers == null || selectionManager.SelectedMarchers.Count == 0)
            return;

        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int countIndex = countsProgressBar?.GetActiveCountIndex() ?? -1;

        if (countIndex < 0)
        {
            Debug.LogWarning("❌ No count is currently selected. Cannot delete dot.");
            return;
        }

        int countToDelete = countIndex + 1;

        foreach (var marcher in selectionManager.SelectedMarchers)
        {
            if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
            {
                marcherLifecycleService.DeleteDot(posManager, currentSet, countToDelete);
            }
        }
    }

    public void PromptDeleteMarchers()
    {
        if (selectionManager.SelectedMarchers.Count == 0)
        {
            Debug.LogWarning("⚠️ No marchers selected for deletion.");
            return;
        }

        deleteConfirmationPanel.Show(selectionManager.GetSelectionCopy(), ConfirmDeleteMarcher);
    }

    private void ConfirmDeleteMarcher(List<GameObject> marchersToDelete)
    {
        foreach (var marcher in marchersToDelete)
        {
            if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
            {
                marcherLifecycleService.DeleteMarcher(posManager);
            }
        }

        Debug.Log($"✅ Deleted {marchersToDelete.Count} marcher(s).");
    }
}
