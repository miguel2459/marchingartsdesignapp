using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class JsonGenerationService
{
    public const string CurrentVersion = "2.0.0";

    /// <summary>
    /// Generates JSON representing all marcher positions (by set and count) for upload or caching.
    /// </summary>
    public string GenerateMarcherStateJSON(List<MarcherPositionsManager> marchers)
    {
     if (marchers == null)
        {
            Debug.LogError("GenerateMarcherStateJSON: Marchers list cannot be null.");
            return null;
        }

        var root = new JSONObject();
        root["version"] = "2.0.0"; // Or your desired version
        root["timestamp"] = System.DateTime.UtcNow.ToString("o");
        var marcherArray = new JSONArray();

        foreach (var marcher in marchers)
        {
            if (marcher == null) continue; // Skip null entries

            var marcherNode = new JSONObject();
            marcherNode["id"] = marcher.name;

            var countPosNode = new JSONObject();
            var allCountPositions = marcher.GetAllCountPositions();

            // Ensure Set 0, Count 0 is included (optional, based on your logic needs)
            if (!allCountPositions.ContainsKey(0))
                allCountPositions[0] = new Dictionary<int, Vector3>();
            if (!allCountPositions[0].ContainsKey(0))
                // Use current transform position as fallback for Set 0 Count 0 if needed
                allCountPositions[0][0] = marcher.transform.position;

            foreach (var setEntry in allCountPositions)
            {
                int setIndex = setEntry.Key;
                var setObj = new JSONObject();

                if (setEntry.Value == null || setEntry.Value.Count == 0)
                    continue; // Skip sets with no counts if necessary

                foreach (var countEntry in setEntry.Value)
                {
                    int countIndex = countEntry.Key;
                    Vector3 pos = countEntry.Value;
                    var posArray = new JSONArray();
                    posArray.Add(pos.x);
                    posArray.Add(pos.y);
                    posArray.Add(pos.z);
                    setObj[countIndex.ToString()] = posArray;
                }
                countPosNode[setIndex.ToString()] = setObj;
            }
            marcherNode["countPositions"] = countPosNode;
            marcherArray.Add(marcherNode);
        }
        root["marchers"] = marcherArray;
        Debug.Log("✅ Generated Marcher State JSON string within JsonPersistenceService.");
        return root.ToString(2); // Pretty print
    }

    /// <summary>
    /// Generates JSON for the set timing map.
    /// </summary>
    public string GenerateSetTimingMapJSON(Dictionary<int, RuntimeCacheSO.SetTimingData> setTimingMap)
    {
        if (setTimingMap == null)
        {
            Debug.LogError("JsonGenerationService: SetTimingMap dictionary cannot be null.");
            return null;
        }

        var root = new JSONObject();

        foreach (var entry in setTimingMap)
        {
            var data = entry.Value;
            JSONObject setNode = new JSONObject
            {
                ["count"] = data.count,
                ["startBPM"] = data.startBPM,
                ["endBPM"] = data.endBPM
            };
            root[entry.Key.ToString()] = setNode;
        }

        Debug.Log("✅ JsonGenerationService: Generated Set Timing Map JSON.");
        return root.ToString(2);
    }

    /// <summary>
    /// Generates a default marching JSON structure for a new show.
    /// </summary>
    public string GenerateDefaultMarcherJson(int marcherCount)
    {
        Vector3 defaultPosition = Vector3.zero;
        var root = new JSONObject
        {
            ["version"] = CurrentVersion,
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };

        var marcherArray = new JSONArray();

        for (int i = 0; i < marcherCount; i++)
        {
            var marcherNode = new JSONObject();
            marcherNode["id"] = $"Marcher{i + 1}";

            var countPosNode = new JSONObject();
            var set0 = new JSONObject();
            var posArray = new JSONArray();

            posArray.Add(defaultPosition.x);
            posArray.Add(defaultPosition.y);
            posArray.Add(defaultPosition.z);

            set0["0"] = posArray; // Add Set 0, Count 0 position
            countPosNode["0"] = set0;

            marcherNode["countPositions"] = countPosNode;
            marcherArray.Add(marcherNode);
        }

        root["marchers"] = marcherArray;
        Debug.Log("✅ JsonGenerationService: Generated default marcher JSON.");
        return root.ToString(2);
    }

    /// <summary>
    /// Generates a default timing JSON for a new show.
    /// </summary>
    public string GenerateDefaultTimingJson(int setCount)
    {
        var root = new JSONObject();

        for (int i = 1; i <= setCount; i++)
        {
            JSONObject setNode = new JSONObject();
            setNode["count"] = 8; // Default counts
            setNode["startBPM"] = 140f; // Default BPM
            setNode["endBPM"] = 140f; // Default BPM
            root[i.ToString()] = setNode;
        }

        Debug.Log("✅ JsonGenerationService: Generated default timing JSON.");
        return root.ToString(2);
    }
}
