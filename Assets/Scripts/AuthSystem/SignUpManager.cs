using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
namespace LoginSystem
{
    public class SignUpManager : MonoBehaviour
    {
        public TMP_InputField nameField;
        public TMP_InputField emailField;
        public TMP_InputField passwordField;
        public TMP_InputField confirmPasswordField;
        public LoginPanelsManager panelsManager;
        public Button backButton;
        public Button createAccountButton;

        private IAuthService backendAuth;
        private void Awake()
        {
            if (!ServiceLocator.IsInitialized)
            {
                Debug.LogError("[SignUpManager] ServiceLocator not initialized. Ensure LoginSceneBootstrapper exists in the scene.");
                ServiceLocator.Initialize(this, useMock: false);
            }

            backendAuth = ServiceLocator.AuthService;
        }
    
        void Update()
        {
            // Only process input if the sign-up panel is active and not in a loading state
            if (panelsManager != null && panelsManager.signUpPanel.activeInHierarchy && !panelsManager.loadingPanel.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
                    Selectable currentSelectable = currentSelection?.GetComponent<Selectable>(); // Get the Selectable component

                    Debug.Log($"[SignUpManager] Enter key pressed. Current selected: {currentSelection?.name ?? "None"}");

                    // MODIFIED: Only call OnCreateAccountButtonPressed if an InputField is currently selected
                    if (currentSelectable != null && currentSelectable is TMP_InputField)
                    {
                        Debug.Log("[SignUpManager] Calling OnCreateAccountButtonPressed() as an input field is selected.");
                        OnCreateAccountButtonPressed();
                    }
                    else
                    {
                        Debug.Log("[SignUpManager] Skipping OnCreateAccountButtonPressed() as a non-input field (e.g., button) is selected.");
                    }
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    SelectNextField();
                }
            }
        }
        
        public void OnCreateAccountButtonPressed()
        {
            string name = nameField.text.Trim();
            string email = emailField.text.Trim();
            string password = passwordField.text.Trim();
            string confirmPassword = confirmPasswordField.text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                panelsManager.ShowMessage("All fields must be filled.");
                return;
            }
            
            if (!AuthInputValidator.TryValidateSignUp(name, email, password, confirmPassword, out string reason))
            {
                panelsManager.ShowMessage(reason);
                return;
            }

            createAccountButton.interactable = false;
            panelsManager.ShowLoading();
            backendAuth.SignUp(name, email, password, result =>
            {
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
                    panelsManager.HideLoading();
                    createAccountButton.interactable = true;
                    string friendlyMessage = BackendErrorMapper.GetFriendlyMessage(result.ErrorMessage);
                    panelsManager.ShowMessage(friendlyMessage);
                }
            });
        }
        private void SelectNextField()
        {
            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

            if (currentSelection == nameField.gameObject)
            {
                emailField.Select();
                emailField.ActivateInputField();
            }
            else if (currentSelection == emailField.gameObject)
            {
                passwordField.Select();
                passwordField.ActivateInputField();
            }
            else if (currentSelection == passwordField.gameObject)
            {
                confirmPasswordField.Select(); // Move to Confirm Password field
                confirmPasswordField.ActivateInputField();
            }
            else if (currentSelection == confirmPasswordField.gameObject)
            {
                backButton.Select(); // Move to Back button
            }
            else if (currentSelection == backButton.gameObject)
            {
                createAccountButton.Select(); // Move to Create Account button
            }
            else if (currentSelection == createAccountButton.gameObject)
            {
                nameField.Select(); // Loop back to the first field
                nameField.ActivateInputField();
            }
            else
            {
                // If nothing is selected, or an unexpected element, select the first field
                nameField.Select();
                nameField.ActivateInputField();
            }
        }
    }
}
