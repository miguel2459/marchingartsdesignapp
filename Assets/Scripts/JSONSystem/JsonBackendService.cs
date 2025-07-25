using System;
using System.Collections;
using System.Threading.Tasks;
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
            Debug.Log($"✅ Upload successful for {jsonType} for {showId}. Server response: {request.downloadHandler.text}");
        else
            Debug.LogError($"❌ Upload failed for {jsonType} for {showId}: {request.error}, {request.downloadHandler.text}");

        onComplete?.Invoke(success);
        request.Dispose();
    }
    
    public async Task<CachedJsonMetadata> RequestDownloadJsonWithMetadataAsync(string showId, string jsonType)
    {
        string action = jsonType == "marcher" ? "getMarcherJson" : "getTimingJson";
        var uriBuilder = new UriBuilder(backendURL);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["action"] = action;
        query["showId"] = showId;
        query["accountSheetId"] = accountSheetId;
        uriBuilder.Query = query.ToString();

        string finalUrl = uriBuilder.ToString();
        Debug.Log($"📥 Requesting {jsonType} JSON metadata for {showId} from: {finalUrl}");

        UnityWebRequest request = UnityWebRequest.Get(finalUrl);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("User-Agent", "UnityWebRequest");

        var op = request.SendWebRequest();
        while (!op.isDone)
            await Task.Yield(); // async wait without blocking

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler?.text ?? "";
            try
            {
                var root = JSON.Parse(result);
                string jsonContent = root["data"].ToString();
                string timestampStr = root["timestamp"];
                DateTime timestamp = DateTime.TryParse(timestampStr, out var parsedTime)
                    ? parsedTime : DateTime.UtcNow;

                Debug.Log($"✅ Received {jsonType} JSON with timestamp: {timestamp:O}");
                return new CachedJsonMetadata(jsonContent, timestamp);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to parse metadata response: {e.Message}\n{request.downloadHandler?.text}");
                return null;
            }
        }
        else
        {
            Debug.LogError($"❌ Request failed: {request.error}, Response: {request.downloadHandler?.text ?? "(empty)"}");
            return null;
        }
    } 
    
    // private IEnumerator DownloadJsonWithMetadataCoroutine(string showId, string jsonType, Action<CachedJsonMetadata> onComplete)
    // {
    //     string action = jsonType == MARCHER_TYPE ? "getMarcherJson" : "getTimingJson";
    //     string baseUrl = backendURL;
    //
    //     var uriBuilder = new System.UriBuilder(baseUrl);
    //     var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
    //     query["action"] = action;
    //     query["showId"] = showId;
    //     query["accountSheetId"] = accountSheetId;
    //     uriBuilder.Query = query.ToString();
    //
    //     string finalUrl = uriBuilder.ToString();
    //     Debug.Log($"📥 Requesting {jsonType} JSON metadata for {showId} from: {finalUrl}");
    //
    //     UnityWebRequest request = UnityWebRequest.Get(finalUrl);
    //     request.downloadHandler = new DownloadHandlerBuffer();
    //     request.SetRequestHeader("User-Agent", "UnityWebRequest");
    //
    //     yield return request.SendWebRequest();
    //
    //     if (request.result == UnityWebRequest.Result.Success)
    //     {
    //         string result = request.downloadHandler?.text ?? "";
    //         try
    //         {
    //             var root = JSON.Parse(result);
    //             string jsonContent = root["data"].ToString();  // Serialize back to string
    //             string timestampStr = root["timestamp"];
    //             DateTime timestamp = DateTime.TryParse(timestampStr, out var parsedTime)
    //                 ? parsedTime : DateTime.UtcNow;
    //
    //             Debug.Log($"✅ Received {jsonType} JSON with timestamp: {timestamp:O}");
    //             onComplete?.Invoke(new CachedJsonMetadata(jsonContent, timestamp));
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"❌ Failed to parse metadata response: {e.Message}\n{result}");
    //             onComplete?.Invoke(null);
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError($"❌ Request failed: {request.error}, Response: {request.downloadHandler?.text ?? "(empty)"}");
    //         onComplete?.Invoke(null);
    //     }
    //
    //     request.Dispose();
    // }
}
