using UnityEngine;
using TMPro;
namespace LoginSystem
{
    public class LoginPanelsManager : MonoBehaviour
    {
        public GameObject loginPanel;
        public GameObject signUpPanel;
        public GameObject loginInputFields;
        public GameObject signUpInputFields;
        public GameObject loading;
        public GameObject error; 
        public TextMeshProUGUI titleText;
        [SerializeField] private SignUpManager signUpManager;
        [SerializeField] private LoginManager loginManager;

        private void Start()
        {
            // Ensure the scene starts with the Login Panel visible
            ShowLoginPanel();
        }

        public void ShowSignUpPanel()
        {
            loginPanel.SetActive(false);
            signUpPanel.SetActive(true);
            signUpManager.HideError();
            titleText.text = "Sign up";
        }

        public void ShowLoginPanel()
        {
            loginPanel.SetActive(true);
            loginManager.HideError();
            signUpPanel.SetActive(false);
            titleText.text = "Login to";
        }

        public void ShowLoading(bool isLogin)
        {
            if (isLogin)
            {
                loginInputFields.SetActive(false);
                error.SetActive(false);
                loading.SetActive(true);
            }
            else
            {
                signUpInputFields.SetActive(false);
                error.SetActive(false);
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
    }
}