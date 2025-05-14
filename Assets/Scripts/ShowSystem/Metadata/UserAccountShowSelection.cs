using UnityEngine;
using UnityEngine.UI;
public class UserAccountShowSelection : MonoBehaviour
{
    [Header("UI References")]
    public GameObject accountPanel;
    public Button accountButton;
    public Button logoutButton;
    public Text usernameText;

    private void Start()
    {
        // Hide the panel initially
        if (accountPanel != null)
            accountPanel.SetActive(false);

        // Assign UI button listeners
        if (accountButton != null)
            accountButton.onClick.AddListener(ToggleAccountPanel);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(LogoutUser);

        // Populate username
        if (usernameText != null && SessionManager.instance != null)
        {
            usernameText.text = SessionManager.instance.userStateSO.UserName;
        }
    }

    private void ToggleAccountPanel()
    {
        if (accountPanel != null)
        {
            accountPanel.SetActive(!accountPanel.activeSelf);
        }
    }

    private void LogoutUser()
    {
        Debug.Log("🔒 Logging out from Show Selection screen...");
        SessionManager.instance.Logout();
    }

    private void OnDestroy()
    {
        if (accountButton != null)
            accountButton.onClick.RemoveListener(ToggleAccountPanel);

        if (logoutButton != null)
            logoutButton.onClick.RemoveListener(LogoutUser);
    }
}
