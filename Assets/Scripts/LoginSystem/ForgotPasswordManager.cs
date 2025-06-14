using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LoginSystem;

public class ForgotPasswordManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject forgotPasswordPanel;
    public TMP_InputField emailInputField;
    public Button submitButton;
    public Button returnToLoginButton;
    public TextMeshProUGUI errorText; // Universal error message display

    [Header("Services")]
    public IAuthService  backendAuthService;

    private void Awake()
    {
        // Hook up button listeners
        submitButton.onClick.AddListener(OnSubmitClicked);
        returnToLoginButton.onClick.AddListener(OnReturnToLoginClicked);
        ServiceLocator.Initialize(this, useMock: false); // or true for testing
        backendAuthService = ServiceLocator.AuthService;
    }

    /// <summary>
    /// Called when the "Forgot Password?" button is clicked.
    /// </summary>
    public void ShowForgotPasswordPanel()
    {
        loginPanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
        errorText.text = "";
        emailInputField.text = "";
    }

    /// <summary>
    /// Called when the "Return to Login" button is clicked.
    /// </summary>
    private void OnReturnToLoginClicked()
    {
        forgotPasswordPanel.SetActive(false);
        loginPanel.SetActive(true);
        errorText.text = "";
    }

    /// <summary>
    /// Called when the "Submit" button is clicked to send a password reset email.
    /// </summary>
    private void OnSubmitClicked()
    {
        string email = emailInputField.text.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
        {
            errorText.text = "Please enter your email.";
            return;
        }

        errorText.text = "🔄 Submitting password reset request...";

        backendAuthService.RequestPasswordReset(email, (success, message) =>
        {
            if (success)
            {
                errorText.text = "✅ Password reset email sent! Check your inbox.";
            }
            else
            {
                errorText.text = BackendErrorMapper.GetFriendlyMessage(message);
            }
        });
    }
}
