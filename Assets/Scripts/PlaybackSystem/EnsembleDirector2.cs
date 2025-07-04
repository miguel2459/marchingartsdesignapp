using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[ExecuteInEditMode]
public class EnsembleDirector2 : MonoBehaviour, IMarcherProvider, ISetProgressTracker
{
    // IMarcherProvider
    public IReadOnlyList<MarcherPositionsManager> Marchers =>
        marcherFactory != null ? marcherFactory.Marchers : marchers;
    public int NumberOfSets => numberOfSets;
    public int numberOfMarchers;
    public int numberOfSets;
    public int lastSet;
    public int countsPerSet;
    public float bpm;
    public float interval;
    public Vector3 fieldCenter;

    [Header("Managers & Components")]
    public Metronome2 metronome;
    public SnapToGridLines snapToGrid;
    public SetProgressBar setBar;
    public ShapeMarchers shapeMarchers;
    public ShapeGroup shapeGroup;
    public ShapeUIManager shapeUI;
    public IntervalManager intervalManager;
    public FieldGridManager fieldManager;
    public EnsembleUIController UIController;
    public SessionBootstrapper bootstrapper;   // drag from scene
    public MarcherFactory    marcherFactory;   // drag from scene
    public SelectedMarchers selectedMarchers;
    public CountsProgressBar counts;
    public List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>(); 
    public List<SetProgressData> inspectorSetProgress = new List<SetProgressData>();
    private MarcherProgressTracker progressTracker;
    private System.Action refreshHandler;
    [SerializeField] private EnsembleSessionLoader sessionLoader;
    [SerializeField] public MarcherManager marcherManager;
    [SerializeField] private EnsemblePathRenderCoordinator pathRenderer;
    [SerializeField] private MarcherPositionHistory positionHistory;
    [SerializeField] private MarcherPositionService marcherService;
    [SerializeField] private DashedPathPreviewManager dashedPreview;

    [Header("Marcher Progress Colors")]
    public Color fullProgressColor = Color.white;
    public Color partialProgressColor = new Color(1f, 0.92f, 0.5f); // soft yellow
    public Color noProgressColor = new Color(0.6f, 0.6f, 0.6f); // grey
    public static EnsembleDirector2 instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        sessionLoader.OnReady += HandleSessionReady;
        marcherManager.OnMarchersReady += OnMarchersReady;

        refreshHandler = () =>
        {
            progressTracker?.Refresh();
            UpdateInspectorSetProgress();   
        };
    }

    private void HandleSessionReady()
    {
        numberOfMarchers = sessionLoader.NumberOfMarchers;
        numberOfSets     = sessionLoader.NumberOfSets;
        lastSet          = sessionLoader.LastSet;
        positionHistory.SetActiveEditContext(lastSet, 1);


        UIController.InitializeUI();
        setBar.OnTotalSetsChanged(numberOfSets);      

        fieldCenter = fieldManager.GetFieldCenter();
    }

    private void OnMarchersReady()
    {
        fieldCenter = fieldManager.GetFieldCenter();
        metronome.Marchers = marcherManager.Marchers; // if you expose a setter
        marchers = new List<MarcherPositionsManager>(marcherManager.Marchers);
        progressTracker = new MarcherProgressTracker(
            marcherManager.Marchers,
            sessionLoader.RuntimeCache.SetTimingMap);
        progressTracker.OnSetPercentChanged += setBar.UpdateSetProgressColor;
        UpdateInspectorSetProgress();
        UIController.InitializeCountsBar();
        VisualizePathsForSet(lastSet); // Show current set's paths on load
        UndoPositionCaseHandlers.Initialize(marcherService, this);
        RedoPositionCaseHandlers.Initialize(marcherService, this);
    }

    public List<GameObject> MarcherObjects =>
    new List<GameObject>(Marchers.Select(m => m.gameObject));


    public void PreviewCountPosition(int setNumber, int clickedCount)
    {
        var timingMap = sessionLoader.RuntimeCache.SetTimingMap;

        if (!timingMap.TryGetValue(setNumber, out var timing))
        {
            Debug.LogWarning($"❌ No timing data found for Set {setNumber}");
            return;
        }

        int totalCounts = Mathf.Max(1, timing.count);
        float t = Mathf.Clamp01(clickedCount / (float)totalCounts);

        Debug.Log($"🧠 Previewing Set {setNumber}, Count {clickedCount} of {totalCounts}");

        foreach (var marcher in marchers)
        {
            if (marcher.GetAllCountPositions().TryGetValue(setNumber, out var setData) &&
                setData.TryGetValue(clickedCount, out var entry))
            {
                Vector3 previewPos = entry.pos;
                marcher.transform.position = previewPos;
                UpdateSelectorAnchorsForSelectedMarchers();
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager) &&
                    marcher.TryGetComponent(out Unit unit))
                {
                    bool isHolding = posManager.IsHoldingAtCount(setNumber, clickedCount);
                    unit.AttachSelectorToConfirmedPosition(previewPos, isHolding);
                }
                //Debug.Log($"🔍 {marcher.name} previewed at Set {setNumber}, Count {clickedCount} → {previewPos}");
            }
            else
            {
                //Debug.LogWarning($"⚠️ {marcher.name} has no position data at Set {setNumber}, Count {clickedCount}");
            }

        }
    }

    public void UpdateSelectorAnchorsForSelectedMarchers()
    {
        int set = int.TryParse(SessionManager.instance.showStateSO.LastSet, out var parsedSet) ? parsedSet : 1;
        int countIndex = counts?.GetActiveCountIndex() ?? -1;
        int count = (countIndex >= 0) ? countIndex + 1 : 1;

        selectedMarchers?.ForEachSelected(m =>
        {
            if (m != null && m.TryGetComponent(out MarcherVisualStateController visual))
            {
                visual.UpdateSelectorAnchor(set, count);
            }
        });
    }

    public void RefreshDashedPreviewForSelected()
    {
        selectedMarchers?.ReCacheAnchorsForSelected();
        dashedPreview?.UpdatePreviewCycle(forceRefresh: true);
    }

    public void VisualizePathsForSet(int setNumber)
    {
        pathRenderer.RenderPathsForSet(setNumber); // or setNumber
    }

    public void RepositionMarchersToSet(int setIndex)
    {
        int targetSet = (setIndex == 1) ? 0 : setIndex - 1;
        int targetCount = 0;

        if (setIndex > 1)
        {
            // Look up last count of the previous set
            if (!sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(targetSet, out var timing))
            {
                Debug.LogWarning($"⚠️ No timing data found for Set {targetSet}");
                return;
            }

            targetCount = timing.count;
        }

        foreach (var marcher in marchers)
        {
            if (marcher.HasPositionAtCount(targetSet, targetCount))
            {
                Vector3 newPosition = marcher.GetPositionAtCount(targetSet, targetCount);
                marcher.transform.position = newPosition;
                // Debug.Log($"{marcher.name} repositioned to Set {targetSet}, Count {targetCount}");
            }
            else
            {
                //Debug.LogWarning($"{marcher.name} has no position for Set {targetSet}, Count {targetCount}");
            }
        }
    }

    public void RefreshProgressTracker()
    {
        var latestMarchers = new List<MarcherPositionsManager>(marcherFactory.Marchers);

        marchers = latestMarchers; // Update master list
        progressTracker = new MarcherProgressTracker(latestMarchers, sessionLoader.RuntimeCache.SetTimingMap);
        progressTracker.OnSetPercentChanged += setBar.UpdateSetProgressColor;

        UpdateInspectorSetProgress(); // Refresh bar visuals
        counts?.UpdateCountProgressColors(lastSet); // Refresh count progress colors
    }

    public void UpdateInspectorSetProgress()
    {
        progressTracker.Refresh();
        inspectorSetProgress = new List<SetProgressData>();
        var timingMap = sessionLoader.RuntimeCache.SetTimingMap;

        for (int setIndex = 1; setIndex <= numberOfSets; setIndex++)
        {
            int countsPerSet = timingMap.TryGetValue(setIndex, out var timing) ? timing.count : 8;
            int totalCounts = marchers.Count * countsPerSet;
            int completedCounts = 0;

            foreach (var marcher in marchers)
            {
                if (!marcher.countPositions.TryGetValue(setIndex, out var countData)) continue;

                for (int c = 1; c <= countsPerSet; c++)
                {
                    if (countData.TryGetValue(c, out var entry) &&
                        (entry.IsConfirmed || entry.IsInferred))
                    {
                        completedCounts++;
                    }
                }
            }

            var data = new SetProgressData
            {
                setIndex = setIndex,
                completedCounts = completedCounts,
                totalCounts = totalCounts
            };

            inspectorSetProgress.Add(data);
            OnSetProgressChanged?.Invoke(setIndex, data.percent); // ✅ ensure UI is notified
        }

        Debug.Log("📊 Inspector set progress updated.");
    }
    public void ColorMarchersForSet(int setIndex, IEnumerable<MarcherPositionsManager> subset = null)
    {
        var targetGroup = subset ?? marcherManager.Marchers;

        foreach (var marcher in targetGroup)
        {
            ApplyProgressColor(marcher, setIndex);
        }

        //Debug.Log($"🎨 Colored {((subset == null) ? "ALL" : "some")} marchers for Set {setIndex}");
    }

    private void ApplyProgressColor(MarcherPositionsManager marcher, int setIndex)
    {
        int totalCounts = sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing)
            ? timing.count : 8;

        int confirmedOrInferred = 0;

        if (marcher.countPositions.TryGetValue(setIndex, out var countMap))
        {
            for (int c = 1; c <= totalCounts; c++)
            {
                if (countMap.TryGetValue(c, out var entry) &&
                    (entry.IsConfirmed || entry.IsInferred))
                {
                    confirmedOrInferred++;
                }
            }
        }

        // ✅ Skip if marcher is currently selected
        if (selectedMarchers != null && selectedMarchers.IsSelected(marcher.gameObject))
            return;

        Renderer renderer = marcher.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (confirmedOrInferred == totalCounts)
            {
                renderer.material.color = fullProgressColor;
            }
            else if (confirmedOrInferred > 0)
            {
                renderer.material.color = partialProgressColor;
            }
            else
            {
                renderer.material.color = noProgressColor;
            }
        }
        
        if (marcher.TryGetComponent(out MarcherVisualStateController visual))
        {
            int currentCount = GetCurrentCountForSet(setIndex);
            visual.ApplyHoldColorIfEligible(setIndex, currentCount);
        }
    }

    public void RenderDefaultPathsForAll()
    {
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);

        if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(currentSet, out var timing))
            return;

        int totalCounts = timing.count;
        int fallbackSet = (currentSet == 1) ? 0 : currentSet - 1;
        int fallbackCount = 0;

        if (currentSet > 1 &&
            SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var prevTiming))
        {
            fallbackCount = prevTiming.count;
        }

        foreach (var marcher in Marchers)
        {
            Vector3 start = marcher.GetPositionAtCount(fallbackSet, fallbackCount);
            Vector3[] path = marcher.GetInterpolatedPath(currentSet, totalCounts, start);
            marcher.ShowPath(path);

            marcher.pathVisualizer?.SetColor(new Color(1f, 1f, 1f, 0.8f));
        }
    }

    public int GetCountTotalForSet(int set)
    {
        if (sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(set, out var timing))
            return timing.count;
        return 0;
    }

    

    private int GetCurrentCountForSet(int setIndex)
    {
        int activeIndex = counts.GetActiveCountIndex();
        return (activeIndex >= 0) ? activeIndex + 1 : 1; // Convert from 0-based to 1-based
    }


    public bool IsMarcherSelected(GameObject marcher)
    {
        return selectedMarchers != null && selectedMarchers.IsSelected(marcher);
    }

    public float GetSetProgress(int setIndex)
    {
        return progressTracker != null ? progressTracker.GetPercent(setIndex) : 0f;
    }
    public string GenerateMarcherStateJSON()
    {
        return sessionLoader.JsonService.GenerateMarcherStateJSON(marchers);
    }

    public string GenerateSetTimingMapJSON()
    {
        return sessionLoader.JsonService.GenerateSetTimingMapJSON(sessionLoader.RuntimeCache.SetTimingMap);
    }
    [System.Serializable]
    public class SetProgressData
    {
        public int setIndex;
        public int completedCounts; // confirmed or inferred
        public int totalCounts;     // marchers * counts in set
        public float percent => totalCounts > 0 ? (float)completedCounts / totalCounts : 0f;
    }
    // ISetProgressTracker
    public event System.Action<int, float> OnSetProgressChanged = delegate { };
    void OnEnable()  =>
        MarcherPositionsManager.OnAnyMarcherPositionUpdated += refreshHandler;

    void OnDisable() =>
        MarcherPositionsManager.OnAnyMarcherPositionUpdated -= refreshHandler;

    void OnDestroy()
    {
        sessionLoader.OnReady -= HandleSessionReady;
        marcherManager.OnMarchersReady -= OnMarchersReady;
    }

      /// <summary>Allow UI to reach into our loader.</summary>
    public EnsembleSessionLoader SessionLoader => sessionLoader;

    /// <summary>Shortcut to the underlying ShowState SO.</summary>
    public ShowStateSO ShowState => sessionLoader.ShowState;
}
