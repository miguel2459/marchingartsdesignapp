// SessionLoader.cs
using UnityEngine;
using System;

[RequireComponent(typeof(SessionBootstrapper))]
public class EnsembleSessionLoader : MonoBehaviour
{
    [SerializeField] private SessionBootstrapper bootstrapper;
    [Header("Injected Session Data")]
    [Tooltip("ScriptableObject holding show state (LastSet, NumberOfSets, etc.)")]
    [SerializeField] private ShowStateSO showState;                // for LastSet
    [Tooltip("ScriptableObject holding runtime cache (SetTimingMap, ParsedCountPositions)")]
    [SerializeField] private RuntimeCacheSO runtimeCache;          // for SetTimingMap
    [Tooltip("Service used to generate/parse JSON blobs for marchers & timing")]
    [SerializeField] private JsonCoordinatorService jsonService;   // for JSON generation

    /// <summary>
    /// Raised once session data (showStateSO & runtimeCacheSO) has been parsed and is ready.
    /// </summary>
    public event Action OnReady = delegate { };

    private int numberOfMarchers;
    private int numberOfSets;
    //private int lastSet;

    public void EnsembleLoaderInit()
    {
        // auto‑assign if someone forgot to wire in Inspector
        if (bootstrapper == null)
            bootstrapper = GetComponent<SessionBootstrapper>();
        if (showState     == null) showState     = SessionManager.instance.showStateSO;
        if (runtimeCache  == null) runtimeCache  = SessionManager.instance.runtimeCacheSO;
        if (jsonService   == null) jsonService   = SessionManager.instance.JsonService;

        bootstrapper.OnSessionReady += HandleSessionReady;
        Debug.Log("Session Ready!");
    }

    private void OnDestroy()
    {
        if (bootstrapper != null)
            bootstrapper.OnSessionReady -= HandleSessionReady;
    }

    private void HandleSessionReady()
    {
        InitializeSession();
        OnReady.Invoke();
    }

    private void InitializeSession()
    {
        numberOfMarchers = showState.NumberOfMarchers;
        numberOfSets     = showState.NumberOfSets;

        Debug.Log($"SessionLoader: ✅ Loaded session → marchers={numberOfMarchers}, sets={numberOfSets}, lastSet={showState.LastSet}");
    }

    public void ReloadSession()
    {
        // re‑read showState if you like, or just re‑invoke the ready signal:
        InitializeSession();   
        OnReady.Invoke();
    }

    // Expose clean, read‑only properties
    public int NumberOfMarchers => numberOfMarchers;
    public int NumberOfSets     => numberOfSets;
    public int LastSet          => int.TryParse(showState.LastSet, out var ls) ? ls : 1;
    public ShowStateSO ShowState   => showState;
    public RuntimeCacheSO RuntimeCache => runtimeCache;
    public JsonCoordinatorService JsonService => jsonService;
}
