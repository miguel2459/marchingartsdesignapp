using System.Collections;
using UnityEngine;
using System;

public class SessionBootstrapper : MonoBehaviour
{
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private JsonCoordinatorService jsonService;      

    /// <summary>Fired when both JSON blobs are parsed and cached.</summary>
    public event Action OnSessionReady = delegate { };

    private void Awake()
    {
        if (sessionManager == null)  sessionManager = SessionManager.instance;
        if (jsonService == null)     jsonService    = sessionManager.JsonService;
    }

    private IEnumerator Start()
    {
        Debug.Log("🚀 SessionBootstrapper starting...");

        string showId = sessionManager.showStateSO.CurrentShowID;
        if (string.IsNullOrEmpty(showId))
        {
            Debug.LogError("❌ SessionBootstrapper: Show ID is missing in ShowStateSO.");
            yield break;
        }

        bool marcherDone = false;
        bool timingDone = false;

        string marcherJson = null;
        string timingJson = null;

        // Request marcher JSON
        jsonService.GetJson(showId, "marcher", json =>
        {
            marcherJson = json;
            marcherDone = true;
        });

        // Request timing JSON
        jsonService.GetJson(showId, "timing", json =>
        {
            timingJson = json;
            timingDone = true;
        });

        // Wait for both async callbacks
        yield return new WaitUntil(() => marcherDone && timingDone);

        // Cache them into RuntimeCacheSO
        sessionManager.runtimeCacheSO.CachedMarcherJSON = marcherJson;
        sessionManager.runtimeCacheSO.CachedTimingJSON = timingJson;

        Debug.Log("SessionBootstrapper: 📦 Raw JSON ready — parsing…");

        jsonService.ParseMarcherStateJSON(marcherJson, out var countData, out var identityData);
        sessionManager.runtimeCacheSO.ParsedCountPositions = countData;
        sessionManager.runtimeCacheSO.ParsedIdentities = identityData;

        sessionManager.runtimeCacheSO.SetTimingMap = jsonService.ParseSetTimingMapJSON(timingJson);

        // Sanity check
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
