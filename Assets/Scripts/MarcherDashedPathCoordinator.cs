using UnityEngine;

/// <summary>
/// Coordinates all dashed path visualizers (forward, backward, anchor) for a marcher during editing.
/// Applies step size color and tiling adjustments.
/// </summary>
public class MarcherDashedPathCoordinator : MonoBehaviour
{
    [Header("Dashed Line Visualizers")]
    [SerializeField] private MarcherForwardDashedVisualizer forwardDashedVisualizer;
    [SerializeField] private MarcherBackwardDashedVisualizer backwardDashedVisualizer;
    [SerializeField] private MarcherAnchorDashedVisualizer anchorDashedVisualizer;

    // Cached anchor points
    private Vector3? previousConfirmedPosition = null;
    private Vector3? nextConfirmedPosition = null;
    private Vector3? activeConfirmedPosition = null;

    // --- Added from MarcherDashedPathVisualizer logic ---
    private Vector3 lastUpdatePosition; // Track position for updates
    private bool allowDashedPreview = false;
    public void EnableDashedPreview() => allowDashedPreview = true;
    public void DisableDashedPreview() => allowDashedPreview = false;


    public void CacheAnchorsFromSceneContext()
    {
        
        if (!TryGetComponent(out MarcherPositionsManager marcherPosManager))
        {
            Debug.LogError($"[Coordinator] Missing MarcherPositionsManager on {gameObject.name}");
            return;
        }
        
        int set = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int countIndex = EnsembleDirector2.instance.counts.GetActiveCountIndex();
        int count = (countIndex >= 0) ? countIndex + 1 : 1;
        int activeSet, activeCount;

        if (countIndex == -1)
        {
            // 👈 No count selected: fallback to previous set’s last count
            int previousSet = (set == 1) ? 0 : set - 1;
            activeSet = previousSet;

            // 🧮 Get last count from the previous set
            activeCount = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(previousSet, out var timing)
                ? timing.count : 1;
        }
        else
        {
            // ✅ Count is selected: use it directly
            activeSet = set;
            activeCount = count;
        }

        // 🔄 Create interpolator helper to search for confirmed dots
        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcherPosManager,
            (setIndex) => SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8,
            () => transform.position
        );

        // 🔙 Get previous confirmed dot (if available), otherwise use current position
        Vector3 previous = transform.position;
        if (interpolator.TryFindLastConfirmedPosition(activeSet, activeCount, out _, out _, out Vector3 prevPos))
            previous = prevPos;

        // 🔜 Get next confirmed dot (if available)
        Vector3? next = null;
        if (interpolator.TryFindNextConfirmedPosition(activeSet, activeCount, out _, out _, out Vector3 nextPos))
            next = nextPos;

        // 🧷 Only cache anchor if the current dot is confirmed ("march" or "inferred")
        Vector3? active = null;
        if (marcherPosManager.HasPositionAtCount(activeSet, activeCount) &&
            (marcherPosManager.GetTagForCount(activeSet, activeCount) == "march" ||
            marcherPosManager.GetTagForCount(activeSet, activeCount) == "inferred"))
        {
            active = marcherPosManager.GetPositionAtCount(activeSet, activeCount);
        }

        // 📦 Pass anchors to visualizer (previous, next, current)
        CacheAnchors(previous, next, active);
        StartPreview();
    }


    public void CacheAnchors(Vector3 previousPos, Vector3? nextPos = null, Vector3? activeConfirmedPos = null)
    {
        previousConfirmedPosition = previousPos;
        nextConfirmedPosition = nextPos;
        activeConfirmedPosition = activeConfirmedPos;
        lastUpdatePosition = transform.position;
        Debug.Log($"[Coordinator:{gameObject.name}] Anchors Cached: Prev={previousPos}, Next={nextPos?.ToString() ?? "None"}, Active={activeConfirmedPos?.ToString() ?? "None"}");
    }

    public void UpdateDashedPreview(Vector3 currentPosition)
    {
        if (!allowDashedPreview) return;
        
        // Optimization: Only update if the marcher has actually moved significantly
        if (Vector3.Distance(lastUpdatePosition, currentPosition) < 0.01f)
            return;

        lastUpdatePosition = currentPosition;

        // 💡 Edge case: Only show anchor when editing Set 1 at Set 0:Count 0
        bool isSet1 = SessionManager.instance.showStateSO.LastSet == "1";
        int activeCountIndex = EnsembleDirector2.instance.counts.GetActiveCountIndex();
        bool isCount0 = (activeCountIndex < 0); // no count selected or user is on Count 0
        bool anchorOnlyMode = isSet1 && isCount0;

        if (anchorOnlyMode)
        {
            if (previousConfirmedPosition.HasValue && anchorDashedVisualizer != null)
            {
                anchorDashedVisualizer.SetAnchorPath(currentPosition, previousConfirmedPosition.Value);
            }

            forwardDashedVisualizer?.Hide();
            backwardDashedVisualizer?.Hide();
            return;
        }

        // --- Update Backward Path ---
        if (previousConfirmedPosition.HasValue && backwardDashedVisualizer != null)
        {
            backwardDashedVisualizer.SetBackwardPath(currentPosition, previousConfirmedPosition.Value);
            if (backwardDashedVisualizer.Renderer != null)
            {
                UpdateColorBasedOnStepSize(backwardDashedVisualizer.Renderer, currentPosition, previousConfirmedPosition.Value);
                AdjustTilingBasedOnPath(backwardDashedVisualizer.Renderer, currentPosition, previousConfirmedPosition.Value);
            }
        }
        else
        {
            backwardDashedVisualizer?.Hide();
        }

        // --- Update Forward Path ---
        if (nextConfirmedPosition.HasValue && forwardDashedVisualizer != null)
        {
            forwardDashedVisualizer.SetForwardPath(currentPosition, nextConfirmedPosition.Value);
            if (forwardDashedVisualizer.Renderer != null)
            {
                UpdateColorBasedOnStepSize(forwardDashedVisualizer.Renderer, currentPosition, nextConfirmedPosition.Value);
                AdjustTilingBasedOnPath(forwardDashedVisualizer.Renderer, currentPosition, nextConfirmedPosition.Value);
            }
        }
        else
        {
            forwardDashedVisualizer?.Hide();
        }

        // --- Update Anchor Path ---
        if (activeConfirmedPosition.HasValue && anchorDashedVisualizer != null)
        {
            anchorDashedVisualizer.SetAnchorPath(currentPosition, activeConfirmedPosition.Value);
        }
        else
        {
            anchorDashedVisualizer?.Hide();
        }
    }

    public void StartPreview()
    {
        UpdateDashedPreview(transform.position); // force refresh on current position
        Debug.Log($"▶️ [Coordinator] {gameObject.name} started preview.");
    }

    public void StopDashedPreview()
    {
        forwardDashedVisualizer?.Hide();
        backwardDashedVisualizer?.Hide();
        anchorDashedVisualizer?.Hide();
        // Reset cached positions maybe? Or handled by new CacheAnchors call? Let's keep them for now.
        Debug.Log($"[Coordinator:{gameObject.name}] Stopping Dashed Preview.");
    }

    private void UpdateColorBasedOnStepSize(LineRenderer renderer, Vector3 current, Vector3 anchor)
    {
        if (renderer == null) return;

        float distance = Vector3.Distance(anchor, current);
        //Color previewColor = StepSizeCalculator.GetStepSizeColor(distance); // Using static class

        //renderer.startColor = previewColor;
        //renderer.endColor = previewColor;
    }

    private void AdjustTilingBasedOnPath(LineRenderer renderer, Vector3 start, Vector3 end)
    {
        if (renderer == null || renderer.material == null)
            return;

        float length = Vector3.Distance(start, end);
        // Adjust tiling factor as needed for your material/texture
        float tilingX = Mathf.Max(1f, length * 1.5f); // Example: 1.5 tiles per unit length
        renderer.material.mainTextureScale = new Vector2(tilingX, 1f);
    }
}