using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;

[ExecuteInEditMode]
public class EnsembleDirector2 : MonoBehaviour
{
    SessionManager session = SessionManager.instance;

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
    private Dictionary<string, Dictionary<int, Vector3>> parsedSetPositions = new Dictionary<string, Dictionary<int, Vector3>>();
    private Dictionary<string, Dictionary<int, Vector3>> parsedStandbyPositions = new Dictionary<string, Dictionary<int, Vector3>>();


    private IEnumerator Start()
    {
        SnapToGridLines.OnGridReady -= OnGridReadyHandler;
        SnapToGridLines.OnGridReady += OnGridReadyHandler;
        
        yield return new WaitUntil(() => !string.IsNullOrEmpty(SessionState.marcherJsonText) && !string.IsNullOrEmpty(SessionState.timingJsonText));
        LoadMarcherStateFromJSON(SessionState.marcherJsonText);
        LoadSetTimingMapFromJSON(SessionState.timingJsonText);

        OnSessionReady();
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
    void OnGridReadyHandler()
    {
        Debug.Log("✅ Grid Ready — Populating Marchers");
        //PopulateMarchers();
    }
    private void InitializeSession(){
        numberOfMarchers = session.SessionState.NumberOfMarchers;
        numberOfSets = session.SessionState.NumberOfSets;
        lastSet = int.Parse(session.SessionState.LastSet);
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
        int lastSetNum = int.Parse(session.SessionState.LastSet);

        foreach (var marcher in marchers)
        {
            marcher.InitializeSetCount(numberOfSets);

            // Inject saved positions into the marcher manager
            if (parsedSetPositions.ContainsKey(marcher.name))
            {
                foreach (var kvp in parsedSetPositions[marcher.name])
                    marcher.setPositions[kvp.Key] = kvp.Value;
            }

            if (parsedStandbyPositions.ContainsKey(marcher.name))
            {
                foreach (var kvp in parsedStandbyPositions[marcher.name])
                    marcher.standbyPositions[kvp.Key] = kvp.Value;
            }

            // ✅ Safe to do after all marcher logic is done
            setBar.OnSetButtonClick(lastSet);

            // Set transform position based on best available data
            if (marcher.setPositions.ContainsKey(lastSetNum))
            {
                marcher.transform.position = marcher.setPositions[lastSetNum];
                usedSavedPositions = true;
            }
            else if (marcher.standbyPositions.ContainsKey(lastSetNum))
            {
                marcher.transform.position = marcher.standbyPositions[lastSetNum];
                usedSavedPositions = true;
            }
        }

        if (!usedSavedPositions)
        {
            ArrangeMarchersInSquare();
        }
    }

    private void AddMarchers(int currentCount)
    {
        for (int i = currentCount; i < numberOfMarchers; i++)
            CreateMarcher(i, new Color(Random.value, Random.value, Random.value), numberOfSets);
    }

    public void PreviewCountPosition(int setNumber, int clickedCount)
    {
        var timingMap = SessionManager.instance.SessionState.SetTimingMap;

        if (!timingMap.TryGetValue(setNumber, out var timing))
        {
            Debug.LogWarning($"❌ No timing data found for Set {setNumber}");
            return;
        }

        if (!timingMap.ContainsKey(setNumber + 1))
        {
            Debug.LogWarning($"❌ Cannot preview count: Set {setNumber + 1} does not exist.");
            return;
        }

        int totalCounts = Mathf.Max(1, timing.count);
        float t = Mathf.Clamp01(clickedCount / (float)totalCounts);

        Debug.Log($"🧠 Previewing Count {clickedCount} of {totalCounts} in Set {setNumber}");
        Debug.Log($"➡ Interpolation factor t = {t:F3}");

        foreach (var marcher in marchers)
        {
            if (marcher.setPositions.TryGetValue(setNumber, out var startPos) &&
                marcher.setPositions.TryGetValue(setNumber + 1, out var endPos))
            {
                Vector3 interpolatedPos = Vector3.Lerp(startPos, endPos, t);

                Debug.Log($"🔄 {marcher.name}:");
                Debug.Log($"   StartPos (Set {setNumber})    = {startPos}");
                Debug.Log($"   EndPos   (Set {setNumber + 1}) = {endPos}");
                Debug.Log($"   Result   (Interpolated)       = {interpolatedPos}");

                marcher.transform.position = interpolatedPos;
            }
            else
            {
                Debug.LogWarning($"⚠️ {marcher.name} is missing setPosition data for Set {setNumber} or Set {setNumber + 1}");
            }
        }
    }



    private void RemoveExcessMarchers(int currentCount)
    {
        for (int i = currentCount - 1; i >= numberOfMarchers; i--)
        {
            DestroyImmediate(marchers[i].gameObject);
            marchers.RemoveAt(i);
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
        marcherController.InitializeMarcher(this);

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

    public string GenerateMarcherStateJSON()
    {
        var root = new JSONObject();
        root["version"] = "1.0.0";
        root["timestamp"] = System.DateTime.UtcNow.ToString("o");

        var marcherArray = new JSONArray();

        foreach (var marcher in marchers)
        {
            var marcherNode = new JSONObject();
            marcherNode["id"] = marcher.name;

            var setPosNode = new JSONObject();
            foreach (var kvp in marcher.setPositions)
            {
                var setArray = new JSONArray();
                setArray.Add(kvp.Value.x);
                setArray.Add(kvp.Value.y);
                setArray.Add(kvp.Value.z);
                setPosNode[kvp.Key.ToString()] = setArray;

            }

            var standbyPosNode = new JSONObject();
            foreach (var kvp in marcher.standbyPositions)
            {
                var standbyArray = new JSONArray();
                standbyArray.Add(kvp.Value.x);
                standbyArray.Add(kvp.Value.y);
                standbyArray.Add(kvp.Value.z);
                standbyPosNode[kvp.Key.ToString()] = standbyArray;
            }

            marcherNode["setPositions"] = setPosNode;
            marcherNode["standbyPositions"] = standbyPosNode;

            marcherArray.Add(marcherNode);
        }

        root["marchers"] = marcherArray;

        return root.ToString(2); // Pretty print with indent
    }

    public string SaveMarcherStateToFile()
    {
        string json = GenerateMarcherStateJSON();
        string showTitle = SessionManager.instance.SessionState.ShowTitle;
        string sanitizedTitle = string.Join("_", showTitle.Split(Path.GetInvalidFileNameChars()));
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{sanitizedTitle}_{timestamp}_marcher_positions.json";

        string directory = Path.Combine(Application.dataPath, "SavedJSON");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = Path.Combine(directory, filename);
        File.WriteAllText(path, json);
        Debug.Log("✅ Marcher state JSON saved to: " + path);

    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif

        return path; // 🔥 Return full path so ShowDataManager can upload it
    }

    public void LoadMarcherStateFromJSON(string jsonText)
    {
        Debug.Log("Attempting to Load JSON position values to marchers");

        parsedSetPositions.Clear();
        parsedStandbyPositions.Clear();

        var root = JSON.Parse(jsonText);
        var marcherArray = root["marchers"].AsArray;

        for (int i = 0; i < marcherArray.Count; i++)
        {
            var marcherData = marcherArray[i];
            string id = marcherData["id"];

            var setPositions = marcherData["setPositions"].AsObject;
            var standbyPositions = marcherData["standbyPositions"].AsObject;

            Dictionary<int, Vector3> setsDict = new Dictionary<int, Vector3>();
            Dictionary<int, Vector3> standbyDict = new Dictionary<int, Vector3>();

            foreach (KeyValuePair<string, JSONNode> kvp in setPositions)
            {
                int set = int.Parse(kvp.Key);
                var vec = kvp.Value.AsArray;
                setsDict[set] = new Vector3(vec[0].AsFloat, vec[1].AsFloat, vec[2].AsFloat);
            }

            foreach (KeyValuePair<string, JSONNode> kvp in standbyPositions)
            {
                int set = int.Parse(kvp.Key);
                var vec = kvp.Value.AsArray;
                standbyDict[set] = new Vector3(vec[0].AsFloat, vec[1].AsFloat, vec[2].AsFloat);
            }

            parsedSetPositions[id] = setsDict;
            parsedStandbyPositions[id] = standbyDict;

            Debug.Log($"🔁 Cached all positions for {id}");
        }
    }

    public void RepositionMarchersToSet(int setIndex)
    {
        foreach (var marcher in marchers)
        {
            Vector3 newPosition;

            if (marcher.setPositions.TryGetValue(setIndex, out newPosition))
            {
                marcher.transform.position = newPosition;
                Debug.Log($"{marcher.name} repositioned to SetPosition for set {setIndex}");
            }
            else if (marcher.standbyPositions.TryGetValue(setIndex, out newPosition))
            {
                marcher.transform.position = newPosition;
                Debug.Log($"{marcher.name} repositioned to StandbyPosition for set {setIndex}");
            }
            else
            {
                Debug.Log($"{marcher.name} has no saved position for set {setIndex}");
            }
        }
    }

    public string SaveSetTimingMapToFile()
    {
        var root = new JSONObject();

        foreach (var entry in SessionManager.instance.SessionState.SetTimingMap)
        {
            var setIndex = entry.Key;
            var data = entry.Value;

            JSONObject setNode = new JSONObject();
            setNode["count"] = data.count;
            setNode["startBPM"] = data.startBPM;
            setNode["endBPM"] = data.endBPM;

            root[setIndex.ToString()] = setNode;
        }

        string json = root.ToString(2); // Pretty print
        string showTitle = SessionManager.instance.SessionState.ShowTitle;
        string sanitizedTitle = string.Join("_", showTitle.Split(Path.GetInvalidFileNameChars()));
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{sanitizedTitle}_{timestamp}_set_timing.json";

        string directory = Path.Combine(Application.dataPath, "SavedJSON");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string path = Path.Combine(directory, filename);
        File.WriteAllText(path, json);

        Debug.Log("✅ SetTiming JSON saved to: " + path);
    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif
        return path;
    }

    public void LoadSetTimingMapFromJSON(string jsonText)
    {
        var json = JSON.Parse(jsonText);
        var map = SessionManager.instance.SessionState.SetTimingMap;
        map.Clear();

        foreach (KeyValuePair<string, JSONNode> kvp in json.AsObject)
        {
            int setIndex = int.Parse(kvp.Key);
            int count = kvp.Value["count"];
            float startBPM = kvp.Value["startBPM"];
            float endBPM = kvp.Value["endBPM"];

            map[setIndex] = new SessionState.SetTimingData(setIndex, count, startBPM, endBPM);
        }

        Debug.Log($"✅ Loaded {map.Count} SetTiming entries into SessionState.");
    }

    public void DeleteCurrentSetPositions()
    {
        int currentSet = int.Parse(SessionManager.instance.SessionState.LastSet);
        int previousSet = Mathf.Max(1, currentSet - 1);

        Debug.Log($"🗑 Deleting Set {currentSet} positions and reverting to Set {previousSet}.");

        foreach (var marcher in marchers)
        {
            // Remove the current set's data
            marcher.setPositions.Remove(currentSet);
            marcher.standbyPositions.Remove(currentSet);
            marcher.SyncInspectorLists();

            // Snap marcher to last known position
            Vector3 newPosition;
            if (marcher.setPositions.TryGetValue(previousSet, out newPosition) ||
                marcher.standbyPositions.TryGetValue(previousSet, out newPosition))
            {
                marcher.transform.position = newPosition;
                Debug.Log($"{marcher.name} ⬅️ Reverted to Set {previousSet} position: {newPosition}");
            }
            else
            {
                Debug.LogWarning($"{marcher.name} ⚠️ No fallback position for Set {previousSet}.");
            }
        }

        Debug.Log("✅ All marcher positions updated.");
    }


    void OnDestroy()
    {
        SnapToGridLines.OnGridReady -= OnGridReadyHandler;
    }
}
