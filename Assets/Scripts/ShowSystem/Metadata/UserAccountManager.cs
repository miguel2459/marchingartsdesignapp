using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UserAccountManager : MonoBehaviour
{
    SessionManager session = SessionManager.instance;
    
    [Header("UI References")]
    public Button showSelectionButton;
    public Button logoutButton;
    public Button saveShow;
    public TMP_Text userNameText;
    public TMP_Text showTitleText;
    public TMP_Text groupNameText;
    public TMP_Text lastSaveDate;
    public TMP_Text lastSaveTime;

    private void Start()
    {
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
        showSelectionButton?.onClick.AddListener(GoToShowSelection);
        saveShow?.onClick.AddListener(OnSaveShowClicked);
        logoutButton?.onClick.AddListener(LogoutUser);

        // Populate static UI text
        if (userNameText != null)
            userNameText.text = session.userStateSO.UserName;

        if (showTitleText != null)
            showTitleText.text = session.showStateSO.ShowTitle;

        if (groupNameText != null)
            groupNameText.text = session.showStateSO.GroupName;

        UpdateLastSaveDisplay();

        Debug.Log("✅ Account UI Initialized");
    }

    private void OnSaveShowClicked()
    {
        string date = DateTime.Now.ToString("MM/dd");
        string time = DateTime.Now.ToString("HH:mm");

        if (lastSaveDate != null) lastSaveDate.text = date;
        if (lastSaveTime != null) lastSaveTime.text = time;

        session.showStateSO.LastModified = $"{date}, {time}";
        session.SaveShow();

        Debug.Log("💾 Save triggered and UI updated.");
    }

    private void UpdateLastSaveDisplay()
    {
        string modified = session.showStateSO.LastModified;

        if (DateTime.TryParse(modified, out DateTime parsedDate))
        {
            if (lastSaveDate != null) lastSaveDate.text = parsedDate.ToString("MM/dd");
            if (lastSaveTime != null) lastSaveTime.text = parsedDate.ToString("HH:mm");
        }
        else if (!string.IsNullOrEmpty(modified))
        {
            // fallback in case of unparseable value
            string[] parts = modified.Split(',');
            if (parts.Length == 2)
            {
                if (lastSaveDate != null) lastSaveDate.text = parts[0].Trim();
                if (lastSaveTime != null) lastSaveTime.text = parts[1].Trim();
            }
        }
    }

    private void GoToShowSelection()
    {
        showSelectionButton.enabled = false;
        SessionManager.instance.ExitShow(); // save and switch scene
    }

    private void LogoutUser()
    {
        SessionManager.instance.StartLogout();
    }

    private void OnDestroy()
    {
        showSelectionButton?.onClick.RemoveListener(GoToShowSelection);
        saveShow?.onClick.RemoveListener(OnSaveShowClicked);
        logoutButton?.onClick.RemoveListener(LogoutUser);
    }
}
