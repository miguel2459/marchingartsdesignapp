using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SessionManager : MonoBehaviour
{
    public static SessionManager instance;

    public string userId;      // Unique User ID (e.g., "U123456789")
    public string userEmail;   // User's Email
    public string userName;    // User's Name
    public string accountSheetID;
    public List<string> savedShows = new List<string>(); // List of show Google Sheets

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // Keeps SessionManager alive across scenes
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

        Debug.Log($"✅ User Logged In: {userName} ({userEmail}) | ID: {userId}| SheetID: {accountSheetID}" );

        // Fetch the user's saved shows from Google Drive
        StartCoroutine(FetchUserShows());
    }

    // Simulate checking for saved shows in Google Drive (to be replaced with real API call)
    private IEnumerator FetchUserShows()
    {
        Debug.Log("📡 Fetching saved shows from Google Drive...");
        
        // Simulated delay (replace with real API call later)
        yield return new WaitForSeconds(2);

        // Here we should request the Google Drive API to list the sheets in the user's folder
        // For now, we'll assume the user has some shows saved
        //savedShows.Add("Comedy Show");
        //savedShows.Add("Horror Night");

        Debug.Log($"🎭 User has {savedShows.Count} saved shows.");
    }

    // Check if the user has saved shows
    public bool HasSavedShows()
    {
        return savedShows.Count > 0;
    }

    // Called when the user creates a new show
    public void AddNewShow(string showName)
    {
        savedShows.Add(showName);
        Debug.Log($"➕ New show added: {showName}");
    }
}
