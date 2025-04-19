using System.Collections;
using UnityEngine;
using System;

public class SessionBootstrapper : MonoBehaviour
{
    SessionManager sessionManager;            
    JsonCoordinatorService jsonService;       

    /// <summary>Fired when both JSON blobs are parsed and cached.</summary>
    public event Action OnSessionReady = delegate { };

    private void Awake()
    {
        if (sessionManager == null)  sessionManager = SessionManager.instance;
        if (jsonService == null)     jsonService    = sessionManager.JsonService;
    }

    private IEnumerator Start()
    {
        // 1. Wait until ShowSelectionManager has written the raw JSON strings.
        yield return new WaitUntil(() =>
               sessionManager.runtimeCacheSO != null
            && !string.IsNullOrEmpty(sessionManager.runtimeCacheSO.CachedMarcherJSON)
            && !string.IsNullOrEmpty(sessionManager.runtimeCacheSO.CachedTimingJSON)
            && jsonService != null);

        Debug.Log("SessionBootstrapper: 📦 Raw JSON ready — parsing…");

        // 2. Parse via JsonService
        sessionManager.runtimeCacheSO.ParsedCountPositions =
            jsonService.ParseMarcherStateJSON(sessionManager.runtimeCacheSO.CachedMarcherJSON);

        sessionManager.runtimeCacheSO.SetTimingMap =
            jsonService.ParseSetTimingMapJSON(sessionManager.runtimeCacheSO.CachedTimingJSON);

        // (Optional sanity checks)
        if (sessionManager.runtimeCacheSO.ParsedCountPositions == null ||
            sessionManager.runtimeCacheSO.SetTimingMap == null)
        {
            Debug.LogError("SessionBootstrapper: ❌ Parsing failed, aborting bootstrap.");
            yield break;
        }

        Debug.Log("SessionBootstrapper: ✅ Parsing complete. Raising OnSessionReady.");
        OnSessionReady.Invoke();
    }
}
