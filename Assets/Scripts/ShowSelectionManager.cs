using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SimpleJSON;
using System.IO;


public class ShowSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform scrollContent; // Parent container for show panels
    public GameObject panelCreateNewShowPrefab; // Prefab for creating a new show
    public GameObject panelSavedShowPrefab; // Prefab for saved shows

    private void Start()
    {
        StartCoroutine(WaitForSessionAndPopulate());
    }

    private IEnumerator WaitForSessionAndPopulate()
    {
        // Wait until the SessionManager has loaded user data
        while (SessionManager.instance == null || SessionManager.instance.savedShows == null)
        {
            Debug.Log("⏳ Waiting for user session to initialize...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("✅ User session detected, populating show selection.");
        PopulateShowSelection();
        StartCoroutine(PreFetchAndCacheAllJSONs());

    }

    private IEnumerator PreFetchAndCacheAllJSONs()
    {
        var shows = SessionManager.instance.savedShows;
        string localDir = Path.Combine(Application.persistentDataPath, "MADA_JSONS");
        Directory.CreateDirectory(localDir);

        foreach (var show in shows)
        {
            string marcherPath = Path.Combine(localDir, $"{show.showID}_marchers.json");
            string timingPath = Path.Combine(localDir, $"{show.showID}_timing.json");

            if (!File.Exists(marcherPath))
                yield return DownloadAndSaveJSON(SessionManager.instance.SessionState.JSONMarchersPositions, marcherPath, isMarcher: true);
            if (!File.Exists(timingPath))
                yield return DownloadAndSaveJSON(SessionManager.instance.SessionState.JSONSetTiming, timingPath, isMarcher: false);
        }

        Debug.Log("✅ Cached all JSONs locally for available shows.");
    }


    private void PopulateShowSelection()
    {
        // Clear existing children (if reloading)
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        List<SessionManager.ShowData> savedShows = SessionManager.instance.savedShows;

        if (savedShows.Count > 0)
        {
            // Populate saved shows dynamically
            foreach (SessionManager.ShowData show in savedShows)
            {
                GameObject savedShowPanel = Instantiate(panelSavedShowPrefab, scrollContent);
                ShowPanelUI panelUI = savedShowPanel.GetComponent<ShowPanelUI>();

                if (panelUI != null)
                {
                    panelUI.SetShowData(show);
                    panelUI.onShowSelected += OnShowSelected;
                }
            }
        }

        // Always add the "Create New Show" button at the end
        GameObject createNewShowPanel = Instantiate(panelCreateNewShowPrefab, scrollContent);
        createNewShowPanel.GetComponent<Button>().onClick.AddListener(() => CreateShowManager.instance.OpenCreateShowPanel());
    }

    public void OnShowSelected(SessionManager.ShowData show)
    {
        Debug.Log($"Selected Show: {show.showTitle}");
        
        // Store selected show data in a static variable or SessionManager
        if (!SceneController.instance.IsSceneCurrentlyLoading(4)) // Prevent duplicate loads
        {
            SessionManager.instance.selectedShow = show;
            StartCoroutine(FetchShowDetails(show.showSheetID));
        }
    }

    private IEnumerator FetchShowDetails(string sheetID)
    {
        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{sheetID}/values/Show Details!B1:B14?key={SessionManager.instance.apiKey}";
        Debug.Log($"🔗 Fetching show details from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Failed to fetch show details: {request.error}");
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            var showDataResponse = JSON.Parse(jsonResponse);

            if (showDataResponse["values"] != null && showDataResponse["values"].Count >= 10)
            {
                Debug.Log($"📥 Show Details Fetched: {showDataResponse.ToString()}");

                // Convert JSONNode values to strings before trimming
                string showID = showDataResponse["values"][0][0].Value.Trim();       // B1
                string title = showDataResponse["values"][1][0].Value.Trim();        // B2
                string group = showDataResponse["values"][2][0].Value.Trim();        // B3
                string fieldType = showDataResponse["values"][4][0].Value.Trim();    // B5
                string year = showDataResponse["values"][5][0].Value.Trim();         // B6
                
                // Safely parse integer values
                int marchers = TryParseInt(showDataResponse["values"][6][0].Value); // B7
                int sets = TryParseInt(showDataResponse["values"][7][0].Value);     // B8
                int props = TryParseInt(showDataResponse["values"][8][0].Value);    // B9
                
                string modified = showDataResponse["values"][9][0].Value.Trim();         // B10
                string status = showDataResponse["values"][10][0].Value.Trim();          // B11
                string setOnExit = showDataResponse ["values"][11][0].Value.Trim();      // B12
                string JSONMarching = showDataResponse ["values"][12][0];                // B13
                string JSONTiming = showDataResponse ["values"][13][0];                  // B14

                // Save to SessionManager
                SessionManager.instance.SaveToSessionManager(
                    showID, title, group, fieldType, year, marchers, sets, props, modified, status, setOnExit, JSONMarching, JSONTiming
                );

                StartCoroutine(LoadShowJSONThenPopulate());
            }
            else
            {
                Debug.LogError("❌ Error: No valid show data found or missing expected rows.");
            }
        }
    }
    private IEnumerator LoadShowJSONThenPopulate()
    {
        // Load Marcher JSON
        string localDir = Path.Combine(Application.persistentDataPath, "MADA_JSONS");
        string localMarcherPath = Path.Combine(localDir, $"{SessionManager.instance.SessionState.CurrentShowID}_marchers.json");

        bool needDownloadMarcher = false;

        if (File.Exists(localMarcherPath))
        {
            try
            {
                SessionState.marcherJsonText = File.ReadAllText(localMarcherPath);
                Debug.Log("📥 Loaded marcher JSON from local.");
            }
            catch
            {
                Debug.LogWarning("⚠️ Failed to read local marcher JSON. Will re-download.");
                needDownloadMarcher = true;
            }
        }
        else
        {
            Debug.Log("📥 Marcher JSON not found locally. Downloading...");
            needDownloadMarcher = true;
        }

        if (needDownloadMarcher)
            yield return DownloadAndSaveJSON(SessionManager.instance.SessionState.JSONMarchersPositions, localMarcherPath, isMarcher: true);



        // Load Timing JSON
        string localTimingPath = Path.Combine(localDir, $"{SessionManager.instance.SessionState.CurrentShowID}_timing.json");
        
        bool needDownloadTiming = false;

        if (File.Exists(localTimingPath))
        {
            try
            {
                SessionState.timingJsonText = File.ReadAllText(localTimingPath);
                Debug.Log("📥 Loaded timing JSON from local.");
            }
            catch
            {
                Debug.LogWarning("⚠️ Failed to read local timing JSON. Will re-download.");
                needDownloadTiming = true;
            }
        }
        else
        {
            Debug.Log("📥 Timing JSON not found locally. Downloading...");
            needDownloadTiming = true;
        }

        if (needDownloadTiming)
            yield return DownloadAndSaveJSON(SessionManager.instance.SessionState.JSONSetTiming, localTimingPath, isMarcher: false);


        SceneController.instance.SwitchScene(4);
    }

    private IEnumerator DownloadAndSaveJSON(string url, string localPath, bool isMarcher)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            File.WriteAllText(localPath, request.downloadHandler.text);

            if (isMarcher)
                SessionState.marcherJsonText = request.downloadHandler.text;
            else
                SessionState.timingJsonText = request.downloadHandler.text;

            Debug.Log($"📦 Saved JSON locally: {localPath}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Failed to download JSON: {url} — {request.error}");
        }
    }


    private int TryParseInt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogWarning($"⚠️ Empty or null value for integer conversion, defaulting to 0.");
            return 0;
        }

        int result;
        if (int.TryParse(value.Trim(), out result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"⚠️ Failed to parse integer from: '{value}', defaulting to 0.");
            return 0; // Default to 0 if parsing fails
        }
    }
}
