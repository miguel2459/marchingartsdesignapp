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
    public Button createAccountButton;
    private string backendURL = "https://script.google.com/macros/s/AKfycbwLDHix4FxcdOSFJQXnQi-yLi-6yFVqUcTJU6hBMdytoX5dxCUys4oKzo0ZFNLAawYHoQ/exec"; // Set this to the deployed API URL

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

        StartCoroutine(CreateNewAccount(name, email, password));
    }

private IEnumerator CreateNewAccount(string name, string email, string password)
{
    string url = backendURL + "?action=signup&name=" + UnityWebRequest.EscapeURL(name) + "&email=" + UnityWebRequest.EscapeURL(email) + "&password=" + UnityWebRequest.EscapeURL(password);
    UnityWebRequest request = UnityWebRequest.Get(url);

    yield return request.SendWebRequest();

    if (request.result != UnityWebRequest.Result.Success)
    {
        ShowError("Network error. Please try again.");
    }
    else
    {
        string jsonResponse = request.downloadHandler.text;
        SignUpResponse response = JsonUtility.FromJson<SignUpResponse>(jsonResponse);

        if (response.status == "email_exists")
        {
            ShowError("Email already in use.");
        }
        else
        {
            Debug.Log("Account Created! User ID: " + response.userId);
            PlayerPrefs.SetString("UserID", response.userId);
            SceneController.instance.SwitchScene(3);
        }
    }
}

[System.Serializable]
private class SignUpResponse
{
    public string status;
    public string userId;
}


    private void ShowError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }
}
