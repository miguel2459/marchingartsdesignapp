using System;
using System.Text;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class ShowDataManager
{
    private readonly MonoBehaviour coroutineHost;
    private readonly UserStateSO userStateSO;
    private readonly ShowStateSO showStateSO;
    private readonly string backendURL;

    public ShowDataManager(MonoBehaviour coroutineHost, UserStateSO userStateSO, ShowStateSO showStateSO, string backendURL)
    {
        this.coroutineHost = coroutineHost;
        this.userStateSO = userStateSO;
        this.showStateSO = showStateSO;
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

    public void CreateNewShowTemplate(
        string url,
        string showID,
        string title,
        string group,
        string email,
        string field,
        string year,
        int marchers,
        int sets,
        int props,
        string modified,
        string status,
        string userFolderId,
        string accountSheetId,
        string setOnExit,
        Action onSuccess,
        Action<string> onError
    )
    {
        coroutineHost.StartCoroutine(CreateShowCoroutine());

        IEnumerator CreateShowCoroutine()
        {
            NewShowPayload payload = new NewShowPayload
            {
                showID = showID,
                showTitle = title,
                groupName = group,
                email = email,
                fieldType = field,
                productionYear = year,
                numberOfMarchers = marchers,
                numberOfSets = sets,
                numberOfProps = props,
                lastModified = modified,
                showStatus = status,
                userFolderId = userFolderId,
                accountSheetId = accountSheetId,
                lastSet = setOnExit
            };

            string json = JsonUtility.ToJson(payload);
            Debug.Log($"📤 Sending NewShow JSON payload:\n{json}");

            UnityWebRequest www = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Google Sheet copied successfully.");
                onSuccess?.Invoke();
            }
            else
            {
                string err = www.error;
                Debug.LogError($"❌ Error copying Google Sheet: {err}");
                Debug.LogError($"📥 Server Response: {www.downloadHandler?.text}");
                onError?.Invoke(err);
            }
        }
    }


    private IEnumerator SaveShowCoroutine()
    {
        if (string.IsNullOrEmpty(showStateSO.LastSet) || showStateSO.NumberOfMarchers <= 0)
        {
            Debug.LogWarning("⚠️ Attempting to save invalid show state. Aborting save.");
            yield break;
        }

        Debug.Log($"📡 Updating Show Details for {showStateSO.ShowTitle} ({showStateSO.CurrentShowID})");

        // 🔥 Save JSON and get the actual path
        EnsembleDirector2 director = GameObject.FindObjectOfType<EnsembleDirector2>();

        if (director != null)
        {
            string marcherJson = director.GenerateMarcherStateJSON();
            if (!string.IsNullOrEmpty(marcherJson))
            {
                SessionManager.instance.JsonService.SaveJson(
                    showStateSO.CurrentShowID, 
                    "marcher", 
                    marcherJson, 
                    success => 
                    Debug.Log($"✅ Marcher JSON upload result: {success}"));
            }

            string timingJson = director.GenerateSetTimingMapJSON();
            if (!string.IsNullOrEmpty(timingJson))
            {
                SessionManager.instance.JsonService.SaveJson(
                    showStateSO.CurrentShowID, 
                    "timing", 
                    timingJson, 
                    success => 
                    Debug.Log($"✅ SetTiming JSON upload result: {success}"));
            }
        }
        
        // 🔁 Save core show details using serialized payload
        ShowDetailsUpdatePayload payload = new ShowDetailsUpdatePayload
        {
            numberOfMarchers = showStateSO.NumberOfMarchers,
            numberOfSets = showStateSO.NumberOfSets,
            numberOfProps = showStateSO.NumberOfProps,
            lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastSet = showStateSO.LastSet,
            showSheetId = SessionManager.instance.selectedShow?.showSheetID,
            accountSheetId = userStateSO.AccountSheetID,
            showID = showStateSO.CurrentShowID
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"📦 Serialized ShowDetailsUpdatePayload:\n{json}");

        using (UnityWebRequest request = new UnityWebRequest(backendURL, "POST"))
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Show Details updated successfully to GDrive for {showStateSO.ShowTitle}.");
            }
            else
            {
                Debug.LogError($"❌ Error updating Show Details to GDrive: {request.error}");
                Debug.LogError($"❌ Response Body: {request.downloadHandler.text}");
            }
        }
    }
    
    [Serializable]
    public class ShowDetailsUpdatePayload
    {
        public string action = "updateshowdetails";
        public int numberOfMarchers;
        public int numberOfSets;
        public int numberOfProps;
        public string lastModified;
        public string lastSet;
        public string showSheetId;
        public string accountSheetId;
        public string showID;
    }

    private IEnumerator UploadMarcherJSON(string path)
    {
        Debug.Log($"📤 Uploading marcher JSON from path: {path}");
        
        string jsonContent = File.ReadAllText(path);
        var jsonData = JSON.Parse(jsonContent);

        JSONObject wrappedPayload = new JSONObject();
        wrappedPayload["action"] = "UpdateMarcherJSON";
        wrappedPayload["userFolderId"] = userStateSO.UserFolderId;
        wrappedPayload["showID"] = showStateSO.CurrentShowID;
        wrappedPayload["accountSheetId"] = userStateSO.AccountSheetID;
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
            Debug.Log("✅ Marcher JSON uploaded successfully to GDrive");
            string response = www.downloadHandler.text;
            Debug.Log($"📥 Marcher JSON Server response: {response}");
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
        payload["userFolderId"] = userStateSO.UserFolderId;
        payload["showID"] = showStateSO.CurrentShowID;
        payload["accountSheetId"] = userStateSO.AccountSheetID;
        payload["setTimingData"] = jsonData;

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload.ToString());
        UnityWebRequest www = new UnityWebRequest(SessionManager.backendURL, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ SetTiming JSON uploaded successfully to GDrive");
            string response = www.downloadHandler.text;
            Debug.Log($"📥 Set Timing Server response: {response}");
        }
        else
        {
            Debug.LogError($"❌ Failed to upload SetTiming JSON: {www.error}");
        }
    }

    private void CopyFileToLocalCache(string sourcePath, string fileName)
    {
        string localDir = Path.Combine(Application.persistentDataPath, "MADA_JSONS");
        Directory.CreateDirectory(localDir);
        string destPath = Path.Combine(localDir, fileName);

        File.Copy(sourcePath, destPath, overwrite: true);
        Debug.Log($"📁 Local cache updated: {destPath}");
    }

    [Serializable]
    public class NewShowPayload
    {
        public string action = "NewShow";
        public string showID;
        public string showTitle;
        public string groupName;
        public string email;
        public string fieldType;
        public string productionYear;
        public int numberOfMarchers;
        public int numberOfSets;
        public int numberOfProps;
        public string lastModified;
        public string showStatus;
        public string userFolderId;
        public string accountSheetId;
        public string lastSet;
    }


    private void ClearShowData()
    {
        Debug.Log("🧹 Clearing Show Data...");

        SessionManager.instance.selectedShow = null;
        showStateSO.CurrentShowID = string.Empty;
        showStateSO.ShowTitle = string.Empty;
        showStateSO.GroupName = string.Empty;
        showStateSO.CreatedBy = string.Empty;
        showStateSO.FieldType = string.Empty;
        showStateSO.ProductionYear = string.Empty;
        showStateSO.NumberOfMarchers = 0;
        showStateSO.NumberOfSets = 0;
        showStateSO.NumberOfProps = 0;
        showStateSO.LastModified = string.Empty;
        showStateSO.ShowStatus = string.Empty;
        showStateSO.LastSet = string.Empty;
    }
}
