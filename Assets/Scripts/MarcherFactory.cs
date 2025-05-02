// MarcherFactory.cs
using System.Collections.Generic;
using UnityEngine;

public class MarcherFactory : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject marcherPrefab;

    [Header("Injected")]
    RuntimeCacheSO runtimeCache;

    private readonly List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();

    public IReadOnlyList<MarcherPositionsManager> Marchers => marchers;
    private void Awake()
    {
        if (runtimeCache == null)
            runtimeCache = SessionManager.instance.runtimeCacheSO;
    }
    // MarcherFactory.cs  (add just below Awake)
    public void ClearExisting()
    {
        foreach (var m in marchers)
            if (m) Destroy(m.gameObject);

        marchers.Clear();
    }

    public void Build(int desiredCount, int totalSets)
    {
        // Trim / grow list to match desiredCount
        while (marchers.Count < desiredCount)  Spawn(nextIndex: marchers.Count, totalSets);
        while (marchers.Count > desiredCount)  DespawnLast();
    }

    private void Spawn(int nextIndex, int totalSets)
    {
        GameObject go = Instantiate(marcherPrefab, Vector3.zero, Quaternion.identity, transform);
        
        // Keep GameObject name consistent for JSON key
        go.name = $"Marcher{nextIndex + 1}";

        var mgr  = go.GetComponent<MarcherPositionsManager>();
        var ctrl = go.GetComponent<MarcherController>();

        mgr.InitializeSetCount(totalSets);
        ctrl.InitializeMarcher(runtimeCache);

        // Label only — for display
        if (go.TryGetComponent(out MarcherIdentityManager identity))
        {
            identity.SetName("MR ", nextIndex + 1); // Label shows MR1, MR2...
        }

        marchers.Add(mgr);
    }

    private void DespawnLast()
    {
        int idx = marchers.Count - 1;
        DestroyImmediate(marchers[idx].gameObject);
        marchers.RemoveAt(idx);
    }
}
