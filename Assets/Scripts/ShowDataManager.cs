using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class ShowDataManager
{
    private readonly MonoBehaviour coroutineHost;
    private readonly SessionState sessionState;
    private readonly string backendURL;

    public ShowDataManager(MonoBehaviour coroutineHost, SessionState sessionState, string backendURL)
    {
        this.coroutineHost = coroutineHost;
        this.sessionState = sessionState;
        this.backendURL = backendURL;
    }

    public void SaveShow()
    {
        coroutineHost.StartCoroutine(SaveShowCoroutine());
    }

    public void ExitShow(Action onComplete = null)
    {
        coroutineHost.StartCoroutine(ExitShowCoroutine(onComplete));
    }

    public void LogOut(){
         coroutineHost.StartCoroutine(LogOutCoroutine());
    }

    private IEnumerator ExitShowCoroutine(Action onComplete)
    {
        yield return SaveShowCoroutine();
        onComplete?.Invoke();
        ClearShowData();
    }

    private IEnumerator LogOutCoroutine(){
        
        yield return SaveShowCoroutine();
        SessionManager.instance.Logout();
    }

    public void CreateNewShowTemplate(string url, string showID, string title, string group, string email, string field, string year, int marchers, int sets, int props, string modified, string status,
    string userFolderId, string accountSheetId, string setOnExit, Action onSuccess, Action<string> onError)
    {
        coroutineHost.StartCoroutine(CreateShowCoroutine());

        IEnumerator CreateShowCoroutine()
        {
            WWWForm form = new WWWForm();
            form.AddField("action", "NewShow");
            form.AddField("showID", showID);
            form.AddField("showTitle", title);
            form.AddField("groupName", group);
            form.AddField("email", email);
            form.AddField("fieldType", field);
            form.AddField("productionYear", year);
            form.AddField("numberOfMarchers", marchers);
            form.AddField("numberOfSets", sets);
            form.AddField("numberOfProps", props);
            form.AddField("lastModified", modified);
            form.AddField("showStatus", status);
            form.AddField("userFolderId", userFolderId);
            form.AddField("accountSheetId", accountSheetId);
            form.AddField("lastSet", setOnExit);

            using (UnityWebRequest www = UnityWebRequest.Post(url, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("✅ Google Sheet copied successfully.");
                    onSuccess?.Invoke();
                }
                else
                {
                    string err = www.error;
                    Debug.LogError("❌ Error copying Google Sheet: " + err);
                    onError?.Invoke(err);
                }
            }
        }
    }

    private IEnumerator SaveShowCoroutine()
    {
        if (string.IsNullOrEmpty(sessionState.LastSet) || sessionState.NumberOfMarchers <= 0)
        {
            Debug.LogWarning("⚠️ Attempting to save invalid show state. Aborting save.");
            yield break;
        }

        Debug.Log($"📡 Updating Show Details for {sessionState.ShowTitle} ({sessionState.CurrentShowID})");

        // 🔥 Save JSON and get the actual path
        EnsembleDirector2 director = GameObject.FindObjectOfType<EnsembleDirector2>();

        if (director != null)
        {
            string savedJsonPath = director.SaveMarcherStateToFile();
            if (!string.IsNullOrEmpty(savedJsonPath))
            {
                coroutineHost.StartCoroutine(UploadMarcherJSON(savedJsonPath));
            }

            string timingJsonPath = director.SaveSetTimingMapToFile();
            if (!string.IsNullOrEmpty(timingJsonPath))
            {
                coroutineHost.StartCoroutine(UploadSetTimingJSON(timingJsonPath));
            }

        }

        // 🔁 Save core show details to Google Sheets
        WWWForm form = new WWWForm();
        form.AddField("action", "UpdateShowDetails");
        form.AddField("numberOfMarchers", sessionState.NumberOfMarchers);
        form.AddField("numberOfSets", sessionState.NumberOfSets);
        form.AddField("numberOfProps", sessionState.NumberOfProps);
        form.AddField("lastModified", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        form.AddField("lastSet", sessionState.LastSet);
        form.AddField("showSheetId", SessionManager.instance.selectedShow?.showSheetID);

        using (UnityWebRequest www = UnityWebRequest.Post(backendURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Show Details updated successfully for {sessionState.ShowTitle}.");
            }
            else
            {
                Debug.LogError($"❌ Error updating Show Details: {www.error}");
            }
        }
    }


    private IEnumerator UploadMarcherJSON(string path)
    {
        Debug.Log($"📤 Uploading marcher JSON from path: {path}");
        
        string jsonContent = File.ReadAllText(path);
        var jsonData = JSON.Parse(jsonContent);

        JSONObject wrappedPayload = new JSONObject();
        wrappedPayload["action"] = "UpdateMarcherJSON";
        wrappedPayload["userFolderId"] = SessionManager.instance.SessionState.UserFolderId;
        wrappedPayload["showID"] = SessionManager.instance.SessionState.CurrentShowID;
        wrappedPayload["showSheetId"] = SessionManager.instance.selectedShow.showSheetID;
        wrappedPayload["marcherData"] = jsonData;

        string payloadStr = wrappedPayload.ToString();
        Debug.Log($"📦 Payload prepared (length: {payloadStr.Length} bytes)");
        
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payloadStr);

        UnityWebRequest www = new UnityWebRequest(backendURL, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("🔄 Sending JSON upload request...");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Marcher JSON uploaded successfully");
            string response = www.downloadHandler.text;
            Debug.Log($"📥 Server response: {response}");
        }
        else
        {
            Debug.LogError($"❌ Upload failed: {www.error}");
            Debug.LogError($"Response code: {www.responseCode}");
            if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
            {
                Debug.LogError($"Response body: {www.downloadHandler.text}");
            }
        }
    }

    private IEnumerator UploadSetTimingJSON(string path)
    {
        Debug.Log($"📤 Uploading SetTiming JSON from path: {path}");

        string jsonContent = File.ReadAllText(path);
        var jsonData = JSON.Parse(jsonContent);

        JSONObject payload = new JSONObject();
        payload["action"] = "UpdateSetTimingJSON";
        payload["userFolderId"] = SessionManager.instance.SessionState.UserFolderId;
        payload["showID"] = SessionManager.instance.SessionState.CurrentShowID;
        payload["showSheetId"] = SessionManager.instance.selectedShow.showSheetID;
        payload["setTimingData"] = jsonData;

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload.ToString());
        UnityWebRequest www = new UnityWebRequest(SessionManager.backendURL, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ SetTiming JSON uploaded successfully");
        }
        else
        {
            Debug.LogError($"❌ Failed to upload SetTiming JSON: {www.error}");
        }
    }


    private void ClearShowData()
    {
        Debug.Log("🧹 Clearing Show Data...");

        SessionManager.instance.selectedShow = null;
        sessionState.CurrentShowID = string.Empty;
        sessionState.ShowTitle = string.Empty;
        sessionState.GroupName = string.Empty;
        sessionState.CreatedBy = string.Empty;
        sessionState.FieldType = string.Empty;
        sessionState.ProductionYear = string.Empty;
        sessionState.NumberOfMarchers = 0;
        sessionState.NumberOfSets = 0;
        sessionState.NumberOfProps = 0;
        sessionState.LastModified = string.Empty;
        sessionState.ShowStatus = string.Empty;
        sessionState.LastSet = string.Empty;
        sessionState.SetsData.Clear();
        sessionState.MarchersData.Clear();
        sessionState.MarchersCoordinates.Clear();
    }
}
