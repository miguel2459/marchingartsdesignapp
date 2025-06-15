using UnityEngine;

public class SessionStateValidator : MonoBehaviour
{
    [Header("Expected App Version")]
    public string currentVersion = "2025.06.14";

    void Awake()
    {
        string runtimeVersion = BuildInfo.BuildVersion;
        if (PlayerPrefs.GetString("AppVersion") != runtimeVersion)
        {
            PlayerPrefs.DeleteAll();
            Debug.Log($"🚨 Cache invalidated. New build version: {runtimeVersion}");
            PlayerPrefs.SetString("AppVersion", runtimeVersion);
        }

        if (!IsSessionValid())
        {
            Debug.LogError("❌ Invalid session. Redirecting to login.");
            ForceLogoutAndRedirect();
        }
    }


    private bool IsSessionValid()
    {
        return !string.IsNullOrEmpty(PlayerPrefs.GetString("AccountSheetID"))
               && !string.IsNullOrEmpty(PlayerPrefs.GetString("UserInfoRef"));
    }

    private void ForceLogoutAndRedirect()
    {
        PlayerPrefs.DeleteKey("AccountSheetID");
        PlayerPrefs.DeleteKey("UserInfoRef");

        // Optional: clear session cache object if you have one
        // SessionState.Clear();

        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }
}

public static class BuildInfo
{
    public static string BuildVersion = "unknown";

    public static void SetBuildVersion(string version)
    {
        BuildVersion = version;
        Debug.Log($"🛠️ Build version set from JS: {version}");
    }
}
