using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using SimpleJSON; // Assuming you continue using SimpleJSON

/// <summary>
/// Manages the UI for selecting existing shows or creating new ones.
/// Handles fetching show metadata, triggering the loading of show JSON data (via JsonPersistenceService),
/// and coordinating the transition to the Show Manager scene.
/// </summary>
public class ShowSelectionManager : MonoBehaviour
{
    // --- Dependencies ---
    private SessionManager session; // Cached reference to the SessionManager instance

    [Header("UI References")]
    [Tooltip("Parent GameObject where show list panels will be instantiated")]
    public Transform scrollContent;
    [Tooltip("Prefab for the 'Create New Show' button/panel")]
    public GameObject panelCreateNewShowPrefab;
    [Tooltip("Prefab for displaying an existing saved show")]
    public GameObject panelSavedShowPrefab;

    //================================================================================
    #region Lifecycle Methods
    //================================================================================

    private void Start()
    {
        // Cache SessionManager instance and perform null check
        session = SessionManager.instance;
        if (session == null)
        {
            Debug.LogError("ShowSelectionManager: SessionManager instance not found! This script requires SessionManager to function.");
            this.enabled = false; // Disable script if session manager is missing
            return;
        }
        StartCoroutine(WaitForSessionAndPopulate());
    }

    /// <summary>
    /// Waits for the SessionManager to be fully initialized (including user login and JsonService)
    /// before populating the show list UI and starting the pre-fetch process.
    /// </summary>
    private IEnumerator WaitForSessionAndPopulate()
    {
        while (session == null || session.savedShows == null || session.JsonService == null)
        {
            Debug.Log("⏳ Waiting for user session and JsonService to initialize...");
            if (session == null) session = SessionManager.instance; // Retry getting instance
            yield return new WaitForSeconds(0.5f); // Wait before checking again
        }

        Debug.Log("✅ User session detected, populating show selection UI.");
        PopulateShowSelection();

        // Start pre-fetching JSON data for existing shows in the background
        _ = PreFetchAndCacheAllJSONsAsync(); // fire and forget
    }

     /// <summary>
     /// Clean up event listeners when the object is destroyed to prevent memory leaks.
     /// </summary>
     private void OnDestroy()
     {
         if (scrollContent != null)
         {
             // Iterate through instantiated show panels and unsubscribe from the selection event
             foreach (Transform child in scrollContent)
             {
                 if (child != null && child.TryGetComponent<ShowPanelUI>(out var panelUI))
                 {
                     panelUI.onShowSelected -= OnShowSelected;
                 }
             }
         }
     }

    #endregion

    //================================================================================
    #region UI Population
    //================================================================================

    /// <summary>
    /// Clears the existing show list UI and repopulates it based on the shows
    /// currently loaded in the SessionManager. Also adds the 'Create New Show' button.
    /// </summary>
    private void PopulateShowSelection()
    {
        // Clear existing UI elements first
        foreach (Transform child in scrollContent)
        {
            if (child != null) Destroy(child.gameObject);
        }

        List<SessionManager.ShowData> savedShows = session.savedShows;

        // Populate panels for each saved show
        if (savedShows != null && savedShows.Count > 0)
        {
            foreach (SessionManager.ShowData show in savedShows)
            {
                 // Basic validation for show data before creating UI
                 if (show == null || string.IsNullOrEmpty(show.showID))
                 {
                     Debug.LogWarning("Skipping invalid show data entry during UI population.");
                     continue;
                 }

                GameObject savedShowPanel = Instantiate(panelSavedShowPrefab, scrollContent);
                if (savedShowPanel.TryGetComponent<ShowPanelUI>(out var panelUI))
                {
                    panelUI.SetShowData(show);
                    // Ensure listener is removed before adding to prevent duplicates
                    panelUI.onShowSelected -= OnShowSelected;
                    panelUI.onShowSelected += OnShowSelected;
                }
                else
                {
                     Debug.LogError($"Prefab '{panelSavedShowPrefab.name}' is missing the ShowPanelUI component!");
                     Destroy(savedShowPanel); // Clean up invalid prefab instance
                }
            }
        } else {
             Debug.Log("No saved shows to display in the UI.");
        }

        // Always add the "Create New Show" button/panel at the end
        GameObject createNewShowPanel = Instantiate(panelCreateNewShowPrefab, scrollContent);
        if (createNewShowPanel.TryGetComponent<Button>(out var createButton))
        {
             // Ensure CreateShowManager instance exists before assigning listener
            if (CreateShowManager.instance != null) {
                 createButton.onClick.AddListener(() => CreateShowManager.instance.OpenCreateShowPanel());
            } else {
                 Debug.LogError("CreateShowManager instance not found! Cannot assign 'Create New Show' button action.");
                 createButton.interactable = false; // Disable button if manager is missing
            }
        } else {
             Debug.LogError($"Prefab '{panelCreateNewShowPrefab.name}' is missing the Button component!");
             Destroy(createNewShowPanel);
        }
    }

    #endregion

    //================================================================================
    #region Show Selection Flow
    //================================================================================

    /// <summary>
    /// Called when a user clicks on an existing show panel in the UI.
    /// Stores the selected show data and initiates the process of fetching its details.
    /// </summary>
    /// <param name="show">The data associated with the selected show.</param>
    public void OnShowSelected(SessionManager.ShowData show)
    {
        if (show == null || string.IsNullOrEmpty(show.showID))
        {
            Debug.LogError("OnShowSelected called with invalid show data.");
            return;
        }

        // Ensure required managers are available
        if (session == null || SceneController.instance == null)
        {
             Debug.LogError("Cannot proceed with show selection: SessionManager or SceneController is null.");
             return;
        }

        // Prevent duplicate scene loads if already transitioning to the Show Manager scene (index 4)
        if (!SceneController.instance.IsSceneCurrentlyLoading(4))
        {
            session.selectedShow = show;
            // Start the process by fetching the detailed metadata from the show's specific Google Sheet
            Debug.Log($"Selected Show: {show.showTitle} (ID: {show.showID})");
            StartCoroutine(FetchShowDetails(show.showSheetID));
        } else {
             Debug.LogWarning("Scene 4 (ShowManagerScene) is already loading, skipping OnShowSelected action.");
        }
    }

    #endregion

    //================================================================================
    #region Data Fetching Coroutines
    //================================================================================

    /// <summary>
    /// Fetches the detailed metadata for a selected show directly from its specific Google Sheet.
    /// This metadata includes counts, names, etc., but not the large JSON position/timing data.
    /// </summary>
    /// <param name="sheetID">The Google Sheet ID for the specific show.</param>
    private IEnumerator FetchShowDetails(string sheetID)
    {
         // Pre-flight checks
         if (string.IsNullOrEmpty(session.apiKey) || string.IsNullOrEmpty(sheetID))
         {
              Debug.LogError($"FetchShowDetails: Missing API Key or Sheet ID. Cannot fetch details.");
              yield break; // Stop if essential info is missing
         }

        // Construct the URL for the Google Sheets API v4
        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{sheetID}/values/Show Details!B1:B12?key={session.apiKey}";
        Debug.Log($"🔗 Fetching show details metadata from: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Set User-Agent header, good practice for web requests
            request.SetRequestHeader("User-Agent", "UnityWebRequest");

            yield return request.SendWebRequest();

            // Handle network or protocol errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Failed to fetch show details: {request.error} (URL: {url})");
                // TODO: Implement user-facing error message (e.g., "Could not load show details. Please check connection.")
                yield break;
            }

            // Process successful response
            string jsonResponse = request.downloadHandler.text;
            JSONNode showDataResponse = null;
            try
            {
                showDataResponse = JSON.Parse(jsonResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ JSON Parse Error fetching details: {e.Message}\nRaw Response: {jsonResponse}");
                 // TODO: Implement user-facing error message
                yield break;
            }

            // Validate the structure of the received JSON data
            // Expecting 'values' array with at least 12 rows (B1 to B12)
            if (showDataResponse?["values"] != null && showDataResponse["values"].Count >= 12)
            {
                Debug.Log($"📥 Show Details Metadata Fetched Successfully.");

                // Helper function for safer access to potentially missing values in the JSON response
                string GetValue(int rowIndex) => showDataResponse["values"][rowIndex]?[0]?.Value ?? "";

                // Extract data
                string showID = GetValue(0).Trim();       // B1
                string title = GetValue(1).Trim();        // B2
                string group = GetValue(2).Trim();        // B3
                string fieldType = GetValue(4).Trim();    // B5
                string year = GetValue(5).Trim();         // B6
                int marchers = TryParseInt(GetValue(6)); // B7
                int sets = TryParseInt(GetValue(7));     // B8
                int props = TryParseInt(GetValue(8));    // B9
                string modified = GetValue(9).Trim();     // B10
                string status = GetValue(10).Trim();      // B11
                string setOnExit = GetValue(11).Trim();   // B12

                // Get the JSON links from the initially loaded ShowData (these point to Drive/Backend, not fetched here)
                string JSONMarching = session.selectedShow?.marcherJSONLink ?? "";
                string JSONTiming = session.selectedShow?.timingJSONLink ?? "";

                // --- Critical Check ---
                // Ensure the showID fetched from the sheet matches the selected show's ID
                if (showID != session.selectedShow?.showID)
                {
                    Debug.LogError($"❌ Mismatch between selected show ID ('{session.selectedShow?.showID}') and ID fetched from sheet ('{showID}')! Aborting load.");
                     // TODO: Implement user-facing error message
                    yield break;
                }

                // Save the fetched metadata into the persistent ShowStateSO via SessionManager
                session.SaveToSessionManager(
                    showID, title, group, fieldType, year, marchers, sets, props, modified, status, setOnExit, JSONMarching, JSONTiming
                );

                // Now that metadata is loaded, trigger the loading of the large JSON files (Position/Timing)
                // This will use the JsonPersistenceService (cache check / backend download)
                StartCoroutine(LoadShowJsonData()); // Renamed for clarity
            }
            else
            {
                Debug.LogError($"❌ Error: Invalid show details data format or missing rows in response from {url}. Received: {jsonResponse}");
                 // TODO: Implement user-facing error message
            }
        } // UnityWebRequest is disposed here
    }

    /// <summary>
    /// Coordinates the loading of Marcher and Timing JSON data using the JsonPersistenceService.
    /// This handles checking the local cache first, then requesting data from the backend if needed.
    /// Switches to the ShowManager scene upon successful loading of both JSON files.
    /// </summary>
    private IEnumerator LoadShowJsonData()
    {
        if (session.JsonService == null || session.showStateSO == null || string.IsNullOrEmpty(session.showStateSO.CurrentShowID))
        {
            Debug.LogError("LoadShowJsonData: JsonService or ShowStateSO not ready.");
            yield break;
        }

        string showId = session.showStateSO.CurrentShowID;
        string marcherJson = session.JsonService.LoadJsonFromCache(showId, "marcher");
        string timingJson = session.JsonService.LoadJsonFromCache(showId, "timing");

        if (string.IsNullOrEmpty(marcherJson) || string.IsNullOrEmpty(timingJson))
        {
            Debug.LogError("❌ Missing cached JSON.");
            yield break;
        }

        bool parsedMarcher = false;
        bool parsedTiming = false;

        float timeout = 10f;
        float timer = 0f;

        StartCoroutine(session.JsonService.ParseMarcherStateJSONAsync(marcherJson, (countPos, identities) =>
        {
            if (countPos != null && countPos.Count > 0)
            {
                session.runtimeCacheSO.CachedMarcherJSON = marcherJson;
                session.runtimeCacheSO.ParsedCountPositions = countPos;
                session.runtimeCacheSO.ParsedIdentities = identities;
                parsedMarcher = true;
                Debug.Log("✅ Parsed marcher JSON");
            }
            else Debug.LogError("❌ Failed to parse marcher JSON");
        }));

        StartCoroutine(session.JsonService.ParseSetTimingMapJSONAsync(timingJson, (timingMap) =>
        {
            if (timingMap != null && timingMap.Count > 0)
            {
                session.runtimeCacheSO.CachedTimingJSON = timingJson;
                session.runtimeCacheSO.SetTimingMap = timingMap;
                parsedTiming = true;
                Debug.Log("✅ Parsed timing JSON");
            }
            else Debug.LogError("❌ Failed to parse timing JSON");
        }));

        // ⏳ Wait with timeout watchdog
        while (!(parsedMarcher && parsedTiming) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (parsedMarcher && parsedTiming)
        {
            Debug.Log("🚀 JSON parse complete. Proceeding to ShowManagerScene.");
            SceneController.instance?.SwitchScene(4);
        }
        else
        {
            Debug.LogError($"❌ JSON parse timed out after {timeout} seconds. Aborting.");
            // Optional: Show retry UI or debug overlay
        }
    }

     /// <summary>
    /// Attempts to pre-fetch/cache JSON data for all known shows in the background.
    /// Uses the JsonPersistenceService to handle cache checks and downloads.
    /// </summary>
    // private IEnumerator PreFetchAndCacheAllJSONs()
    // {
    //     if (session.savedShows == null || session.savedShows.Count == 0)
    //     {
    //         Debug.Log("ℹ️ No saved shows found to prefetch.");
    //         yield break;
    //     }
    //     if (session.JsonService == null)
    //     {
    //          Debug.LogError("PreFetchAndCacheAllJSONs: JsonService is not available. Skipping prefetch.");
    //          yield break;
    //     }
    //
    //     var showsToPrefetch = new List<SessionManager.ShowData>(session.savedShows);
    //     Debug.Log($"🧠 Starting background prefetch/cache check for {showsToPrefetch.Count} shows...");
    //
    //     // Define a small delay to potentially spread out requests if hitting a backend
    //     var delay = new WaitForSeconds(0.1f); // 100ms delay between shows
    //
    //     foreach (var show in showsToPrefetch)
    //     {
    //         if (string.IsNullOrEmpty(show.showID)) continue; // Skip invalid show entries
    //
    //         // Use GetJson for its cache-check/download logic. We discard the result here.
    //         session.JsonService.GetJson(show.showID, "marcher", _ => { /* No action needed on result */ });
    //         session.JsonService.GetJson(show.showID, "timing", _ => { /* No action needed on result */ });
    //
    //         yield return delay; // Wait briefly before starting the next show's requests
    //     }
    //
    //     Debug.Log("🎉 Finished initiating background prefetch requests.");
    // }
    private async Task PreFetchAndCacheAllJSONsAsync()
    {
        if (session.savedShows == null || session.savedShows.Count == 0)
        {
            Debug.Log("ℹ️ No saved shows found to prefetch.");
            return;
        }

        if (session.JsonService == null)
        {
            Debug.LogError("PreFetchAndCacheAllJSONs: JsonService is not available. Skipping prefetch.");
            return;
        }

        var showsToPrefetch = new List<SessionManager.ShowData>(session.savedShows);
        Debug.Log($"🧠 Starting background prefetch/cache check for {showsToPrefetch.Count} shows...");

        float totalStart = Time.realtimeSinceStartup;

        foreach (var show in showsToPrefetch)
        {
            if (string.IsNullOrEmpty(show.showID)) continue;

            float start = Time.realtimeSinceStartup;

            // 🚀 Launch both fetches in parallel
            Task<string> marcherTask = session.JsonService.GetJsonAsync(show.showID, "marcher");
            Task<string> timingTask = session.JsonService.GetJsonAsync(show.showID, "timing");

            // ✅ Await both to complete before moving to next show
            await Task.WhenAll(marcherTask, timingTask);

            float duration = Time.realtimeSinceStartup - start;
            Debug.Log($"📦 Fetched marcher + timing JSON for {show.showID} in {duration:F2} sec");

            await Task.Delay(100); // throttle next round to avoid API hammering
        }

        float totalElapsed = Time.realtimeSinceStartup - totalStart;
        Debug.Log($"🎉 Finished downloading all JSON metadata. (⏱ Total: {totalElapsed:F2} sec)");
    }

    #endregion

    //================================================================================
    #region Utility Methods
    //================================================================================

    /// <summary>
    /// Safely parses a string into an integer. Returns 0 if parsing fails or input is null/empty.
    /// Logs a warning if parsing fails.
    /// </summary>
    private int TryParseInt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            // Don't warn if empty is expected, just return 0
            return 0;
        }
        if (int.TryParse(value.Trim(), out int result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"⚠️ Failed to parse integer from: '{value}', defaulting to 0.");
            return 0;
        }
    }

    #endregion
}
