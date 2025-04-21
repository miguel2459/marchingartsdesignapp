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
        int prevSet = Mathf.Max(1, sessionLoader.LastSet - 1);
        var timingMap = sessionLoader.RuntimeCache.SetTimingMap;
        int fallbackCount = timingMap.TryGetValue(prevSet, out var t) ? t.count : 1;
        bool usedSaved = false;

        foreach (var m in Marchers)
        {
            m.InitializeSetCount(sessionLoader.NumberOfSets);

            // restore saved data if exists
            if (sessionLoader.RuntimeCache
                             .ParsedCountPositions
                             .TryGetValue(m.name, out var restored))
            {
                m.countPositions = restored;
                m.SyncInspectorList();
            }

            // position at last saved dot
            if (m.HasPositionAtCount(prevSet, fallbackCount))
            {
                m.transform.position =
                    m.GetPositionAtCount(prevSet, fallbackCount);
                usedSaved = true;
            }
            else
            {
                Debug.LogWarning(
                  $"{m.name} ⚠️ no saved pos for Set {prevSet}, Count {fallbackCount}");
            }
        }

        // if nothing saved, arrange in a square
        if (!usedSaved)
            ArrangeInSquare();

        // confirm the center as Set0‐Count0
        ConfirmInitialCenter();
    }

    private void ArrangeInSquare()
    {
        if (shapeMarchers == null)
        {
            Debug.LogWarning("MarcherManager: ShapeMarchers missing");
            return;
        }

        var objs = new List<GameObject>();
        foreach (var m in Marchers) objs.Add(m.gameObject);

        shapeMarchers.ArrangeFormation(
          ShapeMarchers.ShapeType.Box,
          intervalManager.GetIntervalType(sessionLoader.RuntimeCache.SetTimingMap[1].count),
          objs);
    }

    private void ConfirmInitialCenter()
    {
        foreach (var m in Marchers)
        {
            // 🧠 Check if Set 0:0 already exists
            if (m.HasPositionAtCount(0, 0))
            {
                Debug.Log($"{m.name} ⏭️ Already has confirmed Set 0:0 — skipping.");
                continue;
            }

            Vector3 pos = m.transform.position;
            m.SetPositionAtCount(0, 0, pos, "march");
            Debug.Log($"{m.name} 🔒 Confirmed Set 0, Count 0 at {pos}");
        }
    }
}
