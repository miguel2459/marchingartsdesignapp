using UnityEngine;
using UnityEngine.UI;

public class UserAccountManager : MonoBehaviour
{
    [Header("UI References")]
    public Button showSelectionButton;
    public Button logoutButton;
    public Text userNameText;
    public Button accountButton;
    public GameObject accountPanel;

    private void Start()
    {
        // Ensure dependencies are assigned
        if (SessionManager.instance == null)
        {
            Debug.LogError("❌ SessionManager instance is missing!");
            return;
        }

        if (SceneController.instance == null)
        {
            Debug.LogError("❌ SceneController instance is missing!");
            return;
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        // 🔹 Ensure UI elements exist before assigning listeners
        if (showSelectionButton != null)
            showSelectionButton.onClick.AddListener(GoToShowSelection);

        if (logoutButton != null)
            logoutButton.onClick.AddListener(LogoutUser);

        if (accountButton != null)
            accountButton.onClick.AddListener(ToggleAccountPanel);

        if (userNameText != null)
            userNameText.text = SessionManager.instance.userName;
        else
            Debug.LogWarning("⚠ UserName Text UI is not assigned.");
    }

    private void GoToShowSelection()
    {
        SessionManager.instance.ExitShow();
        SceneController.instance.SwitchScene(3);
    }

    private void LogoutUser()
    {
        SessionManager.instance.Logout();
    }

    private void ToggleAccountPanel()
    {
        if (accountPanel == null)
        {
            Debug.LogWarning("⚠ Account panel is not assigned.");
            return;
        }

        accountPanel.SetActive(!accountPanel.activeSelf);
    }

    private void OnDestroy()
    {
        // 🔹 Remove listeners to prevent memory leaks
        if (showSelectionButton != null)
            showSelectionButton.onClick.RemoveListener(GoToShowSelection);

        if (logoutButton != null)
            logoutButton.onClick.RemoveListener(LogoutUser);

        if (accountButton != null)
            accountButton.onClick.RemoveListener(ToggleAccountPanel);
    }
}
