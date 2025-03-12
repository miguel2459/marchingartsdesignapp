using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SimpleJSON;

public class SessionManager : MonoBehaviour
{
    public string apiKey;
    public static string backendURL;
    public static SessionManager instance;

    [Header("User Information")]
    public string userId;
    public string userFolderId;
    public string userEmail;
    public string userName;
    public string accountSheetID;

    [Header("Session Data")]
    public List<ShowData> savedShows = new List<ShowData>();
    public ShowData selectedShow;
    public string currentShowID;
    public string showTitle;
    public string groupName;
    public string createdBy;
    public string fieldType;
    public string productionYear;
    public int numberOfMarchers;
    public int numberOfSets;
    public int numberOfProps;
    public string lastModified;
    public string showStatus;
    public List<string> setsData = new List<string>();
    public List<string> marchersData = new List<string>();

    public Dictionary<string, Vector3> marchersCoordinates = new Dictionary<string, Vector3>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            savedShows.Clear();

            LoadApiKey(); // Load API key from config.json
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🔹 Initialize user session when they log in
    public void InitializeUser(string id, string email, string name, string sheetID, string folderId)
    {
        userId = id;
        userFolderId = folderId;
        userEmail = email;
        userName = name;
        accountSheetID = sheetID;

        Debug.Log($"✅ User Session Initialized: {userName} ({userEmail}) | ID: {userId} | SheetID: {accountSheetID}");

        StartCoroutine(FetchUserShows()); // Fetch the user's saved shows
    }

    // 🔹 Fetch user shows from Google Sheets
    private IEnumerator FetchUserShows()
    {
        Debug.Log("📡 Fetching user data from Google Sheets...");

        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{accountSheetID}/values/Main!B2:B10?key={apiKey}";
        Debug.Log($"🔗 API URL: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error fetching all data: {request.error}");
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"📥 JSON Response: {jsonResponse}");

            var mainSheetResponse = JSON.Parse(jsonResponse);
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
            }
            else
            {
                Debug.Log("ℹ️ No shows found for this user.");
            }
        }
    }

    // 🔹 Fetch list of shows from the "Shows" sheet
    private IEnumerator FetchShowList()
    {
        string showsSheetUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{accountSheetID}/values/Shows!A2:E?key={apiKey}";

        using (UnityWebRequest showsRequest = UnityWebRequest.Get(showsSheetUrl))
        {
            yield return showsRequest.SendWebRequest();

            if (showsRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error fetching show data: {showsRequest.error}");
                yield break;
            }

            string showsJsonResponse = showsRequest.downloadHandler.text;
            Debug.Log($"📥 'Shows' API Response: {showsJsonResponse}");

            var showsResponse = JSON.Parse(showsJsonResponse);

            if (showsResponse["values"] != null && showsResponse["values"].Count > 0)
            {
                savedShows.Clear();
                foreach (JSONNode row in showsResponse["values"].AsArray)
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
    }

    // 🔹 Save selected show details into session
    public void SaveToSessionManager(string id, string title, string group, string field, string year, int marchers, int sets, int props, string modified, string status)
    {
        currentShowID = id;
        showTitle = title;
        groupName = group;
        fieldType = field;
        productionYear = year;
        numberOfMarchers = marchers;
        numberOfSets = sets;
        numberOfProps = props;
        lastModified = modified;
        showStatus = status;
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
    Debug.Log("🚪 Exiting Show... Resetting show-specific data.");

    // 🔹 Reset only show-related data, keeping user session intact
    selectedShow = null;
    currentShowID = string.Empty;
    showTitle = string.Empty;
    groupName = string.Empty;
    createdBy = string.Empty;
    fieldType = string.Empty;
    productionYear = string.Empty;
    numberOfMarchers = 0;
    numberOfSets = 0;
    numberOfProps = 0;
    lastModified = string.Empty;
    showStatus = string.Empty;

    Debug.Log("✅ Show session cleared. Returning to Show Selection.");

    // 🔹 Return to Show Selection Scene
    SceneController.instance.SwitchScene(3);
}


    public void AutoLogin()
    {
        if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1) // 🔹 Check if user is already logged in
        {
            string userId = PlayerPrefs.GetString("UserID", "");
            string email = PlayerPrefs.GetString("UserEmail", "");
            string userName = PlayerPrefs.GetString("UserName", "");
            string userSheetID = PlayerPrefs.GetString("AccountSheetID", "");
            string folderId = PlayerPrefs.GetString("FolderID", "");

            if (!string.IsNullOrEmpty(userId))
            {
                Debug.Log($"🔄 Auto-Logging In: {userName} ({email})");

                // 🔹 Initialize session with stored credentials
                InitializeUser(userId, email, userName, userSheetID, folderId);
            }
        }
    }

    public void Logout()
    {
        Debug.Log("🔒 Logging out...");

        // 🔹 Clear stored credentials from PlayerPrefs
        PlayerPrefs.SetInt("IsLoggedIn", 0);
        PlayerPrefs.DeleteKey("UserID");
        PlayerPrefs.DeleteKey("UserEmail");
        PlayerPrefs.DeleteKey("FolderID");
        PlayerPrefs.DeleteKey("UserName");
        PlayerPrefs.DeleteKey("AccountSheetID");
        PlayerPrefs.DeleteKey("LastShowID"); // Clear last selected show
        PlayerPrefs.Save();

        // 🔹 Reset SessionManager user data
        userId = string.Empty;
        userFolderId = string.Empty;
        userEmail = string.Empty;
        userName = string.Empty;
        accountSheetID = string.Empty;

        // 🔹 Reset Show & Session Data
        selectedShow = null;
        savedShows.Clear();
        currentShowID = string.Empty;
        showTitle = string.Empty;
        groupName = string.Empty;
        createdBy = string.Empty;
        fieldType = string.Empty;
        productionYear = string.Empty;
        numberOfMarchers = 0;
        numberOfSets = 0;
        numberOfProps = 0;
        lastModified = string.Empty;
        showStatus = string.Empty;
        setsData.Clear();
        marchersData.Clear();
        marchersCoordinates.Clear();

        Debug.Log("✅ User session fully reset.");

        // 🔹 Return to login screen
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
