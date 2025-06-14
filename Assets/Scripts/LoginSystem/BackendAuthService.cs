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

        // DELETED: The 'RetryCoroutine' method is removed entirely.
        // It was causing the unnecessary retries and confusion.
        /*
        private IEnumerator RetryCoroutine(Func<IEnumerator> operation)
        {
            // ... (Removed)
        }
        */

        private IEnumerator LoginCoroutine(string email, string password, Action<LoginResult> onComplete)
        {
            int attempts = 0;
            while (attempts < MaxRetries) // Loop for retries
            {
                attempts++; // Increment attempt counter for logging
                string url = backendURL;
                var payload = new LoginPayload { email = email, password = password};

                UnityWebRequest request = CreatePostRequest(url, payload);
                request.timeout = (int)TimeoutLoginSeconds;

                yield return request.SendWebRequest(); // Send the web request

                // Check for network/transport errors that *warrant* a retry
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"⚠ Login request failed (attempt {attempts}/{MaxRetries}): {request.error}. Retrying...");
                    if (attempts < MaxRetries)
                    {
                        yield return new WaitForSeconds(0.5f); // Small delay before next retry
                        continue; // Continue to the next iteration of the while loop for a retry
                    }
                    else
                    {
                        // Max retries reached for a network-related error. Abort.
                        Debug.LogError($"❌ Max retries reached for login. Aborting. Error: {request.error}");
                        onComplete?.Invoke(new LoginResult(false, "network_error")); // Report a generic network error
                        yield break; // Exit coroutine as we are done
                    }
                }
                else // request.result is Success, or another specific HTTP error (e.g., 401 for unauthorized) that means we got a valid response from backend.
                {
                    // Communication with the server was successful, even if the backend returned an error.
                    // Process the response and DO NOT RETRY.
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"📥 Login response: {jsonResponse}");
                        var response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                        if (response.status == "success")
                        {
                            onComplete?.Invoke(new LoginResult(true, null, response.userId, response.userName, response.folderId, response.userSheetID));
                        }
                        else
                        {
                            // Backend returned a specific application-level error (e.g., "incorrect_password").
                            // Pass this status directly to the error mapper.
                            onComplete?.Invoke(new LoginResult(false, response.status));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"🚨 Login JSON parse error: {e.Message}");
                        onComplete?.Invoke(new LoginResult(false, "parse_error")); // Report a JSON parsing error
                    }
                    finally
                    {
                        request.Dispose(); // Clean up the request
                    }
                    yield break; // Exit coroutine as we received a definitive response (success or application error)
                }
            }
        }

        private IEnumerator SignUpCoroutine(string name, string email, string password, Action<SignUpResult> onComplete)
        {
            int attempts = 0;
            while (attempts < MaxRetries) // Loop for retries
            {
                attempts++; // Increment attempt counter
                string url = backendURL;
                var payload = new SignUpPayload { name = name, email = email, password = password };

                UnityWebRequest request = CreatePostRequest(url, payload);
                request.timeout = (int)TimeoutSignUpSeconds;

                yield return request.SendWebRequest(); // Send the web request

                // Check for network/transport errors that *warrant* a retry
                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"⚠ SignUp request failed (attempt {attempts}/{MaxRetries}): {request.error}. Retrying...");
                    if (attempts < MaxRetries)
                    {
                        yield return new WaitForSeconds(0.5f); // Small delay before next retry
                        continue; // Continue to the next iteration of the while loop for a retry
                    }
                    else
                    {
                        // Max retries reached for a network-related error. Abort.
                        Debug.LogError($"❌ Max retries reached for signup. Aborting. Error: {request.error}");
                        onComplete?.Invoke(new SignUpResult(false, "network_error")); // Report a generic network error
                        yield break; // Exit coroutine as we are done
                    }
                }
                else // request.result is Success, or another specific HTTP error
                {
                    // Communication with the server was successful, even if the backend returned an error.
                    // Process the response and DO NOT RETRY.
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"📥 SignUp response: {jsonResponse}");
                        var response = JsonUtility.FromJson<SignUpResponse>(jsonResponse);

                        if (response.status == "success")
                        {
                            onComplete?.Invoke(new SignUpResult(true, null, response.userId, response.folderId, response.userSheetID));
                        }
                        else
                        {
                            // Backend returned a specific application-level error.
                            // Pass this status directly to the error mapper.
                            onComplete?.Invoke(new SignUpResult(false, response.status));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"🚨 SignUp JSON parse error: {e.Message}");
                        onComplete?.Invoke(new SignUpResult(false, "parse_error")); // Report a JSON parsing error
                    }
                    finally
                    {
                        request.Dispose(); // Clean up the request
                    }
                    yield break; // Exit coroutine
                }
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

            Debug.Log($"[Unity] Preparing POST request to: {url}");
            Debug.Log($"[Unity] Payload JSON: {json}");
            Debug.Log($"[Unity] Raw body size: {bodyRaw.Length} bytes");

            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[Unity] Request Header - Content-Type: {request.GetRequestHeader("Content-Type")}");
            return request;
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