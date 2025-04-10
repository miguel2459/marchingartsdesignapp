using UnityEngine;
using System.IO;

/// <summary>
/// Handles user authentication, auto-login, and logout.
/// Owns a reference to SessionState.
/// </summary>
public class UserSessionManager
{
    // 🔒 Private field to hold the reference to the injected ScriptableObject session data.
    // This allows the class to store the runtime session state internally.
    private UserStateSO userState;

    // 🔓 Public read-only property to expose the session state externally.
    // Other classes can read from this, but only this class can modify the reference.
    // This follows the principle of encapsulation and protects against unintended changes.
    public UserStateSO UserState => userState;
    private const string KeyIsLoggedIn = "IsLoggedIn";
    private const string KeyUserId = "UserID";
    private const string KeyEmail = "UserEmail";
    private const string KeyUserName = "UserName";
    private const string KeySheetId = "AccountSheetID";
    private const string KeyFolderId = "FolderID";

    public void InjectUserState(UserStateSO so)
    {
        userState = so;
    }
    
    public void InitializeUser(string id, string email, string name, string sheetID, string folderId)
    {
        UserState.UserId = id;
        UserState.UserEmail = email;
        UserState.UserName = name;
        UserState.AccountSheetID = sheetID;
        UserState.UserFolderId = folderId;

        PlayerPrefs.SetInt(KeyIsLoggedIn, 1);
        PlayerPrefs.SetString(KeyUserId, id);
        PlayerPrefs.SetString(KeyEmail, email);
        PlayerPrefs.SetString(KeyUserName, name);
        PlayerPrefs.SetString(KeySheetId, sheetID);
        PlayerPrefs.SetString(KeyFolderId, folderId);
        PlayerPrefs.Save();

        Debug.Log($"✅ UserSessionManager: User Initialized, Logged in as {name} ({email})");

        SessionManager.instance.InitializeJsonService();
        SessionManager.instance.InitializeUserShows(); 
    }

    public bool TryAutoLogin()
    {
        if (PlayerPrefs.GetInt(KeyIsLoggedIn, 0) == 1)
        {
            string id = PlayerPrefs.GetString(KeyUserId, "");
            string email = PlayerPrefs.GetString(KeyEmail, "");
            string name = PlayerPrefs.GetString(KeyUserName, "");
            string sheetID = PlayerPrefs.GetString(KeySheetId, "");
            string folderId = PlayerPrefs.GetString(KeyFolderId, "");

            if (!string.IsNullOrEmpty(id))
            {
                Debug.Log("🔄 UserSessionManager: TryAutoLogin: Persistent Login Found!");
                InitializeUser(id, email, name, sheetID, folderId);
                return true;
            }
        }

        Debug.Log("⚠️ UserSessionManager: TryAutoLogin: No valid session for auto-login.");
        return false;
    }

    public void Logout()
    {
        Debug.Log("🔒 UserSessionManager: Logging out...");

        PlayerPrefs.SetInt(KeyIsLoggedIn, 0);
        PlayerPrefs.DeleteKey(KeyUserId);
        PlayerPrefs.DeleteKey(KeyEmail);
        PlayerPrefs.DeleteKey(KeyUserName);
        PlayerPrefs.DeleteKey(KeySheetId);
        PlayerPrefs.DeleteKey(KeyFolderId);
        PlayerPrefs.Save();

        // Wipe the current session by clearing all fields in the SO
        UserState.Clear();

        // 🧹 Delete local JSON cache
        string jsonCacheDir = Path.Combine(Application.persistentDataPath, "MADA_JSONS");

        if (Directory.Exists(jsonCacheDir))
        {
            Debug.Log($"🧹 Deleting cached JSON files in: {jsonCacheDir}");
            string[] jsonFiles = Directory.GetFiles(jsonCacheDir, "*_*.json");

            foreach (string file in jsonFiles)
            {
                try
                {
                    File.Delete(file);
                    Debug.Log($"🗑️ Deleted cached JSON: {file}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"⚠️ Failed to delete cached file {file}: {ex.Message}");
                }
            }
        }

        SceneController.instance.isSessionInitialized = false;
        Debug.Log("✅ UserSessionManager: Logout complete.");
    }

}
