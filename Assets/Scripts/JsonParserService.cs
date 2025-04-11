using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

/// <summary>
/// Parses JSON strings into runtime C# data structures.
/// Used for marcher state and set timing map reconstruction.
/// </summary>
public class JsonParserService
{
    public Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>> ParseMarcherStateJSON(string jsonText)
    {
        Debug.Log("📥 Parsing Marcher State JSON (v2.0.0) with tagging support...");
        var parsedData = new Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>();

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("ParseMarcherStateJSON: Input JSON text is null or empty.");
            return parsedData;
        }

        try
        {
            var root = JSON.Parse(jsonText);
            var marcherArray = root["marchers"]?.AsArray;

            if (marcherArray == null)
            {
                Debug.LogError("ParseMarcherStateJSON: 'marchers' array not found or invalid.");
                return parsedData;
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

                parsedData[id] = restoredSetData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ ParseMarcherStateJSON: Error parsing JSON: {e.Message}\n{jsonText}");
            return new Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>>();
        }

        Debug.Log($"✅ Parsed marcher state JSON for {parsedData.Count} performers.");
        return parsedData;
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
}
