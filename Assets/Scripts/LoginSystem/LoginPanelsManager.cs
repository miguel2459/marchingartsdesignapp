// LoginPanelsManager.cs

using UnityEngine;
using TMPro; // Make sure this is included

namespace LoginSystem
{
    public class LoginPanelsManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject loginInputFields;
        public GameObject signUpInputFields;
        public GameObject loading;
        
        public GameObject errorMessagePanel; 
        public TextMeshProUGUI errorMessageText; 

        public TextMeshProUGUI titleText;
        [SerializeField] private SignUpManager signUpManager;
        [SerializeField] private LoginManager loginManager;

        private void Start()
        {
            ShowLoginPanel();
        }

        public void ShowSignUpPanel()
        {
            loginPanel.SetActive(false);
            signUpPanel.SetActive(true);
            HideError(); // Call centralized HideError
            titleText.text = "Sign up";
        }

        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            HideError(); // Call centralized HideError
            signUpPanel.SetActive(false);
            titleText.text = "Login to";
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
            {
                errorMessageText.text = message;
            }
        }

        // ADDED: Centralized method to hide the error message
        public void HideError()
        {
            //Optionally clear the text when hidden, though not strictly necessary
            if (errorMessageText != null) 
            {
                errorMessageText.text = "";
            }
        }
    }
}