using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using SimpleJSON;
public class SessionManager : MonoBehaviour
{
    private string apiKey;
    public static SessionManager instance;

    public string userId;      // Unique User ID (e.g., "U123456789")
    public string userEmail;   // User's Email
    public string userName;    // User's Name
    public string accountSheetID;
    public List<ShowData> savedShows = new List<ShowData>(); // List of show Google Sheets

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
    public void InitializeUser(string id, string email, string name, string sheetID)
    {
        userId = id;
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
                    string showsSheetUrl = $"https://sheets.googleapis.com/v4/spreadsheets/{accountSheetID}/values/Shows!A2:D?key={apiKey}";

                    using (UnityWebRequest showsRequest = UnityWebRequest.Get(showsSheetUrl))
                    {
                        yield return showsRequest.SendWebRequest();

                        if (showsRequest.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"❌ Error fetching show data: {showsRequest.error}");
                            yield break;
                        }

                        string showsJsonResponse = showsRequest.downloadHandler.text;
                        var showsResponse = JSON.Parse(showsJsonResponse);

                        if (showsResponse["values"] != null)
                        {
                            savedShows.Clear();
                            foreach (JSONNode  row in showsResponse["values"].AsArray)
                            {
                            if (row.AsArray.Count >= 4) // Ensure all required fields are present
                                {
                                    ShowData show = new ShowData
                                    {
                                        showID = row[0],
                                        showTitle = row[1],
                                        showSheetID = row[2],
                                        lastModified = row[3],
                                        creator = creatorName
                                    };

                                    savedShows.Add(show);
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
        public string creator;  // New attribute: Account Name (Creator)
    }
}
