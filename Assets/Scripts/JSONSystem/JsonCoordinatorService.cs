using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unified interface that coordinates JSON services:
/// - generation
/// - parsing
/// - local cache
/// - backend upload/download
/// </summary>
public class JsonCoordinatorService
{
    private readonly JsonGenerationService generationService;
    private readonly JsonParserService parserService;
    private readonly JsonCacheService cacheService;
    private readonly JsonBackendService backendService;

    public JsonCoordinatorService(JsonGenerationService gen, JsonParserService parser, JsonCacheService cache, JsonBackendService backend)
    {
        generationService = gen;
        parserService = parser;
        cacheService = cache;
        backendService = backend;
    }
    
    public async Task<string> GetJsonAsync(string showId, string jsonType)
    {
        bool hasLocalFile = cacheService.DoesCacheExist(showId, jsonType);
        string cached = hasLocalFile ? cacheService.LoadJsonFromLocalCache(showId, jsonType) : null;
        DateTime? localTimestamp = hasLocalFile ? JsonTimestampService.LoadTimestamp(showId, jsonType) : null;

        // If no local file, we must fetch from backend
        if (!hasLocalFile)
        {
            Debug.Log($"📂 No local cache for {jsonType} of {showId}. Fetching from backend...");
            var metadata = await backendService.RequestDownloadJsonWithMetadataAsync(showId, jsonType);
            if (metadata != null)
            {
                cacheService.SaveJsonToLocalCache(showId, jsonType, metadata.jsonContent);
                JsonTimestampService.SaveTimestamp(showId, jsonType, metadata.serverTimestamp);
                return metadata.jsonContent;
            }
            else
            {
                Debug.LogError($"❌ Failed to download {jsonType} for {showId} with no local fallback available.");
                return null;
            }
        }

        // Local file exists — compare timestamps
        var metaCheck = await backendService.RequestDownloadJsonWithMetadataAsync(showId, jsonType);
        if (metaCheck == null)
        {
            Debug.LogWarning($"⚠️ Failed to get backend metadata for {jsonType} of {showId}. Using local cache.");
            return cached;
        }

        bool isCloudNewer = !localTimestamp.HasValue || metaCheck.serverTimestamp > localTimestamp.Value;

        if (isCloudNewer)
        {
            Debug.Log($"☁️ Cloud version is newer. Updating local cache for {jsonType} of {showId}.");
            cacheService.SaveJsonToLocalCache(showId, jsonType, metaCheck.jsonContent);
            JsonTimestampService.SaveTimestamp(showId, jsonType, metaCheck.serverTimestamp);
            return metaCheck.jsonContent;
        }
        else
        {
            Debug.Log($"📂 Using cached {jsonType} JSON for {showId}, already up to date.");
            return cached;
        }
    }
    
    public string LoadJsonFromCache(string showId, string jsonType)
    {
        return cacheService.LoadJsonFromLocalCache(showId, jsonType);
    }

    /// <summary>
    /// Saves JSON content directly to the backend and caches it locally.
    /// </summary>
    public void SaveJson(string showId, string jsonType, string jsonContent, Action<bool> onComplete)
    {
        cacheService.SaveJsonToLocalCache(showId, jsonType, jsonContent);
        backendService.RequestUploadJson(showId, jsonType, jsonContent, success =>
        {
            Debug.Log($"JsonCoordinatorService: {jsonType} JSON for {showId}, upload status = {success}");
            onComplete?.Invoke(success);
        });
    }

    /// <summary>
    /// Wrapper around generation methods.
    /// </summary>
    public string GenerateMarcherStateJSON(List<MarcherPositionsManager> marchers) =>
        generationService.GenerateMarcherStateJSON(marchers);

    public string GenerateSetTimingMapJSON(Dictionary<int, RuntimeCacheSO.SetTimingData> map) =>
        generationService.GenerateSetTimingMapJSON(map);

    public string GenerateDefaultMarcherJson(int count) =>
        generationService.GenerateDefaultMarcherJson(count);

    public string GenerateDefaultTimingJson(int sets) =>
        generationService.GenerateDefaultTimingJson(sets);

    /// <summary>
    /// Wrapper around parsing methods.
    /// </summary>
    public IEnumerator ParseMarcherStateJSONAsync(string jsonText, Action<Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>, Dictionary<string, MarcherIdentity>> onComplete)
    {
        yield return parserService.ParseMarcherStateJSONAsync(jsonText, onComplete);
    }

    public IEnumerator ParseSetTimingMapJSONAsync(string jsonText, Action<Dictionary<int, RuntimeCacheSO.SetTimingData>> onComplete)
    {
        yield return parserService.ParseSetTimingMapJSONAsync(jsonText, onComplete);
    }

}
