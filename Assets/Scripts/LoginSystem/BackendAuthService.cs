// ✅ Drop-in Update: Retry + Timeout Logic for BackendAuthService and JsonBackendService
using System;
using System.Text;
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
            string url = backendURL + "?action=login";
            // *** MODIFIED: Use the new serializable LoginPayload class ***
            var payload = new LoginPayload { email = email, password = password }; 

            UnityWebRequest request = CreatePostRequest(url, payload);
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
                Debug.Log($"📥 Login response: {request.downloadHandler.text}");
                var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                if (response.status == "success")
                {
                    onComplete?.Invoke(new LoginResult(true, null, response.userId, response.userName, response.folderId, response.userSheetID));
                }
                else
                {
                    onComplete?.Invoke(new LoginResult(false, response.status));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"🚨 Login JSON parse error: {e.Message}");
                onComplete?.Invoke(new LoginResult(false, "parse_error"));
            }

            request.Dispose();
        }

        private IEnumerator SignUpCoroutine(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            string url = backendURL + "?action=signup";
            // *** MODIFIED: Use the new serializable SignUpPayload class ***
            var payload = new SignUpPayload { name = name, email = email, password = password };

            UnityWebRequest request = CreatePostRequest(url, payload);
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
                Debug.Log($"📥 SignUp response: {request.downloadHandler.text}");
                var response = JsonUtility.FromJson<SignUpResponse>(request.downloadHandler.text);

                if (response.status == "success")
                {
                    onComplete?.Invoke(new SignUpResult(true, null, response.userId, response.folderId, response.userSheetID));
                }
                else
                {
                    onComplete?.Invoke(new SignUpResult(false, response.status));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"🚨 SignUp JSON parse error: {e.Message}");
                onComplete?.Invoke(new SignUpResult(false, "parse_error"));
            }

            request.Dispose();
        }
        
        private UnityWebRequest CreatePostRequest(string url, object payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            Debug.Log($"[Unity] Preparing POST request to: {url}");
            Debug.Log($"[Unity] Payload JSON: {json}"); // This log will now show the correct payload
            Debug.Log($"[Unity] Raw body size: {bodyRaw.Length} bytes");

            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[Unity] Request Header - Content-Type: {request.GetRequestHeader("Content-Type")}");
            return request;
        }

        // *** ADDED: Serializable class for Login POST payload ***
        [Serializable]
        private class LoginPayload
        {
            public string email;
            public string password;
        }

        // *** ADDED: Serializable class for SignUp POST payload ***
        [Serializable]
        private class SignUpPayload
        {
            public string name;
            public string email;
            public string password;
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