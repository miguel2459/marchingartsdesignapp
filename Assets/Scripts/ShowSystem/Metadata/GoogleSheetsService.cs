using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class GoogleSheetsService
{
    private string apiKey;
    private MonoBehaviour coroutineHost;

    public GoogleSheetsService(string apiKey, MonoBehaviour coroutineHost)
    {
        this.apiKey = apiKey;
        this.coroutineHost = coroutineHost;
    }

    public void FetchUserShowMetadata(string sheetId, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"https://us-central1-mada-backend.cloudfunctions.net/appsScriptProxy?action=getUserShowMetadata&accountSheetId={sheetId}";
        coroutineHost.StartCoroutine(GetRequest(url, onSuccess, onError));
    }

    public void FetchShowList(string sheetId, System.Action<JSONNode> onSuccess, System.Action<string> onError)
    {
        string url = $"https://us-central1-mada-backend.cloudfunctions.net/appsScriptProxy?action=getShowList&accountSheetId={sheetId}";
        coroutineHost.StartCoroutine(GetJSONRequest(url, onSuccess, onError));
    }

    private IEnumerator GetRequest(string url, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                onError?.Invoke(request.error);
            else
                onSuccess?.Invoke(request.downloadHandler.text);
        }
    }

    private IEnumerator GetJSONRequest(string url, System.Action<JSONNode> onSuccess, System.Action<string> onError)
    {
        return GetRequest(url, 
            raw => {
                var json = JSON.Parse(raw);
                onSuccess?.Invoke(json);
            }, 
            onError
        );
    }
}
