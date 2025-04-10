using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

/// <summary>
/// Handles uploading and downloading JSON data to/from the backend using UnityWebRequest.
/// Should be invoked via coroutine-capable host (e.g., SessionManager).
/// </summary>
public class JsonBackendService
{
    private readonly MonoBehaviour coroutineHost;
    private readonly string backendURL;
    private readonly string userFolderId;
    private readonly string accountSheetId;

    private const string MARCHER_TYPE = "marcher";
    private const string TIMING_TYPE = "timing";

    public JsonBackendService(MonoBehaviour host, string backendURL, string userFolderId, string accountSheetId)
    {
        this.coroutineHost = host;
        this.backendURL = backendURL;
        this.userFolderId = userFolderId;
        this.accountSheetId = accountSheetId;
    }

    public void RequestUploadJson(string showId, string jsonType, string jsonContent, Action<bool> onComplete)
    {
        coroutineHost.StartCoroutine(UploadJsonCoroutine(showId, jsonType, jsonContent, onComplete));
    }

    private IEnumerator UploadJsonCoroutine(string showId, string jsonType, string jsonContent, Action<bool> onComplete)
    {
        Debug.Log($"📤 Uploading {jsonType} JSON for Show ID: {showId}...");

        string action = jsonType == MARCHER_TYPE ? "UpdateMarcherJSON" : "UpdateSetTimingJSON";
        JSONNode jsonData;

        try
        {
            jsonData = JSON.Parse(jsonContent);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to parse JSON content: {e.Message}");
            onComplete?.Invoke(false);
            yield break;
        }

        JSONObject payload = new JSONObject
        {
            ["action"] = action,
            ["userFolderId"] = userFolderId,
            ["showID"] = showId,
            ["accountSheetId"] = accountSheetId
        };

        if (jsonType == MARCHER_TYPE)
            payload["marcherData"] = jsonData;
        else
            payload["setTimingData"] = jsonData;

        string payloadJson = payload.ToString();
        Debug.Log($"📦 Payload Preview:\n{payloadJson}");
        
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload.ToString());

        UnityWebRequest request = new UnityWebRequest(backendURL, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;

        if (success)
            Debug.Log($"✅ Upload successful for {jsonType}. Server response: {request.downloadHandler.text}");
        else
            Debug.LogError($"❌ Upload failed for {jsonType}: {request.error}, {request.downloadHandler.text}");

        onComplete?.Invoke(success);
        request.Dispose();
    }

    public void RequestDownloadJson(string showId, string jsonType, Action<string> onComplete)
    {
        coroutineHost.StartCoroutine(DownloadJsonCoroutine(showId, jsonType, onComplete));
    }

    private IEnumerator DownloadJsonCoroutine(string showId, string jsonType, Action<string> onComplete)
    {
        string action = jsonType == MARCHER_TYPE ? "getMarcherJson" : "getTimingJson";
        string url = $"{backendURL}?action={action}&showId={UnityWebRequest.EscapeURL(showId)}&accountSheetId={UnityWebRequest.EscapeURL(accountSheetId)}";

        Debug.Log($"📥 Downloading {jsonType} JSON from: {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "UnityWebRequest");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string resultJson = request.downloadHandler.text;
            Debug.Log($"✅ Downloaded {jsonType} JSON.");
            onComplete?.Invoke(resultJson);
        }
        else
        {
            Debug.LogError($"❌ Failed to download {jsonType} JSON: {request.error}, {request.downloadHandler.text}");
            onComplete?.Invoke(null);
        }

        request.Dispose();
    }
}
