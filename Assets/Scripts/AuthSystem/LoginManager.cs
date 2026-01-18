using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
namespace LoginSystem
{
    public class LoginManager : MonoBehaviour
    {
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public LoginPanelsManager panelsManager;
        public Button signupButton;
        public Button loginButton;

        private IAuthService backendAuth;

        private void Awake()
        {
            if (!ServiceLocator.IsInitialized)
            {
                Debug.LogError("[LoginManager] ServiceLocator not initialized. Ensure LoginSceneBootstrapper exists in the scene.");
                // Safe fallback to avoid hard null refs in dev/test.
                ServiceLocator.Initialize(this, useMock: false);
            }

            backendAuth = ServiceLocator.AuthService;
        }
        
        void Update()
        {
            // Only process input if the login panel is active and not in a loading state
            if (panelsManager != null && panelsManager.loginPanel.activeInHierarchy && !panelsManager.loadingPanel.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
                    Selectable currentSelectable = currentSelection?.GetComponent<Selectable>(); // Get the Selectable component

                    Debug.Log($"[LoginManager] Enter key pressed. Current selected: {currentSelection?.name ?? "None"}");

                    // MODIFIED: Only call OnLoginButtonPressed if an InputField is currently selected
                    if (currentSelectable != null && currentSelectable is TMP_InputField)
                    {
                        Debug.Log("[LoginManager] Calling OnLoginButtonPressed() as an input field is selected.");
                        OnLoginButtonPressed();
                    }
                    else
                    {
                        Debug.Log("[LoginManager] Skipping OnLoginButtonPressed() as a non-input field (e.g., button) is selected.");
                    }
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    SelectNextField();
                }
            }
        }

        public void OnLoginButtonPressed()
        {
            string email = emailField.text.Trim();
            string password = passwordField.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                panelsManager.ShowMessage("Email and password cannot be empty.");
                return;
            }
            
            if (!AuthInputValidator.TryValidateLogin(email, password, out string reason))
            {
                panelsManager.ShowMessage(reason);
                return;
            }
            
            loginButton.interactable = false;
            panelsManager.ShowLoading();
            backendAuth.Login(email, password, result =>
            {
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
                    panelsManager.HideLoading();
                    loginButton.interactable = true;
                    
                    string friendlyMessage = BackendErrorMapper.GetFriendlyMessage(result.ErrorMessage);
                    panelsManager.ShowMessage(friendlyMessage);
                }
            });
        }
        private void SelectNextField()
        {
            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection == emailField.gameObject)
            {
                passwordField.Select();
                passwordField.ActivateInputField();
            }
            else if (currentSelection == passwordField.gameObject)
            {
                signupButton.Select();
            }
            else if (currentSelection == signupButton.gameObject)
            {
                loginButton.Select(); // Move to the sign-up button
            }
            else if (currentSelection == loginButton.gameObject)
            {
                emailField.Select(); // Loop back to the first field
                emailField.ActivateInputField();
            }
            else
            {
                // If nothing is selected, or an unexpected element, select the first field
                emailField.Select();
                emailField.ActivateInputField();
            }
        }
    }
}
