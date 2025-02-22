using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailField; // ✅ Changed from InputField to TMP_InputField
    public TMP_InputField passwordField; // ✅ Changed from InputField to TMP_InputField
    public TextMeshProUGUI errorMessage; // ✅ Changed from Text to TMP Text
    public Button loginButton;

    private string backendURL = "https://script.google.com/macros/s/AKfycbwLDHix4FxcdOSFJQXnQi-yLi-6yFVqUcTJU6hBMdytoX5dxCUys4oKzo0ZFNLAawYHoQ/exec"; // Set this to the deployed API URL

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

        StartCoroutine(ValidateLogin(email, password));
    }

    private IEnumerator ValidateLogin(string email, string password)
    {
        string url = backendURL + "?action=login&email=" + UnityWebRequest.EscapeURL(email) + "&password=" + UnityWebRequest.EscapeURL(password);
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowError("Network error. Please try again.");
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);

            if (response.status == "success")
            {
                Debug.Log("Login Successful! User ID: " + response.userId);
                PlayerPrefs.SetString("UserID", response.userId); // Store User ID
                SceneController.instance.SwitchScene(3); // Load Show Selection Scene
            }
            else if (response.status == "incorrect_password")
            {
                ShowError("Incorrect password.");
            }
            else
            {
                ShowError("Account not found.");
            }
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
    }
}
