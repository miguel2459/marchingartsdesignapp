using System;
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

    /// <summary>
    /// Attempts to retrieve JSON content using local cache first, then falling back to backend if needed.
    /// </summary>
    public void GetJson(string showId, string jsonType, Action<string> onResult)
    {
        string cached = cacheService.LoadJsonFromLocalCache(showId, jsonType);
        DateTime? localTimestamp = JsonTimestampService.LoadTimestamp(showId, jsonType);

        backendService.RequestDownloadJsonWithMetadata(showId, jsonType, metadata =>
        {
            if (metadata == null)
            {
                Debug.LogWarning($"⚠️ JsonCoordinatorService: Failed to download metadata. Falling back to cache.");
                onResult?.Invoke(cached);
                return;
            }

            bool isCloudNewer = !localTimestamp.HasValue || metadata.serverTimestamp > localTimestamp.Value;

            if (isCloudNewer)
            {
                Debug.Log($"☁️ Cloud version is newer. Updating local cache for {jsonType} of {showId}.");
                cacheService.SaveJsonToLocalCache(showId, jsonType, metadata.jsonContent);
                JsonTimestampService.SaveTimestamp(showId, jsonType, metadata.serverTimestamp);
                onResult?.Invoke(metadata.jsonContent);
            }
            else
            {
                Debug.Log($"📂 Using cached {jsonType} JSON for {showId}, already up to date.");
                onResult?.Invoke(cached);
            }
        });
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
    public void ParseMarcherStateJSON(string jsonText,
    out Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>> countPositions,
    out Dictionary<string, MarcherIdentity> identities)
    {
        parserService.ParseMarcherStateJSON(jsonText, out countPositions, out identities);
    }

    public Dictionary<int, RuntimeCacheSO.SetTimingData> ParseSetTimingMapJSON(string jsonText) =>
        parserService.ParseSetTimingMapJSON(jsonText);
}
