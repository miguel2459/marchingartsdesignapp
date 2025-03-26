using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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

    private IEnumerator ExitShowCoroutine(Action onComplete)
    {
        yield return SaveShowCoroutine();
        ClearShowData();

        onComplete?.Invoke();
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
        Debug.Log($"📡 Updating Show Details for {sessionState.ShowTitle} ({sessionState.CurrentShowID})");

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
