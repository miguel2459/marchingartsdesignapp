using UnityEngine;

/// <summary>
/// Coordinates all dashed path visualizers (forward, backward, anchor) for a marcher during editing.
/// Uses cached anchor context and updates visuals based on current position.
/// </summary>
public class MarcherDashedPathCoordinator : MonoBehaviour
{
    [Header("Dashed Line Visualizers")]
    [SerializeField] private MarcherForwardDashedVisualizer forwardDashedVisualizer;
    [SerializeField] private MarcherBackwardDashedVisualizer backwardDashedVisualizer;
    [SerializeField] private MarcherAnchorDashedVisualizer anchorDashedVisualizer;

    private PathCacheModel pathCache;
    private Vector3 lastUpdatePosition;
    private Vector3? initialPosition = null;
    
    private float lastUpdateTime = 0f;
    [SerializeField] private float minUpdateInterval = 0.05f; // 20 fps max updates
    
    // Cache the current context to avoid recalculating colors every frame
    private int cachedSet = -1;
    private int cachedCount = -1;
    private int cachedTotalCounts = -1;
    private (Color backward, Color forward) cachedColors;

    public void EnableDashedPreview()
    {
        SetRenderState(PathRenderState.LivePreview);
    }

    public void DisableDashedPreview()
    {
        SetRenderState(PathRenderState.Hidden);
    }
    
    private PathRenderState currentState = PathRenderState.Hidden;
    public PathRenderState CurrentState => currentState;

    private void Awake()
    {
        pathCache = new PathCacheModel();
    }

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

        var resolver = new AnchorContextResolver(marcherPosManager, transform);
        pathCache = resolver.ResolveAnchors(set, count);

        lastUpdatePosition = transform.position;

        // Update cached context and colors
        UpdateColorContext();
    }

    private void UpdateColorContext()
    {
        // Get current context
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int countIndex = EnsembleDirector2.instance.counts?.GetActiveCountIndex() ?? -1;
        int currentCount = (countIndex >= 0) ? countIndex + 1 : 0;
        
        // Get total counts for this set
        int totalCountsInSet = SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(currentSet, out var timing) 
            ? timing.count 
            : 8; // fallback

        // Only update colors if context has changed
        if (cachedSet != currentSet || cachedCount != currentCount || cachedTotalCounts != totalCountsInSet)
        {
            cachedSet = currentSet;
            cachedCount = currentCount;
            cachedTotalCounts = totalCountsInSet;
            
            // Get new colors based on context
            cachedColors = DashedPathColorContext.GetDashedLineColors(currentSet, currentCount, totalCountsInSet);
            
            // Apply colors to visualizers
            forwardDashedVisualizer?.SetDynamicColor(cachedColors.forward);
            backwardDashedVisualizer?.SetDynamicColor(cachedColors.backward);
            
            Debug.Log($"[Coordinator] Updated colors for Set {currentSet}, Count {currentCount}/{totalCountsInSet}. " +
                     $"Context: {DashedPathColorContext.GetCountContext(currentSet, currentCount, totalCountsInSet)}");
        }
    }

    public void UpdateDashedPreview(Vector3 currentPosition, bool forceRefresh = false)
    {
        if (currentState != PathRenderState.LivePreview || pathCache == null)
            return;

        if (!forceRefresh)
        {
            if (Time.time - lastUpdateTime < minUpdateInterval)
                return;

            if (Vector3.Distance(lastUpdatePosition, currentPosition) < 0.01f)
                return;
        }

        lastUpdateTime = Time.time;
        lastUpdatePosition = currentPosition;

        // Update color context (this will only recalculate if context changed)
        UpdateColorContext();

        bool isAtSet0Count0 =
            SessionManager.instance.showStateSO.LastSet == "1" &&
            EnsembleDirector2.instance.counts.GetActiveCountIndex() < 0;

        if (isAtSet0Count0)
        {
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

            if (pathCache.NextConfirmed.HasValue && forwardDashedVisualizer != null)
            {
                forwardDashedVisualizer.SetPath(currentPosition, pathCache.NextConfirmed.Value);
            }
            else
            {
                forwardDashedVisualizer?.Hide();
            }

            backwardDashedVisualizer?.Hide();
            return;
        }

        // --- BACKWARD PATH ---
        if (pathCache.PreviousConfirmed.HasValue && backwardDashedVisualizer != null)
        {
            backwardDashedVisualizer.SetPath(currentPosition, pathCache.PreviousConfirmed.Value);
        }
        else
        {
            backwardDashedVisualizer?.Hide();
        }

        // --- FORWARD PATH ---
        if (pathCache.NextConfirmed.HasValue && forwardDashedVisualizer != null)
        {
            forwardDashedVisualizer.SetPath(currentPosition, pathCache.NextConfirmed.Value);
        }
        else
        {
            forwardDashedVisualizer?.Hide();
        }

        // --- ANCHOR LINE ---
        if (pathCache.ActiveConfirmed.HasValue && anchorDashedVisualizer != null)
        {
            anchorDashedVisualizer.SetPath(currentPosition, pathCache.ActiveConfirmed.Value);
        }
        else
        {
            anchorDashedVisualizer?.Hide();
        }

        // --- SELECTOR STATE ---
        if (TryGetComponent(out MarcherVisualStateController visual))
        {
            visual.UpdateSelectorAttachment(currentPosition, pathCache.ActiveConfirmed);
        }
    }

    public void StartPreview()
    {
        // Update colors when starting preview
        UpdateColorContext();
        
        UpdateDashedPreview(transform.position);

        if (pathCache.ActiveConfirmed.HasValue && anchorDashedVisualizer != null)
        {
            anchorDashedVisualizer.SetPath(transform.position, pathCache.ActiveConfirmed.Value);
        }
    }

    public void StopDashedPreview()
    {
        SetRenderState(PathRenderState.Hidden);
    }
    
    public void SetRenderState(PathRenderState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case PathRenderState.Hidden:
                forwardDashedVisualizer?.Hide();
                backwardDashedVisualizer?.Hide();
                anchorDashedVisualizer?.Hide();
                break;

            case PathRenderState.StaticConfirmed:
                // Optional: for future implementation (e.g., rehearsal tracking)
                break;

            case PathRenderState.LivePreview:
                // Update colors and start preview
                UpdateColorContext();
                UpdateDashedPreview(transform.position);
                break;
        }
    }
}