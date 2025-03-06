using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SimpleJSON;
public class SessionManager : MonoBehaviour
{
    private string apiKey;
    public static string backendURL;
    public static SessionManager instance;

    public string userId;      // Unique User ID (e.g., "U123456789")
    public string userFolderId;
    public string userEmail;   // User's Email
    public string userName;    // User's Name
    public string accountSheetID;
    public List<ShowData> savedShows = new List<ShowData>(); // List of show Google Sheets

    public  string currentShowID;
    public  string showTitle;
    public  string groupName;
    public  string createdBy;
    public  string fieldType;
    public  string productionYear;
    public  int numberOfMarchers;
    public  int numberOfSets;
    public  int numberOfProps;
    public  string lastModified;
    public  string showStatus;

    public  List<string> setsData = new List<string>();
    public  List<string> marchersData = new List<string>();

    public  Dictionary<string, Vector3> marchersCoordinates = new Dictionary<string, Vector3>();


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // Keeps SessionManager alive across scenes
            savedShows.Clear();

            // Load API key from config.json
            LoadApiKey();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called when the user logs in
    public void InitializeUser(string id, string email, string name, string sheetID, string folderId)
    {
        userId = id;
        userFolderId = folderId;
        userEmail = email;
        userName = name;
        accountSheetID = sheetID;

        Debug.Log($"✅ User Session Initialized In: {userName} ({userEmail}) | ID: {userId}| SheetID: {accountSheetID}" );

        // Fetch the user's saved shows from Google Drive
        StartCoroutine(FetchUserShows());
    }

    // Simulate checking for saved shows in Google Drive (to be replaced with real API call)
    private IEnumerator FetchUserShows()
    {
        Debug.Log("📡 Fetching user data from Google Sheets...");

        // Single API call to fetch all relevant data
        string url = $"https://sheets.googleapis.com/v4/spreadsheets/{accountSheetID}/values/Main!B2:B10?key={apiKey}";
        Debug.Log($"🔗 API URL: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            Debug.Log("📡 API request sent.");

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ Error fetching all data: {request.error}");
                yield break;
            }
            else if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ API request successful.");
            }

            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"📥 JSON Response: {jsonResponse}");
            var mainSheetResponse = JSON.Parse(jsonResponse);

            Debug.Log(mainSheetResponse.ToString());

            if (mainSheetResponse == null)
            {
                Debug.LogError("❌ Deserialization failed. mainSheetResponse is null.");
                yield break;
            }

            if (mainSheetResponse["values"] != null)
            {
                Debug.Log("✅ mainSheetResponse deserialization successful.");

                string creatorName = mainSheetResponse["values"][1][0];
                int numberOfShows = int.Parse(mainSheetResponse["values"][8][0]);

                Debug.Log($"👤 Account Name: {creatorName}");
                Debug.Log($"📜 Number of Shows: {numberOfShows}");

                if (numberOfShows > 0)
                {
                    // Fetch show data from "Shows" sheet
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
                        Debug.Log($"📥 Raw 'Shows' API Response: {showsJsonResponse}"); // ✅ Debug full response
                        var showsResponse = JSON.Parse(showsJsonResponse);

                        if (showsResponse["values"] != null && showsResponse["values"].Count > 0)
                        {
                            savedShows.Clear();
                            foreach (JSONNode  row in showsResponse["values"].AsArray)
                            {
                                Debug.Log($"🔍 Checking row: {row.ToString()}"); // ✅ Log each row

                            if (row.AsArray.Count >= 5) // Ensure all required fields are present
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
                                    Debug.Log($"✅ Added show: {show.showID} | Title: {show.showTitle}");
                                }
                            }

                            Debug.Log($"🎭 Fetched {savedShows.Count} shows.");
                        }
                    }
                }
                else
                {
                    Debug.Log("ℹ️ No shows found for this user.");
                }
            }
        }
    }

    private void LoadApiKey()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");
        if (File.Exists(configPath))
        {
            string configContent = File.ReadAllText(configPath);
            var configJson = JSON.Parse(configContent);
            apiKey = configJson["googleApiKey"];
            backendURL = configJson["backendURL"]; // ✅ Load backend URL
            Debug.Log("✅ API key loaded successfully.");
        }
        else
        {
            Debug.LogError("❌ config.json not found or API key missing.");
        }
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
