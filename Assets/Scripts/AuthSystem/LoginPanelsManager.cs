// LoginPanelsManager.cs

using UnityEngine;
using TMPro; // Make sure this is included

namespace LoginSystem
{
    public class LoginPanelsManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject forgotPasswordPanel;
        public GameObject loginInputFields;
        public GameObject signUpInputFields;
        public GameObject loading;
        public GameObject guestPanel;
        
        public GameObject errorMessagePanel; 
        public TextMeshProUGUI errorMessageText; 

        public TextMeshProUGUI titleText;
        [SerializeField] private SignUpManager signUpManager;
        [SerializeField] private LoginManager loginManager;

        private void Start()
        {
            errorMessagePanel.SetActive(true);
            HideError();
            ShowLoginPanel();
        }

        public void ShowSignUpPanel()
        {
            forgotPasswordPanel.SetActive(false);
            loginPanel.SetActive(false);
            guestPanel.SetActive(false);
            signUpPanel.SetActive(true);
            
            HideError(); 
            titleText.text = "Sign up";
        }

        public void ShowLoginPanel()
        {
            forgotPasswordPanel.SetActive(false);
            signUpPanel.SetActive(false);
            guestPanel.SetActive(false);
            loginPanel.SetActive(true);
            
            HideError(); 
            titleText.text = "Login to";
        }
        
        public void ShowForgotPasswordPanel()
        {
            loginPanel.SetActive(false);
            signUpPanel.SetActive(false);
            forgotPasswordPanel.SetActive(true);

            HideError();
            titleText.text = "Reset password";
        }

        public void ShowGuestPanel()
        {
            loginPanel.SetActive(false);
            signUpPanel.SetActive(false);
            guestPanel.SetActive(true);
            
            HideError(); 
            titleText.text = "Guest of";
        }
        public void ShowLoading(bool isLogin)
        {
            // Always hide the error message when loading starts
            HideError(); 

            if (isLogin)
            {
                loginInputFields.SetActive(false);
                loading.SetActive(true);
            }
            else
            {
                signUpInputFields.SetActive(false);
                loading.SetActive(true);
            }
        }

        public void HideLoading(bool isLogin)
        {
            if (isLogin)
            {
                loginInputFields.SetActive(true);
                loading.SetActive(false);
            }
            else
            {
                signUpInputFields.SetActive(true);
                loading.SetActive(false);
            }
        }

        // ADDED: Centralized method to display an error message
        public void ShowError(string message)
        {
            if (errorMessageText != null)
                errorMessageText.text = message ?? "";
        }

        public void HideError()
        {
            if (errorMessageText != null)
                errorMessageText.text = "";
        }
    }
}