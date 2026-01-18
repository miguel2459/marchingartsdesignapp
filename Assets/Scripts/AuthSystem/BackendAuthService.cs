// BackendAuthService.cs
// ✅ Drop-in Update: Corrected Retry + Timeout Logic for BackendAuthService
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LoginSystem
{
    public class BackendAuthService : IAuthService
    {
        private readonly string backendURL;
        private readonly MonoBehaviour coroutineHost;
        private const int MaxRetries = 3;
        private const float TimeoutLoginSeconds = 10f; // Typical login should be fast
        private const float TimeoutSignUpSeconds = 30f; // SignUp might involve more backend work (e.g., Drive operations)
        private const float TimeoutGuestSeconds = 30f; // Guest may create Drive assets like SignUp

        public BackendAuthService(string backendURL, MonoBehaviour coroutineHost)
        {
            this.backendURL = backendURL;
            this.coroutineHost = coroutineHost;
        }

        // MODIFIED: Directly start LoginCoroutine, which now handles its own retries
        public void Login(string email, string password, Action<LoginResult> onComplete)
        {
            coroutineHost.StartCoroutine(LoginCoroutine(email, password, onComplete));
        }

        // MODIFIED: Directly start SignUpCoroutine, which now handles its own retries
        public void SignUp(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            coroutineHost.StartCoroutine(SignUpCoroutine(name, email, password, onComplete));
        }
        public void GuestLogin(string email, Action<LoginResult> onComplete)
        {
            coroutineHost.StartCoroutine(GuestLoginCoroutine(email, onComplete));
        }

        private IEnumerator LoginCoroutine(string email, string password, Action<LoginResult> onComplete)
        {
            int attempts = 0;

            while (attempts < MaxRetries)
            {
                attempts++;

                string url = backendURL;
                var payload = new LoginPayload { email = email, password = password };

                // Using-scope guarantees Dispose() even when we 'continue' for a retry.
                using (UnityWebRequest request = CreatePostRequest(url, payload))
                {
                    request.timeout = (int)TimeoutLoginSeconds;

                    yield return request.SendWebRequest();

                    // 1) Retry ONLY on transient transport/data failures (NOT ProtocolError).
                    if (ShouldRetryTransport(request.result))
                    {
                        Debug.LogWarning($"⚠ Login transport failure (attempt {attempts}/{MaxRetries}): {request.error}");

                        if (attempts < MaxRetries)
                        {
                            yield return new WaitForSeconds(0.5f);
                            continue; // request disposed by using-scope
                        }

                        Debug.LogError($"❌ Max retries reached for login transport errors. Aborting. Last error: {request.error}");
                        onComplete?.Invoke(new LoginResult(false, "network_error"));
                        yield break;
                    }

                    // 2) ProtocolError = HTTP 4xx/5xx. Do NOT retry.
                    if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                        string status = TryExtractStatus<LoginResponse>(body);

                        if (string.IsNullOrEmpty(status))
                            status = request.responseCode >= 500 ? "server_error" : "http_error";

                        onComplete?.Invoke(new LoginResult(false, status));
                        yield break;
                    }

                    // 3) Success (or any non-retryable result): parse and return.
                    try
                    {
                        string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"📥 Login response: {jsonResponse}");
        #endif
                        var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                        if (response != null && response.status == "success")
                        {
                            onComplete?.Invoke(new LoginResult(true, null, response.userId, response.userName, response.folderId, response.userSheetID));
                        }
                        else
                        {
                            string status = response != null && !string.IsNullOrEmpty(response.status)
                                ? response.status
                                : "parse_error";

                            onComplete?.Invoke(new LoginResult(false, status));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"🚨 Login JSON parse error: {e.Message}");
                        onComplete?.Invoke(new LoginResult(false, "parse_error"));
                    }

                    yield break;
                }
            }
        }

        private IEnumerator SignUpCoroutine(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            int attempts = 0;

            while (attempts < MaxRetries)
            {
                attempts++;

                string url = backendURL;
                var payload = new SignUpPayload { name = name, email = email, password = password };

                using (UnityWebRequest request = CreatePostRequest(url, payload))
                {
                    request.timeout = (int)TimeoutSignUpSeconds;

                    yield return request.SendWebRequest();

                    // 1) Retry ONLY on transient transport/data failures (NOT ProtocolError).
                    if (ShouldRetryTransport(request.result))
                    {
                        Debug.LogWarning($"⚠ SignUp transport failure (attempt {attempts}/{MaxRetries}): {request.error}");

                        if (attempts < MaxRetries)
                        {
                            yield return new WaitForSeconds(0.5f);
                            continue; // request disposed by using-scope
                        }

                        Debug.LogError($"❌ Max retries reached for signup transport errors. Aborting. Last error: {request.error}");
                        onComplete?.Invoke(new SignUpResult(false, "network_error"));
                        yield break;
                    }

                    // 2) ProtocolError = HTTP 4xx/5xx. Do NOT retry.
                    if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                        string status = TryExtractStatus<SignUpResponse>(body);

                        if (string.IsNullOrEmpty(status))
                            status = request.responseCode >= 500 ? "server_error" : "http_error";

                        onComplete?.Invoke(new SignUpResult(false, status));
                        yield break;
                    }

                    // 3) Success (or any non-retryable result): parse and return.
                    try
                    {
                        string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"📥 SignUp response: {jsonResponse}");
        #endif
                        var response = JsonUtility.FromJson<SignUpResponse>(jsonResponse);

                        if (response != null && response.status == "success")
                        {
                            onComplete?.Invoke(new SignUpResult(true, null, response.userId, response.folderId, response.userSheetID));
                        }
                        else
                        {
                            string status = response != null && !string.IsNullOrEmpty(response.status)
                                ? response.status
                                : "parse_error";

                            onComplete?.Invoke(new SignUpResult(false, status));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"🚨 SignUp JSON parse error: {e.Message}");
                        onComplete?.Invoke(new SignUpResult(false, "parse_error"));
                    }

                    yield break;
                }
            }
        }
        
        private IEnumerator GuestLoginCoroutine(string email, Action<LoginResult> onComplete)
        {
            int attempts = 0;
        
            while (attempts < MaxRetries)
            {
                attempts++;
        
                string url = backendURL;
                var payload = new GuestLoginPayload { email = email };
        
                using (UnityWebRequest request = CreatePostRequest(url, payload))
                {
                    request.timeout = (int)TimeoutGuestSeconds;
        
                    yield return request.SendWebRequest();
        
                    // 1) Retry ONLY on transient transport/data failures (NOT ProtocolError).
                    if (ShouldRetryTransport(request.result))
                    {
                        Debug.LogWarning($"⚠ GuestLogin transport failure (attempt {attempts}/{MaxRetries}): {request.error}");
        
                        if (attempts < MaxRetries)
                        {
                            yield return new WaitForSeconds(0.5f);
                            continue;
                        }
        
                        Debug.LogError($"❌ Max retries reached for guest login transport errors. Aborting. Last error: {request.error}");
                        onComplete?.Invoke(new LoginResult(false, "network_error"));
                        yield break;
                    }
        
                    // 2) ProtocolError = HTTP 4xx/5xx. Do NOT retry.
                    if (request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                        string status = TryExtractStatus<LoginResponse>(body);
        
                        if (string.IsNullOrEmpty(status))
                            status = request.responseCode >= 500 ? "server_error" : "http_error";
        
                        onComplete?.Invoke(new LoginResult(false, status));
                        yield break;
                    }
        
                    // 3) Success: parse and return (same response shape as Login).
                    try
                    {
                        string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"📥 GuestLogin response: {jsonResponse}");
        #endif
                        var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
        
                        if (response != null && response.status == "success")
                        {
                            // userName may be "Guest" from backend, but we pass it through.
                            onComplete?.Invoke(new LoginResult(true, null, response.userId, response.userName, response.folderId, response.userSheetID));
                        }
                        else
                        {
                            string status = response != null && !string.IsNullOrEmpty(response.status)
                                ? response.status
                                : "parse_error";
        
                            onComplete?.Invoke(new LoginResult(false, status));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"🚨 GuestLogin JSON parse error: {e.Message}");
                        onComplete?.Invoke(new LoginResult(false, "parse_error"));
                    }
        
                    yield break;
                }
            }
        }
        
        // Retry policy:
        // - Retry on transient transport/data processing failures.
        // - Do NOT retry on ProtocolError (HTTP 4xx/5xx). Treat it as a definitive response.
        private static bool ShouldRetryTransport(UnityWebRequest.Result result)
        {
            return result == UnityWebRequest.Result.ConnectionError ||
                   result == UnityWebRequest.Result.DataProcessingError;
        }

        private static string TryExtractStatus<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var parsed = JsonUtility.FromJson<T>(json);
                if (parsed == null) return null;

                // All our response DTOs expose a public string field named 'status'.
                var statusField = typeof(T).GetField("status");
                return statusField?.GetValue(parsed) as string;
            }
            catch
            {
                return null;
            }
        }
        
        public void RequestPasswordReset(string email, Action<bool, string> onComplete)
        {
            coroutineHost.StartCoroutine(RequestPasswordResetCoroutine(email, onComplete));
        }

        private IEnumerator RequestPasswordResetCoroutine(string email, Action<bool, string> onComplete)
        {
            string url = backendURL;
            var payload = new PasswordResetPayload { email = email };

            UnityWebRequest request = CreatePostRequest(url, payload);
            request.timeout = (int)TimeoutSignUpSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Reset request error: {request.error}");
                onComplete?.Invoke(false, "network_error");
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<GenericResponse>(request.downloadHandler.text);
                bool success = response.status == "success";
                onComplete?.Invoke(success, response.message);
            }
            catch (Exception e)
            {
                Debug.LogError($"🚨 Reset JSON parse error: {e.Message}");
                onComplete?.Invoke(false, "parse_error");
            }

            request.Dispose();
        }

        [Serializable]
        private class GenericResponse
        {
            public string status;
            public string message;
        }

        private UnityWebRequest CreatePostRequest(string url, object payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            // IMPORTANT: Never log secrets (passwords, tokens). This method is shared by Login/SignUp.
            // Only log sanitized metadata in Editor/Development builds.
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Unity] Preparing POST request to: {url}");
            Debug.Log($"[Unity] Payload (sanitized): {GetSanitizedPayloadLog(payload)}");
            Debug.Log($"[Unity] Raw body size: {bodyRaw.Length} bytes");
        #endif

            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Unity] Request Header - Content-Type: {request.GetRequestHeader("Content-Type")}");
        #endif
            return request;
        }

        private static string GetSanitizedPayloadLog(object payload)
        {
            // Prefer explicit type handling over reflection so we don't accidentally miss a secret field.
            switch (payload)
            {
                case LoginPayload lp:
                    return $"{{action:'{lp.action}', email:'{MaskEmail(lp.email)}', password:'***'}}";
                case SignUpPayload sp:
                    return $"{{action:'{sp.action}', name:'{sp.name}', email:'{MaskEmail(sp.email)}', password:'***'}}";
                case PasswordResetPayload pr:
                    return $"{{action:'{pr.action}', email:'{MaskEmail(pr.email)}'}}";
                case GuestLoginPayload gp:
                    return $"{{action:'{gp.action}', email:'{MaskEmail(gp.email)}'}}";
                default:
                    // Avoid logging any unknown payload types because they may contain secrets.
                    return "{payload:'(unrecognized type - logging suppressed)'}";
            }
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        
            int at = email.IndexOf('@');
            if (at <= 1) return "***";
        
            string first = email.Substring(0, 1);
            string domain = at < email.Length - 1 ? email.Substring(at) : "";
            return $"{first}***{domain}";
        }

        // Serializable classes for payloads and responses
        [Serializable]
        private class LoginPayload
        {
            public string action = "login";
            public string email;
            public string password;
        }

        [Serializable]
        private class SignUpPayload
        {
            public string action = "signup";
            public string name;
            public string email;
            public string password;
        }

        [Serializable]
        private class GuestLoginPayload
        {
            public string action = "guestLogin";
            public string email;
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
        
        [Serializable]
        private class PasswordResetPayload
        {
            public string action = "requestPasswordReset";
            public string email;
        }
    }
}