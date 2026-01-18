// GuestManager.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoginSystem
{
    /// <summary>
    /// Handles the "Continue as Guest" flow:
    /// - Validates email via AuthInputValidator
    /// - Submits to backend via IAuthService.GuestLogin(...)
    /// - Saves session and proceeds like normal Login/SignUp
    /// </summary>
    public sealed class GuestManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button returnToLoginButton;

        [Header("UI Controller")]
        [SerializeField] private LoginPanelsManager panelsManager;

        private IAuthService authService;
        private bool isSubmitting;

        private void Awake()
        {
            if (!ValidateSerializedReferences())
            {
                enabled = false;
                return;
            }

            submitButton.onClick.AddListener(OnSubmitClicked);
            returnToLoginButton.onClick.AddListener(OnReturnToLoginClicked);

            if (!ServiceLocator.IsInitialized)
            {
                Debug.LogError("[GuestManager] ServiceLocator not initialized. Ensure LoginSceneBootstrapper exists in the scene.");
                enabled = false;
                return;
            }

            authService = ServiceLocator.AuthService;

            panelsManager.HideError();
            SetInteractable(true);
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitClicked);
            if (returnToLoginButton != null) returnToLoginButton.onClick.RemoveListener(OnReturnToLoginClicked);
        }

        private void Update()
        {
            // Mirror the "Enter-to-submit when an input is selected" behavior.
            if (panelsManager == null || panelsManager.guestPanel == null)
                return;

            if (!panelsManager.guestPanel.activeInHierarchy)
                return;

            if (panelsManager.loading != null && panelsManager.loading.activeInHierarchy)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var currentSelection = EventSystem.current != null
                    ? EventSystem.current.currentSelectedGameObject
                    : null;

                var selectable = currentSelection != null ? currentSelection.GetComponent<Selectable>() : null;

                // Only submit if an InputField is selected (prevents double-submits when a button is focused).
                if (selectable is TMP_InputField)
                    OnSubmitClicked();
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SelectNextField();
            }
        }

        private void OnSubmitClicked()
        {
            if (isSubmitting)
                return;

            string email = emailInput != null ? emailInput.text.Trim() : string.Empty;

            if (!AuthInputValidator.TryValidateEmail(email, out string reason))
            {
                panelsManager.ShowError(reason);
                return;
            }

            BeginSubmittingUI();
            panelsManager.ShowError("Signing in as guest...");

            // NOTE: Requires adding to IAuthService + BackendAuthService:
            // void GuestLogin(string email, Action<LoginResult> onComplete);
            authService.GuestLogin(email, result =>
            {
                if (this == null || !isActiveAndEnabled)
                    return;
            
                EndSubmittingUI();
            
                if (result.Success)
                {
                    Debug.Log($"✅ Guest Login Successful: {result.UserName} ({result.UserId})");
                    SessionManager.instance.userSession.SaveSession(
                        new UserData(result.UserId, email, result.UserName, result.UserSheetID, result.FolderId)
                    );
            
                    StartCoroutine(SceneController.instance.WaitForSessionInitialization());
                }
                else
                {
                    string friendlyMessage = BackendErrorMapper.GetFriendlyMessage(result.ErrorMessage);
                    panelsManager.ShowError(friendlyMessage);
                }
            });
        }

        private void OnReturnToLoginClicked()
        {
            if (isSubmitting)
                return;

            panelsManager.HideError();

            if (emailInput != null)
                emailInput.text = string.Empty;

            panelsManager.ShowLoginPanel();
        }

        private void BeginSubmittingUI()
        {
            isSubmitting = true;
            panelsManager.HideError();
            SetInteractable(false);
        }

        private void EndSubmittingUI()
        {
            isSubmitting = false;
            SetInteractable(true);
        }

        private void SetInteractable(bool interactable)
        {
            if (emailInput != null) emailInput.interactable = interactable;
            if (submitButton != null) submitButton.interactable = interactable;
            if (returnToLoginButton != null) returnToLoginButton.interactable = interactable;
        }

        private void SelectNextField()
        {
            var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            if (current == emailInput.gameObject)
            {
                submitButton.Select();
            }
            else if (current == submitButton.gameObject)
            {
                returnToLoginButton.Select();
            }
            else if (current == returnToLoginButton.gameObject)
            {
                emailInput.Select();
                emailInput.ActivateInputField();
            }
            else
            {
                emailInput.Select();
                emailInput.ActivateInputField();
            }
        }

        private bool ValidateSerializedReferences()
        {
            bool ok = true;

            if (emailInput == null)
            {
                Debug.LogError("[GuestManager] Missing reference: emailInput.");
                ok = false;
            }

            if (submitButton == null)
            {
                Debug.LogError("[GuestManager] Missing reference: submitButton.");
                ok = false;
            }

            if (returnToLoginButton == null)
            {
                Debug.LogError("[GuestManager] Missing reference: returnToLoginButton.");
                ok = false;
            }

            if (panelsManager == null)
            {
                Debug.LogError("[GuestManager] Missing reference: panelsManager (LoginPanelsManager).");
                ok = false;
            }

            return ok;
        }
    }
}
