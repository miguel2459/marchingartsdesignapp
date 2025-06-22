using UnityEngine;
using System.Collections;

public class SessionStateValidator : MonoBehaviour
{
    public static bool WasSessionValid { get; private set; }
    public static bool HasValidationRun { get; private set; }
    
    public IEnumerator StartValidation()
    {
        Debug.Log("🧪 SessionStateValidator: StartValidation coroutine called.");
#if UNITY_EDITOR
        BuildInfoManager.BuildVersion = "dev-editor";
#endif

        // Wait until BuildVersion is initialized
        while (BuildInfoManager.BuildVersion == "unknown")
        {
            Debug.Log("⏳ SessionStateValidator: Waiting for BuildInfoManager.BuildVersion...");
            yield return null;
        }

        string runtimeVersion = BuildInfoManager.BuildVersion;
        string cachedVersion = PlayerPrefs.GetString("AppVersion");

        Debug.Log($"📦 Build version check: Cached = {cachedVersion}, Runtime = {runtimeVersion}");

        if (cachedVersion != runtimeVersion)
        {
            Debug.Log($"🚨 SessionStateValidator: Cache invalidated. New build version: {runtimeVersion}");
            PlayerPrefs.SetString("AppVersion", runtimeVersion);
            PlayerPrefs.Save();
            Debug.Log($"💾 SessionStateValidator: AppVersion stored to PlayerPrefs: {runtimeVersion}");
        }
        else
        {
            Debug.Log("✅ SessionStateValidator: Version is current. No cache clearing needed.");
        }

        // Perform session validation
        WasSessionValid = IsSessionValid();
        HasValidationRun = true;

        if (!WasSessionValid)
        {
            Debug.LogWarning("⚠️ SessionStateValidator: Session validation failed.");
            ForceLogoutAndRedirect();
        }
        else
        {
            Debug.Log("✅ Session is valid.");
        }
    }


    private bool IsSessionValid()
    {
        string sheetID = PlayerPrefs.GetString("AccountSheetID");
        string userFolder = PlayerPrefs.GetString("FolderID");

        Debug.Log($"🔍 SessionStateValidator: Checking PlayerPrefs: AccountSheetID = '{sheetID}', UserFolderID = '{userFolder}'");

        return !string.IsNullOrEmpty(sheetID) && !string.IsNullOrEmpty(userFolder);
    }

    private void ForceLogoutAndRedirect()
    {
        Debug.Log("🧹 SessionStateValidator: Clearing session-related PlayerPrefs...");
        PlayerPrefs.DeleteKey("AccountSheetID");
        PlayerPrefs.DeleteKey("UserInfoRef");

        // Optional: clear session cache object if you have one
        //SessionState.Clear();

        Debug.Log("🔁 SessionStateValidator: Restarting to login scene...");
        //SceneController.Instance.SwitchScene("LoginScene");
    }
}