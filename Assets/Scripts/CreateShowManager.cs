using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;

public class CreateShowManager : MonoBehaviour
{
    public static CreateShowManager instance; // Singleton instance

    [Header("UI Elements")]
    public GameObject panelCreateNewShow;
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
        if (ValidateInputFields())
        {
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


            // Save Show Data Locally in SessionManager
            SessionManager.instance.SaveToSessionManager(showID, showTitle, groupName, fieldType, productionYear, numMarchers, numSets, numProps, lastModified, showStatus, setOnExit);

            // Copy the Marching Show Template Google Sheet
            StartCoroutine(SessionManager.instance.NewShowTemplateToGoogleDrive(showID, showTitle, groupName, fieldType, productionYear, numMarchers, numSets, numProps, lastModified, showStatus, setOnExit));

            // Populate Set and Marcher IDs
            SaveSetsData(numSets);
            SaveMarchersData(numMarchers);

            // Save Marcher Coordinates JSON
            SaveMarchersCoordinatesJson(showID, numMarchers);

            // Close panel after creation
            panelCreateNewShow.SetActive(false);
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

    private void SaveSetsData(int numSets)
    {
        List<string> setsData = new List<string>();
        for (int i = 0; i <= numSets; i++)
        {
            setsData.Add(i.ToString());
        }
        SessionManager.instance.setsData = setsData;
    }

    private void SaveMarchersData(int numMarchers)
    {
        List<string> marchersData = new List<string>();
        for (int i = 1; i <= numMarchers; i++)
        {
            marchersData.Add("M" + i);
        }
        SessionManager.instance.marchersData = marchersData;
    }

    private void SaveMarchersCoordinatesJson(string showID, int numMarchers)
    {
        Dictionary<string, Vector3> marchersCoordinates = new Dictionary<string, Vector3>();

        for (int i = 1; i <= numMarchers; i++)
        {
            marchersCoordinates["M" + i] = Vector3.zero;
        }

        string json = JsonUtility.ToJson(new MarcherDataWrapper { marchers = marchersCoordinates });
        string filePath = Application.persistentDataPath + "/" + showID + "_Marchers.json";
        File.WriteAllText(filePath, json);

        Debug.Log("Marchers JSON saved at: " + filePath);
    }
}

[System.Serializable]
public class MarcherDataWrapper
{
    public Dictionary<string, Vector3> marchers;
}
