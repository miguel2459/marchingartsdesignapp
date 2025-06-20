using UnityEngine;

public class BuildInfoManager : MonoBehaviour
{
    public static string BuildVersion = "unknown";

    // This will be called via JS SendMessage("BuildInfoManager", "SetBuildVersion", version)
    public void SetBuildVersion(string version)
    {
        BuildVersion = version;
        Debug.Log($"🛠️ BuildInfoManager: Build version set from JS: {version}");
    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}