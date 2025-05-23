using System.Collections.Generic;
using System.Collections;

using UnityEngine;

public class MarcherSelectionManager
{
    private readonly EnsembleDirector2 director;
    private readonly DashedPathPreviewManager pathPreviewManager;
    private readonly List<GameObject> selected = new List<GameObject>();
    public Color normalColor = Color.white;
    public IReadOnlyList<GameObject> SelectedMarchers => selected;

    public MarcherSelectionManager(EnsembleDirector2 director, DashedPathPreviewManager pathPreviewManager)
    {
        this.director = director;
        this.pathPreviewManager = pathPreviewManager;
    }

    public void Select(GameObject marcher)
    {
        if (selected.Contains(marcher)) return;

        selected.Add(marcher);

        if (marcher.TryGetComponent(out MarcherVisualStateController visual))
            visual.SetSelected(true);

        // if (marcher.TryGetComponent(out MarcherDashedPathCoordinator coord))
        // {
        //     int set = int.Parse(SessionManager.instance.showStateSO.LastSet);
        //     int countIndex = EnsembleDirector2.instance.counts?.GetActiveCountIndex() ?? -1;
        //     int count = (countIndex >= 0) ? countIndex + 1 : -1;
        //     coord.SetAnchorContext(set, count);
        // }

        int currentSetIndex = int.Parse(SessionManager.instance.showStateSO.LastSet);
        director.VisualizePathsForSet(currentSetIndex);
        pathPreviewManager?.RegisterSelectedMarchers(selected);
        Debug.Log($"🎯 Marcher selected: {marcher.name}");
    }

    public void Deselect(GameObject marcher)
    {
        if (!selected.Contains(marcher)) return;

        selected.Remove(marcher);

        if (marcher.TryGetComponent(out MarcherVisualStateController visual))
            visual.SetSelected(false);

        int currentSetIndex = int.Parse(SessionManager.instance.showStateSO.LastSet);
        director.VisualizePathsForSet(currentSetIndex);
        director.ColorMarchersForSet(currentSetIndex, new[] { marcher.GetComponent<MarcherPositionsManager>() });
    }

    public void SelectAll(IEnumerable<GameObject> allMarchers)
    {
        if (allMarchers == null) return;

        ClearAndResetVisuals();

        foreach (GameObject marcher in allMarchers)
        {
            if (marcher == null) continue;
            Select(marcher);
        }

        pathPreviewManager?.RegisterSelectedMarchers(GetSelectionCopy());

        Debug.Log($"MarcherSelectionService: 🔢 Selected all marchers ({SelectedMarchers.Count}).");
    }

    public void ClearAndResetVisuals()
    {
        foreach (GameObject marcher in selected)
        {
            if (marcher == null) continue;

            // Reset material color
            if (marcher.TryGetComponent(out Renderer renderer))
                renderer.material.color = normalColor;

            // Reset selector visuals
            if (marcher.TryGetComponent(out MarcherVisualStateController visual))
                visual.SetSelected(false);

            // Reset parent
            marcher.transform.SetParent(director.transform);
        }

        ClearAll();
        pathPreviewManager?.StopAllPreviews();
        director.RenderDefaultPathsForAll();
        //Debug.Log("MarcherSelectionManager: Cleared selection and reset visuals.");
    }

    public void ClearAll()
    {
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

        foreach (GameObject marcher in selected.ToArray())
        {
            if (marcher == null) continue;

            if (marcher.TryGetComponent(out Renderer renderer))
                renderer.material.color = director.noProgressColor;

            if (marcher.TryGetComponent(out MarcherVisualStateController visual))
                visual.SetSelected(false);
        }

        selected.Clear();

        director.ColorMarchersForSet(currentSet);
        director.VisualizePathsForSet(currentSet);
        pathPreviewManager?.StopAllPreviews();

        //Debug.Log("🧹 Cleared all selected marchers.");
    }

    public void ReCacheAnchorsForSelected()
    {
        pathPreviewManager?.DisableAllPreviews(); // clear before re-caching
        director.StartCoroutine(DelayedCacheAnchors()); // assumes director is a MonoBehaviour
    }

    private IEnumerator DelayedCacheAnchors()
    {
        yield return null; // Wait one frame to allow transform & UI state to update

        if (SelectedMarchers == null || SelectedMarchers.Count == 0)
            yield break;

        foreach (var marcher in SelectedMarchers)
        {
            if (marcher == null) continue;

            var coordinator = marcher.GetComponent<MarcherDashedPathCoordinator>();
            if (coordinator != null)
            {
                int set = int.Parse(SessionManager.instance.showStateSO.LastSet);
                int countIndex = EnsembleDirector2.instance.counts?.GetActiveCountIndex() ?? -1;
                int count = (countIndex >= 0) ? countIndex + 1 : -1;
                coordinator.SetAnchorContext(set, count);
            }
        }

        pathPreviewManager?.RegisterSelectedMarchers(GetSelectionCopy());
    }


    public List<GameObject> GetSelectionCopy()
    {
        return new List<GameObject>(selected);
    }

    public void ForEachSelected(System.Action<GameObject> action)
    {
        foreach (var marcher in selected)
            action?.Invoke(marcher);
    }
}
