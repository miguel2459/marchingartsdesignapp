using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[ExecuteInEditMode]
public class EnsembleDirector2 : MonoBehaviour
{
    SessionManager session = SessionManager.instance;
    RuntimeCacheData runtimeCache;

    [Header("Marcher Settings")]
    public int numberOfMarchers;
    public int numberOfSets;
    public int lastSet;
    public int countsPerSet;
    public float bpm;
    public float interval;
    public Vector3 fieldCenter;

    [Header("Prefabs")]
    public GameObject marcherPrefab;
    public GameObject positionSpherePrefab;

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
    public List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();
    private Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>> parsedCountPositions = new Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>();

    private IEnumerator Start()
    {
        SnapToGridLines.OnGridReady -= OnGridReadyHandler;
        SnapToGridLines.OnGridReady += OnGridReadyHandler;

        // Wait for JSON strings to be loaded into cache (by ShowSelectionManager)
        yield return new WaitUntil(() => session != null &&
                                    session.runtimeCacheSO != null &&
                                    !string.IsNullOrEmpty(session.runtimeCacheSO.CachedMarcherJSON) &&
                                    !string.IsNullOrEmpty(session.runtimeCacheSO.CachedTimingJSON) &&
                                    SessionManager.instance.JsonService != null); // Also wait for JsonService

        Debug.Log("Cached JSON strings found. Parsing using JsonPersistenceService...");

        // Call the service to parse the JSON strings and get the data structures back
        parsedCountPositions = SessionManager.instance.JsonService.ParseMarcherStateJSON(session.runtimeCacheSO.CachedMarcherJSON);

        // Assign the parsed map directly to the RuntimeCacheSO's map
        session.runtimeCacheSO.SetTimingMap = SessionManager.instance.JsonService.ParseSetTimingMapJSON(session.runtimeCacheSO.CachedTimingJSON);

        // Check if parsing was successful before proceeding
        if (parsedCountPositions == null || session.runtimeCacheSO.SetTimingMap == null)
        {
            Debug.LogError("Failed to parse JSON data via JsonPersistenceService. Aborting OnSessionReady.");
            // Potentially handle this error state (e.g., show UI message, prevent further execution)
            yield break; // Stop the coroutine
        }


        Debug.Log("JSON Parsing complete. Proceeding with OnSessionReady.");
        OnSessionReady(); // Now call OnSessionReady with parsed data available
    }
    void OnGridReadyHandler()
    {
        Debug.Log("✅ Grid Ready — Populating Marchers");
        //PopulateMarchers();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteCurrentSetPositions();
        }
    }
    public void OnSessionReady()
    {
        InitializeSession();
        setBar.OnTotalSetsChanged(numberOfSets);
        shapeMarchers.InitializeShapeManagers(marcherPrefab, interval);
        fieldCenter = fieldManager.GetFieldCenter();

        PopulateMarchers();
    }
    private void InitializeSession(){
        numberOfMarchers = session.showStateSO.NumberOfMarchers;
        numberOfSets = session.showStateSO.NumberOfSets;
        lastSet = int.Parse(session.showStateSO.LastSet);
        UIController.InitializeUI();
    }
    public void PopulateMarchers()
    {
        SnapToGridLines.OnGridReady -= PopulateMarchers;
        marchers.Clear();

        marchers = new List<MarcherPositionsManager>(GetComponentsInChildren<MarcherPositionsManager>());
        int currentMarcherCount = marchers.Count;

        if (currentMarcherCount < numberOfMarchers)
            AddMarchers(currentCount: currentMarcherCount);
        else if (currentMarcherCount > numberOfMarchers)
            RemoveExcessMarchers(currentCount: currentMarcherCount);

        bool usedSavedPositions = false;
        int lastSetNum = int.Parse(session.showStateSO.LastSet);
        int previousSet = Mathf.Max(1, lastSetNum - 1);

        // 🧠 Use SetTimingMap to determine fallback count value
        int fallbackCount = session.runtimeCacheSO.SetTimingMap.TryGetValue(previousSet, out var timing)
            ? timing.count
            : 1;

        foreach (var marcher in marchers)
        {
            marcher.InitializeSetCount(numberOfSets);

            // ✅ Inject saved count-based positions
            if (parsedCountPositions.TryGetValue(marcher.name, out var restored))
            {
                marcher.countPositions = restored;
                marcher.SyncInspectorList();
            }

            // ✅ Position based on fallback (Set before current, last count)
            if (marcher.HasPositionAtCount(previousSet, fallbackCount))
            {
                Vector3 startPos = marcher.GetPositionAtCount(previousSet, fallbackCount);
                marcher.transform.position = startPos;
                usedSavedPositions = true;
            }
            else
            {
                Debug.LogWarning($"{marcher.name} ⚠️ No saved position for Set {previousSet}, Count {fallbackCount}");
            }
        }

        setBar.OnSetButtonClick(lastSet);

        if (!usedSavedPositions)
        {
            ArrangeMarchersInSquare();
        }
        ConfirmInitialCenterPosition();
    }
    private void ConfirmInitialCenterPosition()
    {
        foreach (var marcher in marchers)
        {
            Vector3 pos = marcher.transform.position;
            marcher.SetPositionAtCount(0, 0, pos, "march");
            Debug.Log($"{marcher.name} 🔒 Confirmed Set 0, Count 0 at {pos}");
        }
    }
    private void AddMarchers(int currentCount)
    {
        for (int i = currentCount; i < numberOfMarchers; i++)
            CreateMarcher(i, new Color(Random.value, Random.value, Random.value), numberOfSets);
    }
    private void RemoveExcessMarchers(int currentCount)
    {
        for (int i = currentCount - 1; i >= numberOfMarchers; i--)
        {
            DestroyImmediate(marchers[i].gameObject);
            marchers.RemoveAt(i);
        }
    }
    public void PreviewCountPosition(int setNumber, int clickedCount)
    {
        var timingMap = session.runtimeCacheSO.SetTimingMap;

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

                Debug.Log($"🔍 {marcher.name} previewed at Set {setNumber}, Count {clickedCount} → {previewPos}");
            }
            else
            {
                Debug.LogWarning($"⚠️ {marcher.name} has no position data at Set {setNumber}, Count {clickedCount}");
            }

        }
    }
    private void CreateMarcher(int index, Color color, int sets)
    {
        Vector3 position = Vector3.zero;

        GameObject marcherObject = Instantiate(marcherPrefab, position, Quaternion.identity, transform);
        marcherObject.name = $"Marcher{index + 1}";

        MarcherPositionsManager marcher = marcherObject.GetComponent<MarcherPositionsManager>();
        MarcherController marcherController = marcherObject.GetComponent<MarcherController>();

        marcher.InitializeSetCount(sets); // ✅ REPLACED: no need for InitializeSets
        marcherController.InitializeMarcher(this, session.runtimeCacheSO);

        marchers.Add(marcher);
    }
    private void ArrangeMarchersInSquare()
    {
        if (shapeMarchers == null)
        {
            Debug.LogWarning("ShapeMarchers is null; cannot arrange formation.");
            return;
        }

        // Convert the list of marcher components into a list of GameObjects.
        List<GameObject> marcherObjects = new List<GameObject>();
        foreach (var marcher in marchers)
        {
            marcherObjects.Add(marcher.gameObject);
        }

        // Utilize ShapeMarchers to position marchers in a square.
        shapeMarchers.ArrangeFormation(
            ShapeMarchers.ShapeType.Box,  // Use a Box formation to create a square shape.
            intervalManager.GetIntervalType(interval),  // Set spacing based on the interval manager.
            marcherObjects  // List of all marcher GameObjects.
        );
    }
    public void RepositionMarchersToSet(int setIndex)
    {
        int targetSet = (setIndex == 1) ? 0 : setIndex - 1;
        int targetCount = 0;

        if (setIndex > 1)
        {
            // Look up last count of the previous set
            if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(targetSet, out var timing))
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
                Debug.LogWarning($"{marcher.name} has no position for Set {targetSet}, Count {targetCount}");
            }
        }
    }
    public void DeleteCurrentSetPositions()
    {
        int currentSet = int.Parse(session.showStateSO.LastSet);
        int fallbackSet = (currentSet == 1) ? 0 : currentSet - 1;
        int fallbackCount = 0;

        Debug.Log($"🗑 Deleting Set {currentSet} positions and reverting to Set {fallbackSet}, Count {fallbackCount}.");

        if (currentSet > 1)
        {
            if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var timing))
            {
                Debug.LogWarning($"⚠️ No SetTiming entry for Set {fallbackSet}");
                return;
            }

            fallbackCount = timing.count;
        }

        foreach (var marcher in marchers)
        {
            // Remove current set's counts
            if (marcher.countPositions.ContainsKey(currentSet))
            {
                marcher.countPositions.Remove(currentSet);
                marcher.SyncInspectorList();
            }

            // Reposition based on fallback logic
            if (marcher.HasPositionAtCount(fallbackSet, fallbackCount))
            {
                Vector3 fallbackPos = marcher.GetPositionAtCount(fallbackSet, fallbackCount);
                marcher.transform.position = fallbackPos;
                Debug.Log($"{marcher.name} ⬅️ Reverted to Set {fallbackSet}, Count {fallbackCount}: {fallbackPos}");
            }
            else
            {
                Debug.LogWarning($"{marcher.name} ⚠️ No fallback position for Set {fallbackSet}, Count {fallbackCount}.");
            }
        }

        Debug.Log("✅ All marcher positions for current set deleted and reverted.");
    }

    public string GenerateMarcherStateJSON()
    {
        return session.JsonService.GenerateMarcherStateJSON(marchers);
    }

    public string GenerateSetTimingMapJSON()
    {
        return session.JsonService.GenerateSetTimingMapJSON(session.runtimeCacheSO.SetTimingMap);
    }

    void OnDestroy()
    {
        SnapToGridLines.OnGridReady -= OnGridReadyHandler;
    }
}
