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
     public Dictionary<string, Dictionary<int, Dictionary<int, Vector3>>> ParseMarcherStateJSON(string jsonText)
    {
        Debug.Log("📥 Parsing Marcher State JSON within JsonPersistenceService...");
        var parsedData = new Dictionary<string, Dictionary<int, Dictionary<int, Vector3>>>();

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("ParseMarcherStateJSON: Input JSON text is null or empty.");
            return parsedData; // Return empty dictionary
        }

        try
        {
            var root = JSON.Parse(jsonText);
            var marcherArray = root["marchers"]?.AsArray; // Use null-conditional operator

            if (marcherArray == null)
            {
                Debug.LogError("ParseMarcherStateJSON: 'marchers' array not found or not an array in JSON.");
                return parsedData;
            }


            for (int i = 0; i < marcherArray.Count; i++)
            {
                var marcherData = marcherArray[i];
                string id = marcherData["id"];
                var countPositions = marcherData["countPositions"]?.AsObject; // Use null-conditional

                if (string.IsNullOrEmpty(id) || countPositions == null)
                {
                    Debug.LogWarning($"ParseMarcherStateJSON: Skipping marcher at index {i} due to missing id or countPositions.");
                    continue;
                }


                Dictionary<int, Dictionary<int, Vector3>> restoredSetData = new Dictionary<int, Dictionary<int, Vector3>>();

                foreach (var setKvp in countPositions)
                {
                    if (!int.TryParse(setKvp.Key, out int setIndex))
                    {
                        Debug.LogWarning($"ParseMarcherStateJSON: Could not parse set index '{setKvp.Key}' for marcher '{id}'. Skipping set.");
                        continue;
                    }

                    var countDict = new Dictionary<int, Vector3>();
                    var countsInSet = setKvp.Value?.AsObject; // Use null-conditional

                    if (countsInSet == null)
                    {
                        Debug.LogWarning($"ParseMarcherStateJSON: countPositions for set '{setIndex}' is not an object for marcher '{id}'. Skipping set.");
                        continue;
                    }

                    foreach (var countKvp in countsInSet)
                    {
                        if (!int.TryParse(countKvp.Key, out int countIndex))
                        {
                            Debug.LogWarning($"ParseMarcherStateJSON: Could not parse count index '{countKvp.Key}' for marcher '{id}', set '{setIndex}'. Skipping count.");
                            continue;
                        }

                        var vec = countKvp.Value?.AsArray; // Use null-conditional
                        if (vec != null && vec.Count >= 3)
                        {
                            // Safely parse floats
                            float x = vec[0].AsFloat;
                            float y = vec[1].AsFloat;
                            float z = vec[2].AsFloat;
                            countDict[countIndex] = new Vector3(x, y, z);
                        } else {
                            Debug.LogWarning($"ParseMarcherStateJSON: Invalid position array for marcher '{id}', set '{setIndex}', count '{countIndex}'. Skipping count.");
                        }
                    }
                    restoredSetData[setIndex] = countDict;
                }
                parsedData[id] = restoredSetData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ParseMarcherStateJSON: Error parsing JSON: {e.Message}\nJSON Text: {jsonText}");
            return new Dictionary<string, Dictionary<int, Dictionary<int, Vector3>>>(); // Return empty on error
        }

        Debug.Log($"✅ Parsed data for {parsedData.Count} marchers within JsonPersistenceService.");
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
