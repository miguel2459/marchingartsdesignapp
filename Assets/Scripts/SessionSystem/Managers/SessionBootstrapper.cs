using System.Collections;
using UnityEngine;
using System;

public class SessionBootstrapper : MonoBehaviour
{
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private JsonCoordinatorService jsonService;      

    /// <summary>Fired when both JSON blobs are parsed and cached.</summary>
    public event Action OnSessionReady = delegate { };

    public void SessionBootstrapperInit()
    {
        if (sessionManager == null)  sessionManager = SessionManager.instance;
        if (jsonService == null)     jsonService    = sessionManager.JsonService;
    }

    public IEnumerator BootStrapperStart()
    {
        Debug.Log("🚀 SessionBootstrapper starting...");

        string showId = sessionManager.showStateSO.CurrentShowID;
        if (string.IsNullOrEmpty(showId))
        {
            Debug.LogError("❌ SessionBootstrapper: Show ID is missing in ShowStateSO.");
            yield break;
        }

        // ✅ Early exit if already parsed (Scene 3 handled it)
        if (sessionManager.runtimeCacheSO.ParsedCountPositions != null &&
            sessionManager.runtimeCacheSO.SetTimingMap != null)
        {
            Debug.Log("⏩ Parsed session data already exists. Skipping JSON load.");
            OnSessionReady.Invoke();
            yield break;
        }

        Debug.LogWarning("⚠️ JSON not yet parsed. You likely bypassed Scene 3. Aborting load.");
    }
}
