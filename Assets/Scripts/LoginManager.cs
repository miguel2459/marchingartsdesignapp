using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI errorMessage;
    public LoginPanelsManager panelsManager;
    public Button loginButton;

    private string backendURL = "https://script.google.com/macros/s/AKfycbzjq9Od5--Dt8CxdTI4fcjXSEehNVet_vvowbs34SGf0QtYwLksIZx1HjJ0wxNEF_ROMw/exec";

    private void Start()
    {
        errorMessage.gameObject.SetActive(false);
    }

    public void OnLoginButtonPressed()
    {
        string email = emailField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Email and password cannot be empty.");
            return;
        }
        panelsManager.ShowLoading(true);
        StartCoroutine(ValidateLogin(email, password));
    }

    private IEnumerator ValidateLogin(string email, string password)
    {
        string url = backendURL + "?action=login&email=" + UnityWebRequest.EscapeURL(email) + "&password=" + UnityWebRequest.EscapeURL(password);
        
        Debug.Log("📡 Request URL: " + url); // ✅ Check request in Unity Console

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "UnityWebRequest"); // ✅ Prevent Google from blocking Unity

        yield return request.SendWebRequest();

        // ✅ Handle Network Errors
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Network error: " + request.error);
            panelsManager.HideLoading(true);
            ShowError("Network error. Please try again.");
            yield break; // Stop execution here
        }

        string rawResponse = request.downloadHandler.text;
        Debug.Log("📥 Response from server:\n" + rawResponse); // ✅ Log FULL response

        try
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(rawResponse);

            if (response.status == "success")
            {
                Debug.Log($"✅ Login Successful! User: {response.userName} | ID: {response.userId} | SheetID: {response.userSheetID}");

                // 🔹 Store user details in PlayerPrefs for persistence
                PlayerPrefs.SetString("UserID", response.userId);
                PlayerPrefs.SetString("UserEmail", email);
                PlayerPrefs.SetString("UserName", response.userName);
                PlayerPrefs.SetString("AccountSheetID", response.userSheetID);
                PlayerPrefs.Save();

                // 🔹 Initialize session after login
                SessionManager.instance.InitializeUser(response.userId, email, response.userName, response.userSheetID);

                SceneController.instance.SwitchScene(3);
            }
            else
            {
                panelsManager.HideLoading(true);
                ShowError(response.status == "incorrect_password" ? "Incorrect password." : "Account not found.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("🚨 JSON Parse Error: " + e.Message);
            Debug.LogError("📝 Raw Response (for debugging):\n" + rawResponse);
            ShowError("Error parsing response.");
            panelsManager.HideLoading(true);
        }
    }



    private void ShowError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string status;
        public string userId;
        public string userName;
        public string userSheetID;
    }
}
