using UnityEngine;

/// <summary>
/// Handles user authentication, auto-login, and logout.
/// Owns a reference to SessionState.
/// </summary>
public class UserSessionManager
{
    public SessionState SessionState { get; private set; } = new SessionState();

    private const string KeyIsLoggedIn = "IsLoggedIn";
    private const string KeyUserId = "UserID";
    private const string KeyEmail = "UserEmail";
    private const string KeyUserName = "UserName";
    private const string KeySheetId = "AccountSheetID";
    private const string KeyFolderId = "FolderID";

    public void InitializeUser(string id, string email, string name, string sheetID, string folderId)
    {
        SessionState.UserId = id;
        SessionState.UserEmail = email;
        SessionState.UserName = name;
        SessionState.AccountSheetID = sheetID;
        SessionState.UserFolderId = folderId;

        PlayerPrefs.SetInt(KeyIsLoggedIn, 1);
        PlayerPrefs.SetString(KeyUserId, id);
        PlayerPrefs.SetString(KeyEmail, email);
        PlayerPrefs.SetString(KeyUserName, name);
        PlayerPrefs.SetString(KeySheetId, sheetID);
        PlayerPrefs.SetString(KeyFolderId, folderId);
        PlayerPrefs.Save();

        SessionManager.instance.InitializeUser();

        Debug.Log($"✅ UserSessionManager: Logged in as {name} ({email})");
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
                InitializeUser(id, email, name, sheetID, folderId);
                Debug.Log("🔄 UserSessionManager: Auto-login successful.");
                return true;
            }
        }

        Debug.Log("⚠️ UserSessionManager: No valid session for auto-login.");
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

        SessionState = new SessionState(); // Wipe the current session
        SceneController.instance.isSessionInitialized = false;
        Debug.Log("✅ UserSessionManager: Logout complete.");
    }
}
