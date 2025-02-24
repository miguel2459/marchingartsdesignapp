using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class SignUpManager : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TextMeshProUGUI errorMessage;
    public LoginPanelsManager panelsManager;
    public Button createAccountButton;

    private string backendURL = "https://script.google.com/macros/s/AKfycbzjq9Od5--Dt8CxdTI4fcjXSEehNVet_vvowbs34SGf0QtYwLksIZx1HjJ0wxNEF_ROMw/exec"; // Update with latest deployed URL

    private void Start()
    {
        errorMessage.gameObject.SetActive(false);
    }

    public void OnCreateAccountButtonPressed()
    {
        string name = nameField.text.Trim();
        string email = emailField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("All fields must be filled.");
            return;
        }

        panelsManager.ShowLoading(false);
        StartCoroutine(CreateNewAccount(name, email, password));
    }

    private IEnumerator CreateNewAccount(string name, string email, string password)
    {
        string url = backendURL + "?action=signup&name=" + UnityWebRequest.EscapeURL(name) + "&email=" + UnityWebRequest.EscapeURL(email) + "&password=" + UnityWebRequest.EscapeURL(password);
        
        Debug.Log("📡 Sending Sign-Up Request to: " + url); // ✅ Debug URL

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("User-Agent", "UnityWebRequest"); // ✅ Prevent Google from blocking Unity

        yield return request.SendWebRequest();

        // ✅ Handle Network Errors
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Network error: " + request.error);
            panelsManager.HideLoading(false);
            ShowError("Network error. Please try again.");
            yield break; // Stop execution here
        }

        string rawResponse = request.downloadHandler.text;
        Debug.Log("📥 Response from server:\n" + rawResponse); // ✅ Log full response

        // ✅ Detect if the response is HTML instead of JSON
        if (rawResponse.TrimStart().StartsWith("<!DOCTYPE html") || rawResponse.TrimStart().StartsWith("<html"))
        {
            Debug.LogError("🚨 ERROR: Received an HTML page instead of JSON! Possible redirect or server error.");
            ShowError("Unexpected response from server.");
            yield break;
        }

        try
        {
            SignUpResponse response = JsonUtility.FromJson<SignUpResponse>(rawResponse);

            if (response.status == "success")
            {
                Debug.Log("✅ Account Created! User ID: " + response.userId);
                PlayerPrefs.SetString("UserID", response.userId);
                PlayerPrefs.SetString("UserEmail", email);
                PlayerPrefs.SetString("UserName", name);
                PlayerPrefs.SetString("AccountSheetID", response.userSheetID);
                PlayerPrefs.Save();

                // 🔹 Initialize session after account creation
                SessionManager.instance.InitializeUser(response.userId, email, name, response.userSheetID);

                SceneController.instance.SwitchScene(3);
            }
            else if (response.status == "email_exists")
            {
                panelsManager.HideLoading(false);
                ShowError("Email already in use.");
            }
            else
            {
                panelsManager.HideLoading(false);
                ShowError("Error creating account.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("🚨 JSON Parse Error: " + e.Message);
            Debug.LogError("📝 Raw Response:\n" + rawResponse);
            ShowError("Error parsing response.");
        }
    }

    private void ShowError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }

    [System.Serializable]
    private class SignUpResponse
    {
        public string status;
        public string userId;
        public string userSheetID;
    }
}
