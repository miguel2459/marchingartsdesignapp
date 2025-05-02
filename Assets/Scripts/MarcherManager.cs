// MarcherManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(EnsembleSessionLoader))]
[RequireComponent(typeof(MarcherFactory))]
public class MarcherManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnsembleSessionLoader sessionLoader;  // for counts & timing
    [SerializeField] private MarcherFactory    marcherFactory;   // builds marchers
    [SerializeField] private MarcherPositionService marcherPositionService;   // builds marchers

    [Header("Fallback Arrangement")]
    [SerializeField] private ShapeMarchers     shapeMarchers;     // for square fallback
    [SerializeField] private IntervalManager   intervalManager;   // spacing info

    /// <summary>List of all marched‐in scenes, populated on load.</summary>
    public IReadOnlyList<MarcherPositionsManager> Marchers { get; private set; }

    /// <summary>Fired once marchers are built & positioned.</summary>
    public event Action OnMarchersReady = delegate { };

    private void Awake()
    {
        // auto‐wire if needed
        if (sessionLoader  == null) sessionLoader  = GetComponent<EnsembleSessionLoader>();
        if (marcherFactory == null) marcherFactory = GetComponent<MarcherFactory>();

        sessionLoader.OnReady += BuildAndPosition;
    }

    private void OnDestroy()
    {
        sessionLoader.OnReady -= BuildAndPosition;
    }

    private void BuildAndPosition()
    {
        // 1. Build marchers
        marcherFactory.ClearExisting();
        marcherFactory.Build(sessionLoader.NumberOfMarchers,
                             sessionLoader.NumberOfSets);
        Marchers = new List<MarcherPositionsManager>(marcherFactory.Marchers);

        // 2. Inject saved positions / fallback
        PositionMarchers();

        // 3. Signal ready
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

        foreach (var m in Marchers)
        {
            m.InitializeSetCount(sessionLoader.NumberOfSets);

            if (sessionLoader.RuntimeCache.ParsedCountPositions.TryGetValue(m.name, out var restored))
            {
                m.countPositions = restored;
                m.SyncInspectorList();
            }
            // Inject identity if available
            if (sessionLoader.RuntimeCache.ParsedIdentities.TryGetValue(m.name, out var identityData))
            {
                if (m.TryGetComponent(out MarcherIdentityManager identity))
                {
                    identity.LoadIdentity(identityData.section, identityData.abbr, identityData.number);
                }
            }

            var interpolator = new MarcherInterpolator(
                m,
                setIndex => sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8,
                () => m.transform.position
            );

            if (TryFindLatestConfirmedPositionAcrossSets(m, sessionLoader.LastSet, out int latestSet, out int latestCount, out Vector3 latestPos))
            {
                m.transform.position = latestPos;
                usedSaved = true;
                //Debug.Log($"{m.name} ✅ positioned at Set {latestSet}, Count {latestCount} → {latestPos}");
            }
            else
            {
                Debug.LogWarning($"{m.name} ⚠️ no confirmed fallback found. Will require ArrangeInSquare.");
            }
        }

        if (!usedSaved)
        {
            ArrangeInSquare();
            foreach (var m in Marchers)
            {
                marcherPositionService?.ConfirmMarcherPosition(m, 0, 0, m.transform.position);
            }
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

            for (int c = 100; c >= 0; c--) // assume max 100 counts per set
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
            Debug.LogWarning("MarcherManager: ShapeMarchers missing");
            return;
        }

        var objs = new List<GameObject>();
        foreach (var m in Marchers){
            objs.Add(m.gameObject);
        } 

        shapeMarchers.ArrangeFormation(
          ShapeMarchers.ShapeType.Box,
          intervalManager.GetIntervalType(sessionLoader.RuntimeCache.SetTimingMap[1].count),
          objs);
    }
}
