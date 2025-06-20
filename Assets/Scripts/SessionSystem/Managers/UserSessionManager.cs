using UnityEngine;
using System.IO;

public class UserSessionManager
{
    private UserStateSO userState;

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

    public void SaveSession(UserData data)
    {
        userState.UserId = data.UserId;
        userState.UserEmail = data.UserEmail;
        userState.UserName = data.UserName;
        userState.AccountSheetID = data.AccountSheetID;
        userState.UserFolderId = data.UserFolderId;

        PlayerPrefs.SetInt(KeyIsLoggedIn, 1);
        PlayerPrefs.SetString(KeyUserId, data.UserId);
        PlayerPrefs.SetString(KeyEmail, data.UserEmail);
        PlayerPrefs.SetString(KeyUserName, data.UserName);
        PlayerPrefs.SetString(KeySheetId, data.AccountSheetID);
        PlayerPrefs.SetString(KeyFolderId, data.UserFolderId);
        PlayerPrefs.Save();

        Debug.Log($"✅ UserSessionManager: Session saved for {data.UserName} ({data.UserEmail})");

        SessionManager.instance.InitializeJsonService();
        SessionManager.instance.InitializeUserShows();
    }

    public UserData? LoadSession()
    {
        if (PlayerPrefs.GetInt(KeyIsLoggedIn, 0) != 1) return null;

        string id = PlayerPrefs.GetString(KeyUserId, "");
        string email = PlayerPrefs.GetString(KeyEmail, "");
        string name = PlayerPrefs.GetString(KeyUserName, "");
        string sheetID = PlayerPrefs.GetString(KeySheetId, "");
        string folderId = PlayerPrefs.GetString(KeyFolderId, "");

        if (string.IsNullOrEmpty(id)) return null;

        return new UserData(id, email, name, sheetID, folderId);
    }

    public bool TryAutoLogin()
    {
        var session = LoadSession();
        if (session.HasValue)
        {
            SaveSession(session.Value);
            return true;
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

        userState.Clear();

        string jsonCacheDir = Path.Combine(Application.persistentDataPath, "MADA_JSONS");
        if (Directory.Exists(jsonCacheDir))
        {
            Debug.Log($"🧹 UserSessionManager: Deleting cached JSON files in: {jsonCacheDir}");
            foreach (string file in Directory.GetFiles(jsonCacheDir, "*_*.json"))
            {
                try { File.Delete(file); Debug.Log($"🗑️ UserSessionManager: Deleted cached JSON: {file}"); }
                catch (System.Exception ex) { Debug.LogWarning($"⚠️ UserSessionManager: Failed to delete {file}: {ex.Message}"); }
            }
        }

        SceneController.instance.isSessionInitialized = false;
        Debug.Log("✅ UserSessionManager: Logout complete.");
    }
}

public struct UserData
{
    public string UserId;
    public string UserEmail;
    public string UserName;
    public string AccountSheetID;
    public string UserFolderId;

    public UserData(string userId, string userEmail, string userName, string accountSheetID, string userFolderId)
    {
        UserId = userId;
        UserEmail = userEmail;
        UserName = userName;
        AccountSheetID = accountSheetID;
        UserFolderId = userFolderId;
    }
}
