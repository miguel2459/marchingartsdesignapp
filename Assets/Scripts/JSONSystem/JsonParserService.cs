using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

/// <summary>
/// Parses JSON strings into runtime C# data structures.
/// Used for marcher state and set timing map reconstruction.
/// </summary>
public class JsonParserService
{
    public void ParseMarcherStateJSON(string jsonText,
    out Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>> parsedCountPositions,
    out Dictionary<string, MarcherIdentity> parsedIdentities)
    {
        Debug.Log("📥 Parsing Marcher State JSON (v3.0.0) with tagging and identity support...");

        parsedCountPositions = new Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>();
        parsedIdentities = new Dictionary<string, MarcherIdentity>();

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("ParseMarcherStateJSON: Input JSON text is null or empty.");
            return;
        }

        try
        {
            var root = JSON.Parse(jsonText);
            var marcherArray = root["marchers"]?.AsArray;

            if (marcherArray == null)
            {
                Debug.LogError("ParseMarcherStateJSON: 'marchers' array not found or invalid.");
                return;
            }

            for (int i = 0; i < marcherArray.Count; i++)
            {
                var marcherData = marcherArray[i];
                string id = marcherData["id"];
                var countPositions = marcherData["countPositions"]?.AsObject;

                if (string.IsNullOrEmpty(id) || countPositions == null)
                {
                    Debug.LogWarning($"Skipping marcher at index {i} due to missing id or countPositions.");
                    continue;
                }

                // Parse identity if it exists
                if (marcherData.HasKey("identity"))
                {
                    var identityNode = marcherData["identity"];
                    parsedIdentities[id] = new MarcherIdentity
                    {
                        section = identityNode["section"],
                        abbr = identityNode["abbr"],
                        number = identityNode["number"].AsInt
                    };
                }

                // Parse position data
                Dictionary<int, Dictionary<int, PositionEntry>> restoredSetData = new Dictionary<int, Dictionary<int, PositionEntry>>();

                foreach (var setKvp in countPositions)
                {
                    if (!int.TryParse(setKvp.Key, out int setIndex))
                        continue;

                    var setObj = setKvp.Value?.AsObject;
                    if (setObj == null) continue;

                    Dictionary<int, PositionEntry> countDict = new Dictionary<int, PositionEntry>();

                    foreach (var countKvp in setObj)
                    {
                        if (!int.TryParse(countKvp.Key, out int countIndex))
                            continue;

                        var entryObj = countKvp.Value.AsObject;
                        if (entryObj == null || !entryObj.HasKey("pos") || !entryObj.HasKey("type"))
                        {
                            Debug.LogWarning($"Invalid PositionEntry for marcher {id}, Set {setIndex}, Count {countIndex}.");
                            continue;
                        }

                        var posArr = entryObj["pos"].AsArray;
                        string tag = entryObj["type"];

                        if (posArr.Count >= 3)
                        {
                            Vector3 pos = new Vector3(posArr[0].AsFloat, posArr[1].AsFloat, posArr[2].AsFloat);
                            countDict[countIndex] = new PositionEntry(pos, tag);
                        }
                    }

                    restoredSetData[setIndex] = countDict;
                }

                parsedCountPositions[id] = restoredSetData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ ParseMarcherStateJSON: Error parsing JSON: {e.Message}\n{jsonText}");
            parsedCountPositions = new Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>();
            parsedIdentities = new Dictionary<string, MarcherIdentity>();
            return;
        }

        Debug.Log($"✅ Parsed JSON for {parsedCountPositions.Count} marchers and {parsedIdentities.Count} identities.");
    }



    public Dictionary<int, RuntimeCacheSO.SetTimingData> ParseSetTimingMapJSON(string jsonText)
    {
        Debug.Log("📥 JsonParserService: Parsing Set Timing JSON...");
        var map = new Dictionary<int, RuntimeCacheSO.SetTimingData>();

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("JsonParserService: SetTiming JSON input is null or empty.");
            return map;
        }

        try
        {
            var root = JSON.Parse(jsonText);
            var jsonObject = root?.AsObject;

            foreach (var kvp in jsonObject)
            {
                if (!int.TryParse(kvp.Key, out int setIndex)) continue;

                var node = kvp.Value;
                int count = node["count"].AsInt;
                float startBPM = node["startBPM"].AsFloat;
                float endBPM = node["endBPM"].AsFloat;

                map[setIndex] = new RuntimeCacheSO.SetTimingData(setIndex, count, startBPM, endBPM);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"JsonParserService: Failed to parse SetTiming JSON. Error: {e.Message}");
        }

        Debug.Log($"✅ JsonParserService: Parsed timing map with {map.Count} entries.");
        return map;
    }
    
    public IEnumerator ParseMarcherStateJSONAsync(string jsonText, Action<Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>, Dictionary<string, MarcherIdentity>> onComplete)
    {
        yield return null; // allow async behavior

        ParseMarcherStateJSON(jsonText, out var countPositions, out var identities);
        onComplete?.Invoke(countPositions, identities);
    }

    public IEnumerator ParseSetTimingMapJSONAsync(string jsonText, Action<Dictionary<int, RuntimeCacheSO.SetTimingData>> onComplete)
    {
        yield return null; // async yield

        var timingMap = ParseSetTimingMapJSON(jsonText);
        onComplete?.Invoke(timingMap);
    }

}
