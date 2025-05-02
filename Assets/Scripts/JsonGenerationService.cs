using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class JsonGenerationService
{
    public const string CurrentVersion = "3.0.0";

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
        root["version"] = CurrentVersion;
        root["timestamp"] = System.DateTime.UtcNow.ToString("o");
        var marcherArray = new JSONArray();

        foreach (var marcher in marchers)
        {
            if (marcher == null) continue;

            var marcherNode = new JSONObject();
            marcherNode["id"] = marcher.name;

            var countPosNode = new JSONObject();
            var allCountPositions = marcher.GetAllCountPositions(); // Assume this returns the new PositionEntry-based dictionary

            // Ensure Set 0, Count 0 is included
            if (!allCountPositions.ContainsKey(0))
                allCountPositions[0] = new Dictionary<int, PositionEntry>();
            if (!allCountPositions[0].ContainsKey(0))
                allCountPositions[0][0] = new PositionEntry(marcher.transform.position, "hold");

            foreach (var setEntry in allCountPositions)
            {
                int setIndex = setEntry.Key;
                var setObj = new JSONObject();

                foreach (var countEntry in setEntry.Value)
                {
                    int countIndex = countEntry.Key;
                    PositionEntry entry = countEntry.Value;

                    var entryObj = new JSONObject();
                    var posArray = new JSONArray();
                    posArray.Add(entry.pos.x);
                    posArray.Add(entry.pos.y);
                    posArray.Add(entry.pos.z);

                    entryObj["pos"] = posArray;
                    entryObj["type"] = entry.type;

                    setObj[countIndex.ToString()] = entryObj;
                }

                countPosNode[setIndex.ToString()] = setObj;
            }
            // Inside foreach (var marcher in marchers)
            if (marcher.TryGetComponent(out MarcherIdentityManager identityManager))
            {
                var id = identityManager.GetIdentity();
                var identityObj = new JSONObject();
                identityObj["section"] = id.section;
                identityObj["abbr"] = id.abbr;
                identityObj["number"] = id.number;
                marcherNode["identity"] = identityObj;
            }

            marcherNode["countPositions"] = countPosNode;
            marcherArray.Add(marcherNode);
        }

        root["marchers"] = marcherArray;
        Debug.Log("✅ Generated tagged Marcher State JSON (v2.0.0) with march/hold/unset structure.");
        return root.ToString(2);
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

            var countPosNode = new JSONObject(); // sets
            var set0 = new JSONObject();         // counts

            var posArray = new JSONArray();
            posArray.Add(defaultPosition.x);
            posArray.Add(defaultPosition.y);
            posArray.Add(defaultPosition.z);

            var entryObj = new JSONObject();
            entryObj["pos"] = posArray;
            entryObj["type"] = "hold";

            set0["0"] = entryObj;      // Count 0
            countPosNode["0"] = set0;  // Set 0

            marcherNode["countPositions"] = countPosNode;
            marcherArray.Add(marcherNode);
        }

        root["marchers"] = marcherArray;

        Debug.Log("✅ JsonGenerationService: Generated default marcher JSON (v2.0.0 compliant).");
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
