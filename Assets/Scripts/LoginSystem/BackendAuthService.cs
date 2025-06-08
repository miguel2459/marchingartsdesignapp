// ✅ Drop-in Update: Retry + Timeout Logic for BackendAuthService and JsonBackendService

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace LoginSystem
{
    public class BackendAuthService : IAuthService
    {
        private readonly string backendURL;
        private readonly MonoBehaviour coroutineHost;
        private const int MaxRetries = 3;
        private const float TimeoutSeconds = 10f;

        public BackendAuthService(string backendURL, MonoBehaviour coroutineHost)
        {
            this.backendURL = backendURL;
            this.coroutineHost = coroutineHost;
        }

        public void Login(string email, string password, Action<LoginResult> onComplete)
        {
            coroutineHost.StartCoroutine(RetryCoroutine(() => LoginCoroutine(email, password, onComplete)));
        }

        public void SignUp(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            coroutineHost.StartCoroutine(RetryCoroutine(() => SignUpCoroutine(name, email, password, onComplete)));
        }

        private IEnumerator RetryCoroutine(Func<IEnumerator> operation)
        {
            int attempts = 0;
            while (attempts < MaxRetries)
            {
                bool isCompleted = false;
                yield return coroutineHost.StartCoroutine(operation.Invoke());
                if (isCompleted) yield break;

                attempts++;
                Debug.LogWarning($"⚠ Retry attempt {attempts}/{MaxRetries}");
                yield return new WaitForSeconds(2f); // Optional backoff delay
            }

            Debug.LogError("❌ Max retries reached. Aborting.");
        }

        private IEnumerator LoginCoroutine(string email, string password, Action<LoginResult> onComplete)
        {
            string url = backendURL + $"?action=login&email={UnityWebRequest.EscapeURL(email)}&password={UnityWebRequest.EscapeURL(password)}";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = (int)TimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Login error: {request.error}");
                onComplete?.Invoke(new LoginResult(false, "network_error"));
                yield break;
            }

            try
            {
                Debug.LogError("🚨 Raw login response: " + request.downloadHandler.text);
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                if (response.status == "success")
                    onComplete?.Invoke(new LoginResult(true, null, response.userId, response.userName, response.folderId, response.userSheetID));
                else
                    onComplete?.Invoke(new LoginResult(false, response.status));
            }
            catch (Exception e)
            {
                Debug.LogError($"🚨 Login JSON parse error: {e.Message}");
                onComplete?.Invoke(new LoginResult(false, "parse_error"));
            }
        }

        private IEnumerator SignUpCoroutine(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            string url = backendURL + $"?action=signup&name={UnityWebRequest.EscapeURL(name)}&email={UnityWebRequest.EscapeURL(email)}&password={UnityWebRequest.EscapeURL(password)}";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = (int)TimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ SignUp error: {request.error}");
                onComplete?.Invoke(new SignUpResult(false, "network_error"));
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<SignUpResponse>(request.downloadHandler.text);
                if (response.status == "success")
                    onComplete?.Invoke(new SignUpResult(true, null, response.userId, response.folderId, response.userSheetID));
                else
                    onComplete?.Invoke(new SignUpResult(false, response.status));
            }
            catch (Exception e)
            {
                Debug.LogError($"🚨 SignUp JSON parse error: {e.Message}");
                onComplete?.Invoke(new SignUpResult(false, "parse_error"));
            }
        }

        [Serializable]
        private class LoginResponse
        {
            public string status;
            public string userId;
            public string userName;
            public string userSheetID;
            public string folderId;
        }

        [Serializable]
        private class SignUpResponse
        {
            public string status;
            public string userId;
            public string userSheetID;
            public string folderId;
        }
    }
}
