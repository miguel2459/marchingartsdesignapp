using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory responsible for instantiating, naming, and initializing marchers.
/// Maintains authoritative list of active marcher instances.
/// </summary>
public class MarcherFactory : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject marcherPrefab;

    [Header("Injected")]
    [SerializeField] private RuntimeCacheSO runtimeCache;

    private readonly List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();
    public IReadOnlyList<MarcherPositionsManager> Marchers => marchers;

    private void Awake()
    {
        if (runtimeCache == null)
        {
            runtimeCache = SessionManager.instance.runtimeCacheSO;
            // Debug.LogWarning("⚠️ MarcherFactory: runtimeCache injected via fallback.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears all currently spawned marcher GameObjects.
    /// </summary>
    public void ClearExisting()
    {
        foreach (var marcher in marchers)
        {
            if (marcher != null)
                Destroy(marcher.gameObject);
        }

        marchers.Clear();
        // Debug.Log("🧹 MarcherFactory: Cleared existing marchers.");
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the marcher list contains exactly the desired number.
    /// </summary>
    public void Build(int desiredCount, int totalSets)
    {
        while (marchers.Count < desiredCount)
            Spawn(marchers.Count, totalSets);

        while (marchers.Count > desiredCount)
            DespawnLast();
    }

    /// <summary>
    /// Spawns a single marcher at index with optional position override.
    /// </summary>
    public void Spawn(int nextIndex, int totalSets, Vector3? initialPosOverride = null)
    {
        Vector3 spawnPos = initialPosOverride ?? Vector3.zero;

        GameObject go = Instantiate(marcherPrefab, spawnPos, Quaternion.identity, transform);
        go.name = $"Marcher{nextIndex + 1}";

        // Fetch marcher components
        var marcherMgr  = go.GetComponent<MarcherPositionsManager>();
        var marcherCtrl = go.GetComponent<MarcherController>();

        if (marcherMgr == null || marcherCtrl == null)
        {
            Debug.LogError($"❌ Marcher{nextIndex + 1} is missing required components.");
            Destroy(go);
            return;
        }

        marcherMgr.InitializeSetCount(totalSets);
        marcherCtrl.InitializeMarcher(runtimeCache, marcherMgr);

        // Optional: Apply display identity
        if (go.TryGetComponent(out MarcherIdentityManager identity))
        {
            identity.SetName("MR ", nextIndex + 1); // Replace "MR " with a prefix field if needed
        }

        marchers.Add(marcherMgr);

        // Debug.Log($"➕ Spawned Marcher{nextIndex + 1} at {spawnPos}");
    }

    /// <summary>
    /// Removes the most recently added marcher from scene and list.
    /// </summary>
    private void DespawnLast()
    {
        int idx = marchers.Count - 1;

        if (idx >= 0)
        {
            DestroyImmediate(marchers[idx].gameObject);
            marchers.RemoveAt(idx);
            // Debug.Log($"➖ Despawned Marcher{idx + 1}");
        }
    }
}
