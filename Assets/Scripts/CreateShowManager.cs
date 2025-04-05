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
                Debug.Log("Show creation completed. Proceeding...");
                // Close panel after creation
                panelLoadingNewShow.SetActive(false);
                panelCreateNewShow.SetActive(false);

                session.AddNewShow(showTitle);
            },
            onError: (err) =>
            {
                Debug.LogError("Show creation failed: " + err);
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
