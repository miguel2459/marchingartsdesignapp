using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles saving and loading JSON data from the local file system.
/// Manages cache directory structure and file pathing.
/// </summary>
public class JsonCacheService
{
    private readonly string localCacheBasePath;

    private const string MARCHER_SUFFIX = "_marcher_positions.json";
    private const string TIMING_SUFFIX = "_set_timing.json";

    public JsonCacheService()
    {
        localCacheBasePath = Path.Combine(Application.persistentDataPath, "MADA_JSONS");
        Directory.CreateDirectory(localCacheBasePath); // Ensure the directory exists
        Debug.Log("JsonCacheService: ✅ Initialized cache path at " + localCacheBasePath);
    }

    private string GetLocalCachePath(string showId, string jsonType)
    {
        if (string.IsNullOrEmpty(showId))
        {
            Debug.LogError("JsonCacheService: showId cannot be null or empty.");
            return null;
        }

        string suffix = jsonType == "marcher" ? MARCHER_SUFFIX : TIMING_SUFFIX;
        return Path.Combine(localCacheBasePath, showId + suffix);
    }

    public bool SaveJsonToLocalCache(string showId, string jsonType, string jsonContent)
    {
        string path = GetLocalCachePath(showId, jsonType);
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            File.WriteAllText(path, jsonContent);
            Debug.Log($"💾 JsonCacheService: Saved {jsonType} JSON for {showId}, to {path}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JsonCacheService: Failed to save {jsonType} JSON for {showId}, to {path}: {e.Message}");
            return false;
        }
    }

    public string LoadJsonFromLocalCache(string showId, string jsonType)
    {
        string path = GetLocalCachePath(showId, jsonType);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning($"JsonCacheService: Cache miss for {jsonType} JSON for {showId}, at {path}");
            return null;
        }

        try
        {
            string content = File.ReadAllText(path);
            Debug.Log($"📂 JsonCacheService: Loaded {jsonType} JSON for {showId}, from cache: {path}");
            return content;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JsonCacheService: Failed to load {jsonType} JSON for {showId}, from {path}: {e.Message}");
            return null;
        }
    }

    public bool DoesCacheExist(string showId, string jsonType)
    {
        string path = GetLocalCachePath(showId, jsonType);
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }
}
