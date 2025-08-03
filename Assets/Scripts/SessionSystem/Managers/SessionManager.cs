using System; // Added for Exception handling
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

/// <summary>
/// Persistent Singleton manager responsible for the overall application session.
/// Handles user state, show state, configuration loading, access to sub-managers/services,
/// and coordinates major application flows like login, logout, and show loading.
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager instance;

    [Header("Injected References (ScriptableObjects)")]
    public UserStateSO userStateSO;
    public ShowStateSO showStateSO;
    public RuntimeCacheSO runtimeCacheSO;

    [Header("Managed Services & Managers")]
    public UserSessionManager userSession = new UserSessionManager();
    public ShowDataManager showDataManager;
    public GoogleSheetsService sheetsService;
    public JsonCoordinatorService JsonService { get; private set; }

    [Header("Configuration")]
    public string apiKey;
    public static string backendURL;

    [Header("Session Data")]
    public List<ShowData> savedShows = new List<ShowData>();
    public ShowData selectedShow;

    //================================================================================
    #region Lifecycle Methods (Awake)
    //================================================================================
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("SessionManager: Duplicate SessionManager instance detected. Destroying self.");
            Destroy(gameObject);
        }
    }
    #endregion

    //================================================================================
    #region Initialization & Configuration
    //================================================================================

    /// <summary>
    /// Clears ScriptableObject states and local lists for a fresh session start.
    /// </summary>
    public void InitializeSessionState()
    {
        savedShows.Clear();
        if (userStateSO != null) userStateSO.Clear(); 
        if (showStateSO != null) showStateSO.Clear(); 
        if (runtimeCacheSO != null) runtimeCacheSO.Clear();
        Debug.Log("SessionManager: Session state cleared.");
        
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(LoadConfigurationWebGL_ThenInitialize());
#else
        LoadConfigurationLocal();
#endif

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(backendURL))
        {
            Debug.LogWarning("SessionManager: API Key or Backend URL not loaded. Aborting service initialization.");
            return;
        }

        userSession.InjectUserState(userStateSO);
    }

    /// <summary>
    /// Loads configuration values (API Key, Backend URL) from config.json in StreamingAssets.
    /// </summary>
    private void LoadConfigurationLocal()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (File.Exists(configPath))
        {
            try
            {
                string configContent = File.ReadAllText(configPath);
                ParseConfig(configContent);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌SessionManager: Error reading or parsing config.json: {e.Message}");
                apiKey = null;
                backendURL = null;
            }
        }
        else
        {
            Debug.LogError($"❌SessionManager: config.json not found at path: {configPath}. API Key and Backend URL will be unavailable.");
            apiKey = null;
            backendURL = null;
        }
    }

    private IEnumerator LoadConfigurationWebGL_ThenInitialize()
    {
        yield return LoadConfigurationWebGL();
        FinalizeInitialization();
    }

    private IEnumerator LoadConfigurationWebGL()
    {
        // Read values injected by ConfigLoader
        apiKey = PlayerPrefs.GetString("MADA_API_KEY", "");
        backendURL = PlayerPrefs.GetString("MADA_BACKEND_URL", "");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(backendURL))
        {
            Debug.LogError("❌SessionManager: Config values missing from PlayerPrefs in WebGL.");
        }
        else
        {
            Debug.Log("✅SessionManager: API key and Backend URL loaded from PlayerPrefs.");
        }

        yield return null; // still behave like a coroutine
    }

    private void FinalizeInitialization()
    {
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(backendURL))
        {
            Debug.LogError("SessionManager: API Key or Backend URL not loaded. Aborting service initialization.");
            return;
        }

        userSession.InjectUserState(userStateSO);
    }

    private void ParseConfig(string configContent)
    {
        var configJson = JSON.Parse(configContent);
        apiKey = configJson["googleApiKey"];
        backendURL = configJson["backendURL"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(backendURL))
        {
            Debug.LogError("❌SessionManager: config.json is missing 'googleApiKey' or 'backendURL'.");
        }
        else
        {
            Debug.Log("✅SessionManager: API key and Backend URL loaded successfully.");
        }
    }

    /// <summary>
    /// Instantiates and initializes the various services managed by SessionManager.
    /// Requires configuration (API Key, Backend URL) to be loaded first.
    /// </summary>
    public void InitializeJsonService()
    {
        if (!string.IsNullOrEmpty(userStateSO.UserFolderId) && !string.IsNullOrEmpty(userStateSO.AccountSheetID))
        {
            JsonService = new JsonCoordinatorService(
                new JsonGenerationService(),
                new JsonParserService(),
                new JsonCacheService(),
                new JsonBackendService(this, backendURL, userStateSO.UserFolderId, userStateSO.AccountSheetID)
            );

            sheetsService = new GoogleSheetsService(apiKey, this);

            Debug.Log("✅SessionManager: JsonService initialized after user state was loaded.");
        }
        else
        {
            Debug.LogError("❌SessionManager: Cannot initialize JsonService: userFolderId or accountSheetId is missing.");
        }
    }

    #endregion

    //================================================================================
    #region Data Fetching (Google Sheets)
    //================================================================================

    /// <summary>
    /// Public method called after successful login or auto-login to fetch the user's show list.
    /// </summary>
    public void InitializeUserShows()
    {
        // Ensure services needed for fetching are ready
        if (sheetsService == null || userStateSO == null || string.IsNullOrEmpty(userStateSO.AccountSheetID))
        {
             Debug.LogError("SessionManager: Cannot fetch shows. SheetsService not ready or AccountSheetID missing.");
             return;
        }
        StartCoroutine(FetchUserShows());
    }

    /// <summary>
    /// Coroutine to fetch basic user metadata and the number of shows from the main user sheet.
    /// Calls FetchShowList if shows exist.
    /// </summary>
    private IEnumerator FetchUserShows()
    {
        Debug.Log("📡SessionManager: Fetching user metadata and show count from Google Sheets...");
        string sheetId = userStateSO.AccountSheetID;
        bool isDone = false;
        string error = null;
        string jsonText = "";

        // Use sheetsService to get data
        sheetsService.FetchUserShowMetadata(sheetId,
            success => { jsonText = success; isDone = true; },
            err => { error = err; isDone = true; }
        );

        yield return new WaitUntil(() => isDone); // Wait for the async callback

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"❌SessionManager: Error fetching main user sheet metadata: {error}");
            // TODO: Handle error - maybe inform user?
            yield break;
        }

        // Parse the response safely
        JSONNode mainSheetResponse = null;
        try { mainSheetResponse = JSON.Parse(jsonText); } catch (Exception e) { Debug.LogError($"❌SessionManager: JSON Parse Error (User Metadata): {e.Message}"); yield break; }

        // Validate response structure (expecting 'values' array with at least 9 rows for numberOfShows at index 8)
        if (mainSheetResponse?["values"] == null || mainSheetResponse["values"].Count < 9)
        {
            Debug.LogError($"❌SessionManager: Invalid response format or missing data fetching user metadata. Response: {jsonText}");
            yield break;
        }

        // Safely extract data
        string creatorName = mainSheetResponse["values"][1]?[0]?.Value ?? "N/A";
        int numberOfShows = 0;
        int.TryParse(mainSheetResponse["values"][8]?[0]?.Value ?? "0", out numberOfShows);

        Debug.Log($"👤SessionManager: Account: {creatorName}");
        Debug.Log($"📜SessionManager: Number of Shows reported: {numberOfShows}");

        // Fetch the detailed show list if applicable
        if (numberOfShows > 0)
        {
            yield return StartCoroutine(FetchShowList()); // Fetch the list of shows
        }
        else
        {
            Debug.Log("ℹ️SessionManager: No shows found for this user.");
            // Still need to notify SceneController that initialization is done
             if (SceneController.instance != null) SceneController.instance.OnSessionInitialized(); else Debug.LogError("FetchUserShows: SceneController instance is null!");
        }
        // Note: OnSessionInitialized is called within FetchShowList if shows > 0
    }

    /// <summary>
    /// Coroutine to fetch the list of shows (ID, Title, SheetID, Links, etc.) from the 'Shows' tab of the user sheet.
    /// Populates the 'savedShows' list.
    /// </summary>
    private IEnumerator FetchShowList()
    {
        Debug.Log("📡SessionManager: Fetching detailed show list...");
        bool isDone = false;
        JSONNode result = null;
        string error = null;

        sheetsService.FetchShowList(userStateSO.AccountSheetID,
            success => { result = success; isDone = true; },
            err => { error = err; isDone = true; }
        );

        yield return new WaitUntil(() => isDone); // Wait for async callback

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"❌SessionManager: Error fetching show list: {error}");
            // TODO: Handle error
             if (SceneController.instance != null) SceneController.instance.OnSessionInitialized(); // Still signal init complete, but with error
            yield break;
        }

        // Parse response safely
        savedShows.Clear(); // Clear previous list before populating
        if (result?["values"] != null && result["values"].Count > 0)
        {
            foreach (JSONNode row in result["values"].AsArray)
            {
                // Expecting at least 5 columns (up to timingJSONLink)
                if (row.AsArray.Count >= 5)
                {
                    // Safely extract values, providing defaults if missing
                    ShowData show = new ShowData
                    {
                        showID = row[0]?.Value ?? "",
                        showTitle = row[1]?.Value ?? "Untitled Show",
                        group = row[2]?.Value ?? "Unknown Group",
                        showSheetID = row[3]?.Value ?? "",
                        lastModified = row[4]?.Value ?? "",
                        // marcherJSONLink = row[5]?.Value ?? "", // Should be backend URL/ID now, not direct link
                        // timingJSONLink = row[6]?.Value ?? ""  // Should be backend URL/ID now, not direct link
                    };

                    // Basic validation
                    if (!string.IsNullOrEmpty(show.showID) && !string.IsNullOrEmpty(show.showSheetID))
                    {
                         savedShows.Add(show);
                         // Debug.Log($"✅ Added show: {show.showTitle} | ID: {show.showID}");
                    } else {
                         Debug.LogWarning($"SessionManager: Skipping show entry due to missing showID or showSheetID: {row.ToString()}");
                    }
                } else {
                     Debug.LogWarning($"SessionManager: Skipping row in Show List due to insufficient columns: {row.ToString()}");
                }
            }
            Debug.Log($"🎭SessionManager: Total Valid Shows Fetched: {savedShows.Count}");
        } else {
             Debug.Log("ℹ️SessionManager: No show data found in the 'Shows' sheet or response format incorrect.");
        }

        // Notify SceneController that session initialization (including show list fetch) is complete
         if (SceneController.instance != null) SceneController.instance.OnSessionInitialized(); else Debug.LogError("FetchShowList: SceneController instance is null!");
    }

    #endregion

    //================================================================================
    #region Show Management
    //================================================================================

    /// <summary>
    /// Saves fetched show metadata into the ShowStateSO for the currently selected show.
    /// Called by ShowSelectionManager after fetching details from the specific show sheet.
    /// </summary>
    public void SaveToSessionManager(string id, string title, string group, string field, string year, int marchers, int sets, int props, string modified, string status, string setOnExit, string JSONMarching, string JSONTiming)
    {
        if (showStateSO == null) { Debug.LogError("SessionManager: ShowStateSO is null!"); return; }

        showStateSO.CurrentShowID = id;
        showStateSO.ShowTitle = title;
        showStateSO.GroupName = group;
        showStateSO.FieldType = field;
        showStateSO.ProductionYear = year;
        showStateSO.NumberOfMarchers = marchers;
        showStateSO.NumberOfSets = sets;
        showStateSO.NumberOfProps = props;
        showStateSO.LastModified = modified;
        showStateSO.ShowStatus = status;
        showStateSO.LastSet = setOnExit;
        // These URLs might become less relevant if JsonPersistenceService always uses backend endpoints based on ID
        showStateSO.JSONMarchersURL = JSONMarching;
        showStateSO.JSONSetTimingURL = JSONTiming;
         Debug.Log($"SessionManager: ShowStateSO updated for Show ID: {id}");

         // ✅ Initialize ShowDataManager after show metadata is loaded
        showDataManager = new ShowDataManager(
            coroutineHost: this,
            userStateSO: userStateSO,
            showStateSO: showStateSO,
            backendURL: backendURL
        );

    }

    /// <summary>
    /// Called by CreateShowManager after a new show is successfully created on the backend.
    /// Refreshes the show list and triggers the selection flow for the new show.
    /// </summary>
    /// <param name="showTitle">The title of the newly created show.</param>
    public void AddNewShow(string showTitle)
    {
        Debug.Log($"➕SessionManager: Adding new show '{showTitle}' to session flow.");
        StartCoroutine(ReinitializeAndSelectNewShow(showTitle));
    }

    /// <summary>
    /// Coroutine to refresh the show list from the backend and then attempt to find and select
    /// the newly created show in the ShowSelection scene.
    /// </summary>
    private IEnumerator ReinitializeAndSelectNewShow(string showTitle)
    {
        Debug.Log("SessionManager: Refreshing show list after new show creation...");
        yield return StartCoroutine(FetchUserShows()); // Re-fetch user data and show list

        // Attempt to find the new show by title (assuming titles are unique for the user)
        selectedShow = savedShows.Find(show => show.showTitle.Trim().Equals(showTitle.Trim(), StringComparison.OrdinalIgnoreCase));

        if (selectedShow == null)
        {
            Debug.LogError($"❌SessionManager: Could not find newly created show '{showTitle}' in refreshed savedShows list.");
            // TODO: Handle error - maybe inform the user?
            yield break;
        }

        Debug.Log($"✅SessionManager: Found newly created show: {selectedShow.showTitle} (ID: {selectedShow.showID}). Triggering selection.");

        // Attempt to trigger the selection logic in ShowSelectionManager
        // Note: FindObjectOfType is generally discouraged; consider event-based communication or direct reference if possible.
        ShowSelectionManager selectionManager = FindObjectOfType<ShowSelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.OnShowSelected(selectedShow);
        }
        else
        {
            // This might happen if the scene changed before this coroutine finished, which shouldn't normally occur here.
            Debug.LogWarning("⚠️SessionManager: ShowSelectionManager not found in the current scene. Cannot auto-select the new show.");
        }
    }

    #endregion

    //================================================================================
    #region Session Actions (Login, Logout, Save, Exit)
    //================================================================================

    public void AutoLogin()
    {
        // Ensure UserSessionManager is initialized
         if (userSession == null) { Debug.LogError("SessionManager: AutoLogin: userSession is null!"); return; }

        if (userSession.TryAutoLogin()) // TryAutoLogin now also calls InitializeUser
        {
            Debug.Log($"🔄SessionManager: Auto-Login successful. Initializing User Shows for: {userStateSO?.UserName ?? "N/A"}");
            InitializeUserShows(); // Fetch shows for the logged-in user
        }
        else
        {
            Debug.Log("SessionManager: No persistent login found or auto-login failed.");
            // If auto-login fails, the SceneController should already handle navigating to Login/Startup scene.
        }
    }

    public void SaveShow()
    {
         if (showDataManager == null) { Debug.LogError("SaveShow: showDataManager is null!"); return; }
         Debug.Log("💾SessionManager: Initiating save process...");
        showDataManager.SaveShow();
    }

    public void ExitShow()
    {
        Debug.Log("🚪SessionManager: Exiting Show... Saving changes first...");
        if (showDataManager == null || SceneController.instance == null)
        {
             Debug.LogError("SessionManager: ExitShow: showDataManager or SceneController is null! Cannot exit properly.");
             return;
        }
        // Tell ShowDataManager to save, and upon completion, switch to Scene 4 (Show Selection)
        showDataManager.ExitShow(() => SceneController.instance.SwitchScene(4));
    }

    public void StartLogout()
    {
         Debug.Log("🔒SessionManager: Initiating logout process... Saving changes first...");
         if (showDataManager == null) { Debug.LogError("SessionManager: StartLogout: showDataManager is null! Cannot save before logout."); Logout(); return; }
        showDataManager.LogOut(); // LogOut now handles the save AND the call to SessionManager.Logout
    }

    public void Logout()
    {
        Debug.Log("🔒SessionManager: Performing final logout operations...");
        if (userSession != null) userSession.Logout(); // Handles PlayerPrefs and clearing UserStateSO

        // Clear remaining session state
        savedShows.Clear();
        if (showStateSO != null) showStateSO.Clear();
        if (runtimeCacheSO != null) runtimeCacheSO.Clear();
        selectedShow = null;

        // Switch to the initial scene (e.g., Startup or Login)
        if (SceneController.instance != null)
        {
             SceneController.instance.SwitchScene(2); // Switch to Startup Scene (index 2)
        } else {
             Debug.LogError("SessionManager: Logout: SceneController instance is null! Cannot switch scene.");
        }
        Debug.Log("SessionManager: Logout complete.");
    }

    #endregion

    //================================================================================
    #region Data Structures
    //================================================================================
    /// <summary>
    /// Represents the basic metadata for a show listed in the user's main sheet.
    /// </summary>
    [System.Serializable]
    public class ShowData
    {
        public string showID;
        public string showTitle;
        public string showSheetID; // ID of the specific Google Sheet for this show
        public string lastModified;
        public string group;
        // These URLs might be less relevant now, consider if they should be backend identifiers instead
        public string marcherJSONLink;
        public string timingJSONLink;
    }
    #endregion
}
