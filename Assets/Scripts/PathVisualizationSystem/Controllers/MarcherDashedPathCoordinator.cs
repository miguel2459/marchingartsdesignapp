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

    private Vector3? initialPosition = null;

    public void SetInitialPosition(Vector3 pos)
    {
        initialPosition = pos;
    }

    public Vector3? GetInitialPosition() => initialPosition;

    public void SetAnchorContext(int set, int count)
    {
        if (!TryGetComponent(out MarcherPositionsManager marcherPosManager))
        {
            Debug.LogError($"[Coordinator] Missing MarcherPositionsManager on {gameObject.name}");
            return;
        }

        // 🔄 Create interpolator helper to search for confirmed dots
        MarcherInterpolator interpolator = new MarcherInterpolator(
            marcherPosManager,
            (setIndex) => SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8,
            () => transform.position
        );

        // 🔙 Get previous confirmed dot (if available), otherwise use current position
        Vector3 previous = transform.position;
        if (interpolator.TryFindLastConfirmedPosition(set, count, out _, out _, out Vector3 prevPos))
            previous = prevPos;

        // 🔜 Get next confirmed dot (if available)
        Vector3? next = null;
        if (interpolator.TryFindNextConfirmedPosition(set, count, out _, out _, out Vector3 nextPos))
            next = nextPos;

        // 🧷 Only anchor if the current dot is confirmed
        Vector3? active = null;

        if (set == 0 && count == 0 && marcherPosManager.HasPositionAtCount(0, 0))
        {
            active = marcherPosManager.GetPositionAtCount(0, 0);
            Debug.Log($"📍 [Coordinator:{gameObject.name}] Anchor set using Set 0:Count 0 → {active.Value}");
        }
        else if (marcherPosManager.HasPositionAtCount(set, count))
        {
            string tag = marcherPosManager.GetTagForCount(set, count);
            if (tag == "march" || tag == "inferred")
            {
                active = marcherPosManager.GetPositionAtCount(set, count);
            }
        }

        CacheAnchors(previous, next, active);
    }

    public void CacheAnchors(Vector3 previousPos, Vector3? nextPos = null, Vector3? activeConfirmedPos = null)
    {
        previousConfirmedPosition = previousPos;
        nextConfirmedPosition = nextPos;
        activeConfirmedPosition = activeConfirmedPos;
        lastUpdatePosition = transform.position;
        //Debug.Log($"[Coordinator:{gameObject.name}] Anchors Cached: Prev={previousPos}, Next={nextPos?.ToString() ?? "None"}, Active={activeConfirmedPos?.ToString() ?? "None"}");
    }

    public void UpdateDashedPreview(Vector3 currentPosition)
    {
        if (!allowDashedPreview) return;

        if (Vector3.Distance(lastUpdatePosition, currentPosition) < 0.01f)
            return;

        lastUpdatePosition = currentPosition;

        bool isAtSet0Count0 =
            SessionManager.instance.showStateSO.LastSet == "1" &&
            EnsembleDirector2.instance.counts.GetActiveCountIndex() < 0;

        if (isAtSet0Count0)
        {
            // ✅ Anchor to Set 0:0 confirmed dot
            if (TryGetComponent(out MarcherPositionsManager posManager) &&
                posManager.HasPositionAtCount(0, 0))
            {
                Vector3 set0Pos = posManager.GetPositionAtCount(0, 0);
                anchorDashedVisualizer?.SetPath(currentPosition, set0Pos);
            }
            else
            {
                anchorDashedVisualizer?.Hide();
            }

            // ✅ Forward line if next confirmed dot exists
            if (nextConfirmedPosition.HasValue && forwardDashedVisualizer != null)
            {
                forwardDashedVisualizer.SetPath(currentPosition, nextConfirmedPosition.Value);
            }
            else
            {
                forwardDashedVisualizer?.Hide();
            }

            // ❌ No backward line
            backwardDashedVisualizer?.Hide();
            return;
        }


        // --- BACKWARD PATH ---
        if (previousConfirmedPosition.HasValue && backwardDashedVisualizer != null)
        {
            backwardDashedVisualizer.SetPath(currentPosition, previousConfirmedPosition.Value);
        }
        else
        {
            backwardDashedVisualizer?.Hide();
        }

        // --- FORWARD PATH ---
        if (nextConfirmedPosition.HasValue && forwardDashedVisualizer != null)
        {
            forwardDashedVisualizer.SetPath(currentPosition, nextConfirmedPosition.Value);
        }
        else
        {
            forwardDashedVisualizer?.Hide();
        }

        // --- ANCHOR LINE ---
        if (activeConfirmedPosition.HasValue && anchorDashedVisualizer != null)
        {
            anchorDashedVisualizer.SetPath(currentPosition, activeConfirmedPosition.Value);
        }
        else
        {
            anchorDashedVisualizer?.Hide();
        }

        // 🧠 Always update selector logic at the end
        if (TryGetComponent(out MarcherVisualStateController visual))
        {
            visual.UpdateSelectorAttachment(currentPosition, activeConfirmedPosition);
        }
    }



    public void StartPreview()
    {
        UpdateDashedPreview(transform.position); // force refresh on current position
        // 🔁 Force anchor to render even if no movement has happened yet
        if (activeConfirmedPosition.HasValue && anchorDashedVisualizer != null)
        {
            anchorDashedVisualizer.SetPath(transform.position, activeConfirmedPosition.Value);
        }
        //Debug.Log($"▶️ [Coordinator] {gameObject.name} started preview.");
    }

    public void StopDashedPreview()
    {
        forwardDashedVisualizer?.Hide();
        backwardDashedVisualizer?.Hide();
        anchorDashedVisualizer?.Hide();
        // Reset cached positions maybe? Or handled by new CacheAnchors call? Let's keep them for now.
        //Debug.Log($"[Coordinator:{gameObject.name}] Stopping Dashed Preview.");
    }
}