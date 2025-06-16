using UnityEngine;
using System.Collections;

public class SessionStateValidator : MonoBehaviour
{
    private IEnumerator Start()
    {
        Debug.Log("🧪 SessionStateValidator.Start coroutine called.");

        // Wait until BuildVersion is initialized
        while (BuildInfoManager.BuildVersion == "unknown")
        {
            Debug.Log("⏳ Waiting for BuildInfoManager.BuildVersion...");
            yield return null;
        }

        string runtimeVersion = BuildInfoManager.BuildVersion;
        string cachedVersion = PlayerPrefs.GetString("AppVersion");

        Debug.Log($"📦 Build version check: Cached = {cachedVersion}, Runtime = {runtimeVersion}");

        if (cachedVersion != runtimeVersion)
        {
            PlayerPrefs.DeleteAll();
            Debug.Log($"🚨 Cache invalidated. New build version: {runtimeVersion}");
            PlayerPrefs.SetString("AppVersion", runtimeVersion);
        }
        else
        {
            Debug.Log("✅ Version is current. No cache clearing needed.");
        }

        if (!IsSessionValid())
        {
            Debug.LogWarning("⚠️ Session validation failed.");
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
        string userRef = PlayerPrefs.GetString("UserInfoRef");

        Debug.Log($"🔍 Checking PlayerPrefs: AccountSheetID = '{sheetID}', UserInfoRef = '{userRef}'");

        return !string.IsNullOrEmpty(sheetID) && !string.IsNullOrEmpty(userRef);
    }

    private void ForceLogoutAndRedirect()
    {
        Debug.Log("🧹 Clearing session-related PlayerPrefs...");
        PlayerPrefs.DeleteKey("AccountSheetID");
        PlayerPrefs.DeleteKey("UserInfoRef");

        // Optional: clear session cache object if you have one
        //SessionState.Clear();

        Debug.Log("🔁 Restarting to login scene...");
        //SceneController.Instance.SwitchScene("LoginScene");
    }
}