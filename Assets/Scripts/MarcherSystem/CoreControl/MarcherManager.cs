// MarcherManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(EnsembleSessionLoader))]
[RequireComponent(typeof(MarcherFactory))]
public class MarcherManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnsembleSessionLoader sessionLoader;
    [SerializeField] private MarcherFactory marcherFactory;
    [SerializeField] private MarcherPositionService marcherPositionService;
    [SerializeField] private EnsembleDirector2 director;

    [Header("Fallback Arrangement")]
    [SerializeField] private ShapeMarchers shapeMarchers;
    [SerializeField] private IntervalManager intervalManager;

    [Header("Placement Settings")]
    [SerializeField] private ShapeMarchers.ShapeType fallbackShape = ShapeMarchers.ShapeType.Box;

    /// <summary>List of all active marcher instances.</summary>
    public IReadOnlyList<MarcherPositionsManager> Marchers { get; private set; }

    /// <summary>Fired when marchers are built and positioned successfully.</summary>
    public event Action OnMarchersReady = delegate { };

    // ─────────────────────────────────────────────────────────────────────────────

    public void MarcherManagerInit()
    {
        if (sessionLoader == null) sessionLoader = GetComponent<EnsembleSessionLoader>();
        if (marcherFactory == null) marcherFactory = GetComponent<MarcherFactory>();

        sessionLoader.OnReady += RebuildMarchers;
    }

    private void OnDestroy()
    {
        sessionLoader.OnReady -= RebuildMarchers;
    }

    [ContextMenu("Rebuild Marchers Now")]
    public void EditorRebuild() => RebuildMarchers();

    private void RebuildMarchers()
    {
        // 1. Build marchers
        marcherFactory.ClearExisting();
        marcherFactory.Build(sessionLoader.NumberOfMarchers, sessionLoader.NumberOfSets);
        Marchers = new List<MarcherPositionsManager>(marcherFactory.Marchers);

        // 2. Restore saved positions or apply fallback
        PositionMarchers();

        // 3. Broadcast ready event
        OnMarchersReady.Invoke();
    }

    private void PositionMarchers()
    {
        int prevSet;
        int fallbackCount;

        if (sessionLoader.LastSet == 1)
        {
            prevSet = 0;
            fallbackCount = 0;
        }
        else
        {
            prevSet = sessionLoader.LastSet - 1;
            var timingMap = sessionLoader.RuntimeCache.SetTimingMap;
            fallbackCount = timingMap.TryGetValue(prevSet, out var t) ? t.count : 1;
        }

        bool usedSaved = false;
        List<MarcherPositionsManager> fallbackMarchers = new List<MarcherPositionsManager>();

        foreach (var m in Marchers)
        {
            //m.InitializeSetCount(sessionLoader.NumberOfSets);

            // Try restoring position data
            if (sessionLoader.RuntimeCache.ParsedCountPositions.TryGetValue(m.name, out var restored))
            {
                m.LoadPositions(restored);
                // Debug.Log($"📦 Loaded saved positions for {m.name}");
            }

            // Try restoring identity data
            if (sessionLoader.RuntimeCache.ParsedIdentities.TryGetValue(m.name, out var identityData))
            {
                if (m.TryGetComponent(out MarcherIdentityManager identity))
                {
                    identity.LoadIdentity(identityData.section, identityData.abbr, identityData.number);
                }
            }

            // // Try restoring last known confirmed position
            // var interpolator = new MarcherInterpolator(
            //     m,
            //     setIndex => sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8,
            //     () => m.transform.position
            // );

            if (TryFindLatestConfirmedPositionAcrossSets(m, sessionLoader.LastSet, out int latestSet, out int latestCount, out Vector3 latestPos))
            {
                // Reject fallback positions that are still origin
                if (latestPos == Vector3.zero)
                {
                    Debug.LogWarning($"⚠️ {m.name} has confirmed position at Set {latestSet}, Count {latestCount}, but it's still (0,0,0). Ignoring.");
                    fallbackMarchers.Add(m);
                }
                else
                {
                    m.transform.position = latestPos;
                    usedSaved = true;
                    //Debug.Log($"✅ {m.name} positioned at Set {latestSet}, Count {latestCount} → {latestPos}");
                }
            }
        }

        if (!usedSaved)
        {
            ArrangeInSquare();

            foreach (var m in Marchers)
            {
                marcherPositionService?.ConfirmMarcherPosition(m, 0, 0, m.transform.position);
            }

            Debug.Log("🔳 Fallback: arranged all marchers in square and confirmed Set 0, Count 0.");
        }
        
        if (fallbackMarchers.Count > 0)
        {
            FallbackLineupOnBackSideline(fallbackMarchers);
        }
    }

    private bool TryFindLatestConfirmedPositionAcrossSets(
        MarcherPositionsManager marcher,
        int maxSet,
        out int latestSet,
        out int latestCount,
        out Vector3 latestPos)
    {
        latestSet = -1;
        latestCount = -1;
        latestPos = Vector3.zero;

        int cappedSet = Mathf.Max(0, maxSet - 1);
        for (int s = cappedSet; s >= 0; s--)
        {
            if (!marcher.countPositions.TryGetValue(s, out var counts))
                continue;

            for (int c = 100; c >= 0; c--)
            {
                
                if (counts.TryGetValue(c, out var entry) && entry.IsConfirmed)
                {
                    latestSet = s;
                    latestCount = c;
                    latestPos = entry.pos;
                    return true;
                }
            }
        }

        return false;
    }

    private void ArrangeInSquare()
    {
        if (shapeMarchers == null)
        {
            Debug.LogWarning("❌ MarcherManager: ShapeMarchers reference missing.");
            return;
        }

        var objs = new List<GameObject>();
        foreach (var m in Marchers)
        {
            objs.Add(m.gameObject);
        }

        if (!sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(1, out var timing))
        {
            Debug.LogWarning("❌ MarcherManager: Missing SetTiming for Set 1.");
            return;
        }

        shapeMarchers.ArrangeFormation(
            fallbackShape,
            intervalManager.GetIntervalType(timing.count),
            objs
        );
    }
    
    private void FallbackLineupOnBackSideline(List<MarcherPositionsManager> fallbackMarchers)
    {
        if (fallbackMarchers == null || fallbackMarchers.Count == 0)
            return;

        float startX = 52.5f;
        float startZ = 70f;
        float spacing = 1.25f;
        float fixedY = 0.76f;

        for (int i = 0; i < fallbackMarchers.Count; i++)
        {
            var m = fallbackMarchers[i];
            float z = startZ - (i * spacing);
            Vector3 pos = new Vector3(startX, fixedY, z);

            m.transform.position = pos;
            marcherPositionService?.ConfirmMarcherPosition(m, 0, 0, pos);

            Debug.Log($"🟪 Fallback: {m.name} placed at ({pos.x}, {pos.y}, {pos.z}) on back sideline.");
        }

        Debug.Log($"🔁 FallbackLineupOnBackSideline: Positioned and confirmed {fallbackMarchers.Count} marcher(s).");
    }


    public void SetMarchersList(IReadOnlyList<MarcherPositionsManager> updatedList)
    {
        Marchers = updatedList;
    }
}
