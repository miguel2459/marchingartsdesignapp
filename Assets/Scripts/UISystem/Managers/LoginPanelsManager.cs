using UnityEngine;
using TMPro;

public class LoginPanelsManager : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject signUpPanel;
    public GameObject loginInputFields;
    public GameObject signUpInputFields;
    public GameObject loginLoading;
    public GameObject signUpLoading;

    private void Start()
    {
        // Ensure the scene starts with the Login Panel visible
        ShowLoginPanel();
    }

    public void ShowSignUpPanel()
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(true);
    }

    public void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        signUpPanel.SetActive(false);
    }

    public void ShowLoading(bool isLogin)
    {
        if (isLogin)
        {
            loginInputFields.SetActive(false);
            loginLoading.SetActive(true);
        }
        else
        {
            signUpInputFields.SetActive(false);
            signUpLoading.SetActive(true);
        }
    }

    public void HideLoading(bool isLogin)
    {
        if (isLogin)
        {
            loginInputFields.SetActive(true);
            loginLoading.SetActive(false);
        }
        else
        {
            signUpInputFields.SetActive(true);
            signUpLoading.SetActive(false);
        }
    }
}
