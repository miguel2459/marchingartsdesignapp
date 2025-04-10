using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;

public class CreateShowManager : MonoBehaviour
{
    SessionManager session = SessionManager.instance;
    public static CreateShowManager instance; // Singleton instance

    [Header("UI Elements")]
    public GameObject panelCreateNewShow;
    public GameObject panelLoadingNewShow;
    public TMP_InputField inputShowTitle;
    public TMP_InputField inputGroupName;
    public TMP_Dropdown dropdownFieldType;
    public TMP_InputField inputNumMarchers;
    public TMP_InputField inputNumSets;
    public TMP_InputField inputNumProps;
    public Button buttonCreate;
    public Button buttonBack;

    private string showID;
    private string productionYear = System.DateTime.Now.Year.ToString();
    private string lastModified;
    private string showStatus = "Active";

    private void Awake()
    {
        // Singleton pattern to allow global access
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        buttonCreate.onClick.AddListener(OnCreateShow);
        buttonBack.onClick.AddListener(() => panelCreateNewShow.SetActive(false));
    }

    // Open the create show panel
    public void OpenCreateShowPanel()
    {
        panelCreateNewShow.SetActive(true);
    }

    private void OnCreateShow()
    {
        //Enable Loading Panel
        //Turn off Create New Show Panel

        if (ValidateInputFields())
        {
            //Enable Loading Panel
            //Disable Create New Show Panel
            panelLoadingNewShow.SetActive(true);
            // Generate unique show ID
            showID = "SH" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


            // Get current timestamp for last modified
            lastModified = System.DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

            // Gather user input
            string showTitle = inputShowTitle.text;
            string groupName = inputGroupName.text;
            string fieldType = dropdownFieldType.options[dropdownFieldType.value].text;
            int numMarchers = int.Parse(inputNumMarchers.text);
            int numSets = int.Parse(inputNumSets.text);
            int numProps = int.Parse(inputNumProps.text);
            string setOnExit = "1";
            string JSONMarching = null;
            string JSONTiming = null;
            
            Debug.Log("Attempting to create new show " + showTitle);

            // Save Show Data Locally in SessionManager
            SessionManager.instance.SaveToSessionManager(showID, showTitle, groupName, fieldType, productionYear, numMarchers, numSets, numProps, lastModified, showStatus, setOnExit, JSONMarching, JSONTiming);

            // Copy the Marching Show Template Google Sheet
            session.showDataManager.CreateNewShowTemplate(
            SessionManager.backendURL,
            showID,
            showTitle,
            groupName,
            session.userStateSO.UserEmail,
            fieldType,
            productionYear,
            numMarchers,
            numSets,
            numProps,
            lastModified,
            showStatus,
            session.userStateSO.UserFolderId,
            session.userStateSO.AccountSheetID,
            setOnExit,
            onSuccess: () =>
            {
                Debug.Log("✅ Backend show creation successful. Generating and caching default JSONs...");

                bool marcherSaveSuccess = false;
                bool timingSaveSuccess = false;

                // 1. Generate Default Marcher JSON
                string defaultMarcherJson = session.JsonService.GenerateDefaultMarcherJson(numMarchers);
                if (!string.IsNullOrEmpty(defaultMarcherJson))
                {
                    // 2. Save Default Marcher JSON to Local Cache
                    session.JsonService.SaveJson(showID, "marcher", defaultMarcherJson, success => marcherSaveSuccess = success);
                } else {
                     Debug.LogError("Failed to generate default marcher JSON.");
                }

                // 3. Generate Default Timing JSON
                string defaultTimingJson = session.JsonService.GenerateDefaultTimingJson(numSets);
                 if (!string.IsNullOrEmpty(defaultTimingJson))
                {
                    // 4. Save Default Timing JSON to Local Cache
                    session.JsonService.SaveJson(showID, "timing", defaultTimingJson, success => timingSaveSuccess = success);
                } else {
                     Debug.LogError("Failed to generate default timing JSON.");
                }

                // Check if saving defaults was successful before proceeding
                if (marcherSaveSuccess && timingSaveSuccess)
                {
                     Debug.Log("✅ Default JSONs generated and cached locally.");

                     // Close loading panel
                     panelLoadingNewShow.SetActive(false);
                     panelCreateNewShow.SetActive(false);

                     // Trigger SessionManager to refresh show list and select the new one
                     session.AddNewShow(showTitle);
                }
                else
                {
                    Debug.LogError("❌ Failed to save one or both default JSON files locally. Aborting show selection.");
                    // Handle error - maybe show a message to the user?
                    panelLoadingNewShow.SetActive(false); // Still hide loading
                    // Optionally: Add logic to inform the user the show was created backend-wise but local setup failed.
                }
            },
            onError: (err) =>
            {
                Debug.LogError($"❌ Backend show creation failed: {err}");
                // Handle error - hide loading panel, show error message to user
                panelLoadingNewShow.SetActive(false);
            });
        }
    }

    private bool ValidateInputFields()
    {
        return !(string.IsNullOrWhiteSpace(inputShowTitle.text) ||
                 string.IsNullOrWhiteSpace(inputGroupName.text) ||
                 string.IsNullOrWhiteSpace(inputNumMarchers.text) ||
                 string.IsNullOrWhiteSpace(inputNumSets.text) ||
                 string.IsNullOrWhiteSpace(inputNumProps.text));
    }
}

[System.Serializable]
public class MarcherDataWrapper
{
    public Dictionary<string, Vector3> marchers;
}
