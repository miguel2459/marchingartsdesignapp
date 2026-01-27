using UnityEngine;
using System.Collections.Generic;

public class EnsemblePathRenderCoordinator : MonoBehaviour
{
    [SerializeField] private SelectedMarchers selectedMarchers;
    [SerializeField] private EnsembleDirector2 director;

    private readonly Color defaultPathColor = new Color(1f, 1f, 1f, 0.8f);
    private readonly Color selectedPathColor = new Color(0f, 0.81f, 1f, 1f); // #00CFFF

    private void Awake()
    {
        MarcherPositionsManager.OnAnyMarcherPositionUpdated += HandlePathRefresh;
    }

    private void HandlePathRefresh()
    {
        int set = int.TryParse(SessionManager.instance.showStateSO.LastSet, out var lastSet)
            ? lastSet : 1;

        RenderPathsForSet(set);
    }

    public void RenderPathsForSet(int setIndex)
    {
        if (!director.SessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing))
        {
            Debug.LogWarning($"❌ No timing data for Set {setIndex}");
            return;
        }

        int totalCounts = timing.count;
        int fallbackSet = (setIndex == 1) ? 0 : setIndex - 1;
        int fallbackCount = 0;

        if (setIndex > 1 &&
            director.SessionLoader.RuntimeCache.SetTimingMap.TryGetValue(fallbackSet, out var prevTiming))
        {
            fallbackCount = prevTiming.count;
        }

        var selected = (selectedMarchers != null && SelectedMarchers.IsInitialized)
            ? selectedMarchers.GetSelectionCopy()
            : new List<GameObject>();

        bool hasSelection = selected.Count > 0;

        foreach (var marcher in director.Marchers)
        {
            Vector3 start = marcher.GetPositionAtCount(fallbackSet, fallbackCount);
            Vector3[] path = marcher.GetInterpolatedPath(setIndex, totalCounts, start);

            if (hasSelection)
            {
                if (selected.Contains(marcher.gameObject))
                {
                    marcher.ShowPath(path);
                    marcher.pathVisualizer?.SetColor(selectedPathColor);
                }
                else
                {
                    marcher.HidePath();
                }
            }
            else
            {
                marcher.ShowPath(path);
                marcher.pathVisualizer?.SetColor(defaultPathColor);
            }
        }

        //Debug.Log($"🧩 RenderPathsForSet: Set {setIndex} with {(hasSelection ? "selected" : "all")} marchers");
    }

    public void HideAllPaths()
    {
        foreach (var marcher in director.Marchers)
            marcher.HidePath();
    }

    private void OnDestroy()
    {
        MarcherPositionsManager.OnAnyMarcherPositionUpdated -= HandlePathRefresh;
    }

}
