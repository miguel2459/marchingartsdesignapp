using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SimpleJSON;

public class SessionManager : MonoBehaviour
{
    public UserSessionManager userSession = new UserSessionManager();
    public ShowDataManager showDataManager;
    public GoogleSheetsService sheetsService;
    public string apiKey;
    public static string backendURL;
    public static SessionManager instance;

    [Header("🧪 DEBUG ONLY")]
    [SerializeField] private SessionState debugSessionState;
    public SessionState SessionState => userSession.SessionState;

    [Header("Session Data")]
    public List<ShowData> savedShows = new List<ShowData>();
    public ShowData selectedShow;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            savedShows.Clear();

            LoadApiKey(); // Load API key from config.json
            sheetsService = new GoogleSheetsService(apiKey, this);
            showDataManager = new ShowDataManager(this, SessionState, backendURL);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Sync debugSessionState every frame (or do this in a coroutine if you want to reduce overhead)
        debugSessionState = userSession.SessionState;
    }

    // 🔹 Initialize user session when they log in
    public void InitializeUser()
    {    
        StartCoroutine(FetchUserShows());
    }


    // 🔹 Fetch user shows from Google Sheets
    private IEnumerator FetchUserShows()
    {
        Debug.Log("📡 Fetching user data from Google Sheets...");

        string sheetId = SessionState.AccountSheetID;

        bool isDone = false;
        string error = null;
        string jsonText = "";

        sheetsService.FetchUserShowMetadata(sheetId,
            success => {
                jsonText = success;
                isDone = true;
            },
            err => {
                error = err;
                isDone = true;
            });

        // Wait for async callback
        yield return new WaitUntil(() => isDone);

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"❌ Error fetching main sheet: {error}");
            yield break;
        }

        var mainSheetResponse = JSON.Parse(jsonText);
        if (mainSheetResponse == null || mainSheetResponse["values"] == null)
        {
            Debug.LogError("❌ Deserialization failed. No response received.");
            yield break;
        }

        string creatorName = mainSheetResponse["values"][1][0];
        int numberOfShows = int.Parse(mainSheetResponse["values"][8][0]);

        Debug.Log($"👤 Account Name: {creatorName}");
        Debug.Log($"📜 Number of Shows: {numberOfShows}");

        if (numberOfShows > 0)
        {
            yield return StartCoroutine(FetchShowList());
            SceneController.instance.OnSessionInitialized();
        }
        else
        {
            Debug.Log("ℹ️ No shows found for this user.");
            SceneController.instance.OnSessionInitialized();
            SceneController.instance.SwitchScene(3);
        }
    }

    // 🔹 Fetch list of shows from the "Shows" sheet
    private IEnumerator FetchShowList()
    {
        bool isDone = false;
        JSONNode result = null;
        string error = null;

        sheetsService.FetchShowList(SessionState.AccountSheetID,
            success => {
                result = success;
                isDone = true;
            },
            err => {
                error = err;
                isDone = true;
            });

        yield return new WaitUntil(() => isDone);

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"❌ Error fetching show list: {error}");
            yield break;
        }

        if (result["values"] != null && result["values"].Count > 0)
        {
            savedShows.Clear();
            foreach (JSONNode row in result["values"].AsArray)
            {
                if (row.AsArray.Count >= 5)
                {
                    ShowData show = new ShowData
                    {
                        showID = row[0],
                        showTitle = row[1],
                        group = row[2],
                        showSheetID = row[3],
                        lastModified = row[4],
                    };
                    savedShows.Add(show);
                    Debug.Log($"✅ Added show: {show.showTitle} | ID: {show.showID}");
                }
            }

            Debug.Log($"🎭 Total Shows Fetched: {savedShows.Count}");
        }
    }


    // 🔹 Save selected show details into session
    public void SaveToSessionManager(string id, string title, string group, string field, string year, int marchers, int sets, int props, string modified, string status, string setOnExit)
    {
        SessionState.CurrentShowID = id;
        SessionState.ShowTitle = title;
        SessionState.GroupName = group;
        SessionState.FieldType = field;
        SessionState.ProductionYear = year;
        SessionState.NumberOfMarchers = marchers;
        SessionState.NumberOfSets = sets;
        SessionState.NumberOfProps = props;
        SessionState.LastModified = modified;
        SessionState.ShowStatus = status;
        SessionState.LastSet = setOnExit;
    }

    public void AddNewShow(string showTitle)
    {
        Debug.Log($"➕ Adding new show '{showTitle}' to session.");

        StartCoroutine(ReinitializeAndSelectNewShow(showTitle));
    }

    private IEnumerator ReinitializeAndSelectNewShow(string showTitle)
    {
        // Step 1: Refresh the user's shows
        yield return StartCoroutine(FetchUserShows());

        // Step 2: Try to find the newly created show
        selectedShow = savedShows.Find(show => show.showTitle.Trim() == showTitle.Trim());

        if (selectedShow == null)
        {
            Debug.LogError($"❌ Could not find newly created show '{showTitle}' in savedShows.");
            yield break;
        }

        Debug.Log($"✅ Found newly created show: {selectedShow.showTitle} (ID: {selectedShow.showID})");

        // Step 3: Trigger show details loading via ShowSelectionManager
        ShowSelectionManager selectionManager = GameObject.FindObjectOfType<ShowSelectionManager>();
        if (selectionManager != null)
        {
            Debug.Log("📥 Calling ShowSelectionManager.OnShowSelected() with new show.");
            selectionManager.OnShowSelected(selectedShow);
        }
        else
        {
            Debug.LogWarning("⚠️ ShowSelectionManager not found in scene. Can't auto-select the new show.");
        }
    }


    // 🔹 Load API Key from config file
    private void LoadApiKey()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (File.Exists(configPath))
        {
            string configContent = File.ReadAllText(configPath);
            var configJson = JSON.Parse(configContent);
            apiKey = configJson["googleApiKey"];
            backendURL = configJson["backendURL"];

            Debug.Log("✅ API key loaded successfully.");
        }
        else
        {
            Debug.LogError("❌ config.json not found or API key missing.");
        }
    }

    public void ExitShow()
    {
        Debug.Log("🚪 Exiting Show... Saving changes before exiting...");
        showDataManager.ExitShow(() => SceneController.instance.SwitchScene(3));
    }

    public void SaveShow()
    {
        showDataManager.SaveShow();
    }

    public void AutoLogin()
    {
        if (userSession.TryAutoLogin())
        {
            Debug.Log($"🔄 Auto-Login: {SessionState.UserName} ({SessionState.UserEmail})");
            StartCoroutine(FetchUserShows());
        }
    }

    public void Logout()
    {
        Debug.Log("🔒 Logging out...");
        userSession.Logout();
        savedShows.Clear();
        SceneController.instance.SwitchScene(1);
    }

    [System.Serializable]
    public class ShowData
    {
        public string showID;
        public string showTitle;
        public string showSheetID;
        public string lastModified;
        public string group;
    }
}
