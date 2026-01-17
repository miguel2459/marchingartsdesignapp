using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ConfigLoader : MonoBehaviour
{
    [Header("Cloud Function Endpoint")]
    [SerializeField] private string configEndpoint =
        "https://us-central1-mada-backend.cloudfunctions.net/getConfig";

    [SerializeField] private SessionStateValidator validator;

    [System.Serializable]
    private class ConfigResponse
    {
        public string apiKey;
        public string backendURL;
    }

    public IEnumerator LoadAndStore()
    {
        Debug.Log("🌐 ConfigLoader: Requesting config from Google Cloud...");

        using (UnityWebRequest request = UnityWebRequest.Get(configEndpoint))
        {
            request.SetRequestHeader("Accept", "application/json");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : "(null)";
                long statusCode = request.responseCode;
                string allHeaders = "";
                foreach (var kvp in request.GetResponseHeaders() ?? new System.Collections.Generic.Dictionary<string,string>())
                    allHeaders += $"{kvp.Key}: {kvp.Value}\n";

                Debug.LogError(
                    $"❌ ConfigLoader: Config fetch failed\n" +
                    $"    Error: {request.error}\n" +
                    $"    Status: {statusCode}\n" +
                    $"    URL: {configEndpoint}\n" +
                    $"    Headers:\n{allHeaders}" +
                    $"    Body:\n{responseText}"
                );
                yield break;
            }

            var json = request.downloadHandler.text;
            Debug.Log($"📦 ConfigLoader: Config raw response: {json}");

            ConfigResponse config = JsonUtility.FromJson<ConfigResponse>(json);

            if (!string.IsNullOrEmpty(config.apiKey) && !string.IsNullOrEmpty(config.backendURL))
            {
                PlayerPrefs.SetString("MADA_API_KEY", config.apiKey);
                PlayerPrefs.SetString("MADA_BACKEND_URL", config.backendURL);
                PlayerPrefs.Save();

                Debug.Log("✅ ConfigLoader: Config loaded and stored to PlayerPrefs.");
                StartCoroutine(validator.StartValidation());
            }
            else
            {
                Debug.LogError("❌ ConfigLoader: Config response missing required fields.");
            }
        }
    }
}