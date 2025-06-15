using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class RemoteLogger : MonoBehaviour
{
    [Header("Google Script Endpoint")]
    [SerializeField] private string scriptURL;

    private string sessionID;
    private string deviceInfo;

    private List<LogEntry> queuedLogs = new List<LogEntry>();
    private float batchInterval = 2f;
    private int batchSize = 20;
    private bool isSending = false;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        string devicePart = !string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier) && SystemInfo.deviceUniqueIdentifier.Length >= 6
            ? SystemInfo.deviceUniqueIdentifier.Substring(0, 6)
            : Guid.NewGuid().ToString("N").Substring(0, 6);

        sessionID = devicePart + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        deviceInfo = SystemInfo.deviceModel + " | " + SystemInfo.operatingSystem;

        Application.logMessageReceived += HandleLog;
        StartCoroutine(BatchSenderLoop());
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            logString += "\n" + stackTrace;
        }

        queuedLogs.Add(new LogEntry { log = logString, type = type.ToString() });

        if (queuedLogs.Count >= batchSize && !isSending)
        {
            StartCoroutine(SendLogs());
        }
    }

    private IEnumerator BatchSenderLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(batchInterval);

            if (queuedLogs.Count > 0 && !isSending)
            {
                yield return StartCoroutine(SendLogs());
            }
        }
    }

    private IEnumerator SendLogs()
    {
        isSending = true;

        var logsToSend = new List<LogEntry>(queuedLogs);
        queuedLogs.Clear();

        LogBatchPayload payload = new LogBatchPayload
        {
            session = sessionID,
            device = deviceInfo,
            logs = logsToSend
        };

        string json = JsonUtility.ToJson(payload, true);

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(scriptURL, "POST"))
        {
            byte[] jsonToSend = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("📡 RemoteLogger retrying failed batch...");
                queuedLogs.InsertRange(0, logsToSend);
            }
        }

        isSending = false;
    }

    [Serializable]
    public class LogEntry
    {
        public string log;
        public string type;
    }

    [Serializable]
    public class LogBatchPayload
    {
        public string session;
        public string device;
        public List<LogEntry> logs;
    }
}
