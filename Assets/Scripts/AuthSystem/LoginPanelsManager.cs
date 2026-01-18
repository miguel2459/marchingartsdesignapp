using UnityEngine;
using TMPro;

namespace LoginSystem
{
    public class LoginPanelsManager : MonoBehaviour
    {
        public enum AuthUIState
        {
            Login,
            SignUp,
            ForgotPassword,
            Guest,
            Loading
        }
        
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject forgotPasswordPanel;
        public GameObject guestPanel;
        public GameObject loadingPanel;
        public GameObject messagePanel; 
        
        public TextMeshProUGUI messageText; 
        public TextMeshProUGUI titleText;
        
        public AuthUIState CurrentState { get; private set; } = AuthUIState.Login;
        
        private void Start()
        {
            messagePanel.SetActive(true);
            SetState(AuthUIState.Login);
        }
        
        public void SetState(AuthUIState state)
        {
            if (state == AuthUIState.Loading)
            {
                // Use ShowLoadingPanel(...) instead so flow is explicit.
                ShowLoadingPanel();
                return;
            }

            CurrentState = state;

            ApplyPanelsForState(state);
            ApplyTitleForState(state);

            // Generally: whenever switching panels, clear stale error copy.
            HideMessage();
        }
        
        public void ShowLoadingPanel()
        {
            ApplyPanelsForState(AuthUIState.Loading);
            ApplyMessageForLoadingFlow(CurrentState);
        }
        
        private void ApplyPanelsForState(AuthUIState state)
        {
            // Hard-off everything first (deterministic)
            SetActiveSafe(loginPanel, false);
            SetActiveSafe(signUpPanel, false);
            SetActiveSafe(forgotPasswordPanel, false);
            SetActiveSafe(guestPanel, false);
            SetActiveSafe(loadingPanel, false);

            switch (state)
            {
                case AuthUIState.Login:
                    SetActiveSafe(loginPanel, true);
                    break;

                case AuthUIState.SignUp:
                    SetActiveSafe(signUpPanel, true);
                    break;

                case AuthUIState.ForgotPassword:
                    SetActiveSafe(forgotPasswordPanel, true);
                    break;

                case AuthUIState.Guest:
                    SetActiveSafe(guestPanel, true);
                    break;

                case AuthUIState.Loading:
                    SetActiveSafe(loadingPanel, true);
                    break;
            }
        }
        
        private void ApplyTitleForState(AuthUIState state)
        {
            if (titleText == null) return;

            switch (state)
            {
                case AuthUIState.Login:
                    titleText.text = "Login to";
                    break;
                case AuthUIState.SignUp:
                    titleText.text = "Sign up";
                    break;
                case AuthUIState.ForgotPassword:
                    titleText.text = "Reset password";
                    break;
                case AuthUIState.Guest:
                    titleText.text = "Guest of";
                    break;
                case AuthUIState.Loading:
                    // Title for loading is flow-specific; handled elsewhere.
                    break;
            }
        }
        
        public void ApplyMessageForLoadingFlow(AuthUIState state)
        {
            if (messageText == null) return;

            switch (state)
            {
                case AuthUIState.Login:
                    messageText.text = "Logging in...";
                    break;
                case AuthUIState.SignUp:
                    messageText.text = "Creating account...";
                    break;
                case AuthUIState.ForgotPassword:
                    messageText.text = "Sending reset link...";
                    break;
                case AuthUIState.Guest:
                    messageText.text = "Signing in as guest...";
                    break;
            }
        }
        
        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        public void ShowSignUpPanel() => SetState(AuthUIState.SignUp);

        public void ShowLoginPanel() => SetState(AuthUIState.Login);
        
        public void ShowForgotPasswordPanel() => SetState(AuthUIState.ForgotPassword);

        public void ShowGuestPanel() => SetState(AuthUIState.Guest);
        public void ShowLoading() => SetState(AuthUIState.Loading);

        public void HideLoading()
        {
            // Return to the last non-loading state.
            SetState(CurrentState);
        }
        
        public void ShowMessage(string message)
        {
            if (messageText != null)
                messageText.text = message ?? "";
        }

        public void HideMessage()
        {
            if (messageText != null)
                messageText.text = "";
        }
    }
}