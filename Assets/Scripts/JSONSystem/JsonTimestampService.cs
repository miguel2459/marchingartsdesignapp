// JsonTimestampService.cs
using System;
using System.IO;
using UnityEngine;

public class JsonTimestampService
{
    private static readonly string metaDirectory = Path.Combine(Application.persistentDataPath, "MADA_JSONS_META");

    public static void SaveTimestamp(string showId, string jsonType, DateTime timestamp)
    {
        Directory.CreateDirectory(metaDirectory);
        string path = GetMetaPath(showId, jsonType);
        File.WriteAllText(path, timestamp.ToString("o"));
    }

    public static DateTime? LoadTimestamp(string showId, string jsonType)
    {
        string path = GetMetaPath(showId, jsonType);
        if (!File.Exists(path)) return null;
        string iso = File.ReadAllText(path);
        return DateTime.TryParse(iso, out var parsed) ? parsed : (DateTime?)null;
    }

    private static string GetMetaPath(string showId, string jsonType)
    {
        string suffix = jsonType == "marcher" ? "_marcher.meta" : "_timing.meta";
        return Path.Combine(metaDirectory, showId + suffix);
    }
}