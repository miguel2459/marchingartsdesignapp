using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LoginSystem
{
    public class SignUpManager : MonoBehaviour
    {
        public TMP_InputField nameField;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TextMeshProUGUI errorMessage;
        public LoginPanelsManager panelsManager;
        public Button createAccountButton;

        private IAuthService backendAuth;
        private void Awake()
        {
            ServiceLocator.Initialize(this, useMock: false); // or true for testing
            backendAuth = ServiceLocator.AuthService;
        }

        public void HideError() => errorMessage.gameObject.SetActive(false);

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
            backendAuth.SignUp(name, email, password, result =>
            {
                panelsManager.HideLoading(false);

                if (result.Success)
                {
                    Debug.Log($"✅ Account Created: {result.UserId}");
                    SessionManager.instance.userSession.SaveSession(
                        new UserData(result.UserId, email, name, result.UserSheetID, result.FolderId)
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
