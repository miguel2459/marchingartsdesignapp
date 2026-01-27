using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

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
    public TMP_Text loadingStatusText;

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
    
    public void OpenCreateShowPanel()
    {
        panelCreateNewShow.SetActive(true);
    }

    private void OnCreateShow()
    {
        if (ValidateInputFields())
        {
            if (loadingStatusText != null) loadingStatusText.text = "Creating New Show...";
            panelLoadingNewShow.SetActive(true);
            
            showID = "SH" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
                StartCoroutine(SaveDefaultsAndContinue(showID, showTitle, numMarchers, numSets));
            },
            onError: (err) =>
            {
                Debug.LogError($"❌ Backend show creation failed: {err}");
                // Handle error - hide loading panel, show error message to user
                panelLoadingNewShow.SetActive(false);
            });
        }
    }

    private IEnumerator SaveDefaultsAndContinue(string showID, string showTitle, int numMarchers, int numSets)
    {
        string defaultMarcherJson = session.JsonService.GenerateDefaultMarcherJson(numMarchers);
        string defaultTimingJson = session.JsonService.GenerateDefaultTimingJson(numSets);

        if (string.IsNullOrEmpty(defaultMarcherJson) || string.IsNullOrEmpty(defaultTimingJson))
        {
            Debug.LogError("❌ Failed to generate default JSON.");
            panelLoadingNewShow.SetActive(false);
            yield break;
        }

        bool marcherSaveSuccess = false;
        bool timingSaveSuccess = false;

        bool marcherDone = false;
        bool timingDone = false;

        session.JsonService.SaveJson(showID, "marcher", defaultMarcherJson, success => {
            marcherSaveSuccess = success;
            marcherDone = true;
        });

        session.JsonService.SaveJson(showID, "timing", defaultTimingJson, success => {
            timingSaveSuccess = success;
            timingDone = true;
        });

        // Wait until both operations complete
        yield return new WaitUntil(() => marcherDone && timingDone);


        // Wait for both to complete (cheap way)
        float timeout = 5f;
        float elapsed = 0f;
        while ((!marcherSaveSuccess || !timingSaveSuccess) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (marcherSaveSuccess && timingSaveSuccess)
        {
            Debug.Log("✅ Default JSONs generated and cached locally.");
            panelLoadingNewShow.SetActive(false);
            panelCreateNewShow.SetActive(false);
            session.AddNewShow(showTitle);
            //SceneController.instance.SwitchScene(5); // Load ShowManagerScene directly
        }
        else
        {
            Debug.LogError("❌ Failed to save one or both default JSON files locally. Aborting show selection.");
            panelLoadingNewShow.SetActive(false);
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
