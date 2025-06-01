using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LoginSystem
{
    public class LoginManager : MonoBehaviour
    {
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TextMeshProUGUI errorMessage;
        public LoginPanelsManager panelsManager;
        public Button loginButton;

        private IAuthService backendAuth;

        private void Awake()
        {
            ServiceLocator.Initialize(this, useMock: false); // or true for testing
            backendAuth = ServiceLocator.AuthService;
        }

        public void HideError() => errorMessage.gameObject.SetActive(false);

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
            backendAuth.Login(email, password, result =>
            {
                panelsManager.HideLoading(true);

                if (result.Success)
                {
                    Debug.Log($"✅ Login Successful: {result.UserName} ({result.UserId})");
                    SessionManager.instance.userSession.SaveSession(
                        new UserData(result.UserId, email, result.UserName, result.UserSheetID, result.FolderId)
                    );

                    StartCoroutine(SceneController.instance.WaitForSessionInitialization());
                }
                else
                {
                    string friendlyMessage = BackendErrorMapper.GetFriendlyMessage(result.ErrorMessage);
                    ShowError(friendlyMessage);
                }
            });
        }

        private void ShowError(string message)
        {
            errorMessage.text = message;
            errorMessage.gameObject.SetActive(true);
        }
    }
}
