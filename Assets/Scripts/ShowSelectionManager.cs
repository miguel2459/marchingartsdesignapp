using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform scrollContent; // Parent container for show panels
    public GameObject panelCreateNewShowPrefab; // Prefab for creating a new show
    public GameObject panelSavedShowPrefab; // Prefab for saved shows

    private void Start()
    {
        StartCoroutine(WaitForSessionAndPopulate());
    }

    private IEnumerator WaitForSessionAndPopulate()
    {
        // Wait until the SessionManager has loaded user data
        while (SessionManager.instance == null || SessionManager.instance.savedShows == null)
        {
            Debug.Log("⏳ Waiting for user session to initialize...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("✅ User session detected, populating show selection.");
        PopulateShowSelection();
    }

    private void PopulateShowSelection()
    {
        // Clear existing children (if reloading)
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        List<SessionManager.ShowData> savedShows = SessionManager.instance.savedShows;

        if (savedShows.Count > 0)
        {
            // Populate saved shows dynamically
            foreach (SessionManager.ShowData show in savedShows)
            {
                GameObject savedShowPanel = Instantiate(panelSavedShowPrefab, scrollContent);
                ShowPanelUI panelUI = savedShowPanel.GetComponent<ShowPanelUI>();

                if (panelUI != null)
                {
                    panelUI.SetShowData(show);
                    panelUI.onShowSelected += OnShowSelected;
                }
            }
        }

        // Always add the "Create New Show" button at the end
        GameObject createNewShowPanel = Instantiate(panelCreateNewShowPrefab, scrollContent);
        createNewShowPanel.GetComponent<Button>().onClick.AddListener(() => CreateShowManager.instance.OpenCreateShowPanel());
    }

    private void OnShowSelected(SessionManager.ShowData show)
    {
        Debug.Log($"Selected Show: {show.showTitle}");
        // Handle loading the show details in a separate UI panel
    }
}
