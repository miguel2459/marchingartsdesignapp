using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LoginSystem
{
    /// <summary>
    /// Handles the "Forgot Password" flow in the Login scene.
    /// - Uses AuthInputValidator for consistent validation/reason strings.
    /// - Routes all user-facing messages through LoginPanelsManager (single UI authority).
    /// - Prevents double-submits and keeps UI state deterministic during async callbacks.
    /// </summary>
    public sealed class ForgotPasswordManager : MonoBehaviour
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
                Debug.LogError("[ForgotPasswordManager] ServiceLocator not initialized. Ensure LoginSceneBootstrapper exists in the scene.");
                enabled = false;
                return;
            }

            authService = ServiceLocator.AuthService;

            panelsManager.HideError();
            //panelsManager.SetLoading(false);
            SetInteractable(true);
        }

        private void OnDestroy()
        {
            // Good hygiene: remove listeners to prevent duplicate invocations if this object
            // is ever disabled/enabled or re-instantiated.
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitClicked);
            if (returnToLoginButton != null) returnToLoginButton.onClick.RemoveListener(OnReturnToLoginClicked);
        }

        private void OnSubmitClicked()
        {
            if (isSubmitting)
                return;

            string email = emailInput != null ? emailInput.text.Trim() : string.Empty;

            // Full-scope shared validation (consistent reason strings)
            if (!AuthInputValidator.TryValidateEmail(email, out string reason))
            {
                panelsManager.ShowError(reason);
                return;
            }

            BeginSubmittingUI();

            // Neutral "status" message routed through the same UI pipe for consistency
            panelsManager.ShowError("Submitting password reset request...");

            authService.RequestPasswordReset(email, (success, statusOrMessage) =>
            {
                // Safety: if the scene unloaded or object disabled mid-request, do nothing.
                if (this == null || !isActiveAndEnabled)
                    return;

                EndSubmittingUI();

                if (success)
                {
                    panelsManager.ShowError("Password reset email sent. Check your inbox.");
                    return;
                }

                // statusOrMessage may be a backend code; map to friendly copy.
                string friendly = BackendErrorMapper.GetFriendlyMessage(statusOrMessage);
                panelsManager.ShowError(friendly);
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
            panelsManager.HideError();      // clear stale messages before showing status
            //panelsManager.SetLoading(true);
            SetInteractable(false);
        }

        private void EndSubmittingUI()
        {
            isSubmitting = false;
            //panelsManager.SetLoading(false);
            SetInteractable(true);
        }

        private void SetInteractable(bool interactable)
        {
            if (emailInput != null) emailInput.interactable = interactable;
            if (submitButton != null) submitButton.interactable = interactable;
            if (returnToLoginButton != null) returnToLoginButton.interactable = interactable;
        }

        private bool ValidateSerializedReferences()
        {
            bool ok = true;

            if (emailInput == null)
            {
                Debug.LogError("[ForgotPasswordManager] Missing reference: emailInput.");
                ok = false;
            }

            if (submitButton == null)
            {
                Debug.LogError("[ForgotPasswordManager] Missing reference: submitButton.");
                ok = false;
            }

            if (returnToLoginButton == null)
            {
                Debug.LogError("[ForgotPasswordManager] Missing reference: returnToLoginButton.");
                ok = false;
            }

            if (panelsManager == null)
            {
                Debug.LogError("[ForgotPasswordManager] Missing reference: panelsManager (LoginPanelsManager).");
                ok = false;
            }

            return ok;
        }
    }
}
