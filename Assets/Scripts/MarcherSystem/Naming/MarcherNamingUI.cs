using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI panel for assigning names to selected marchers.
/// </summary>
public class MarcherNamingUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject namingPanel;
    public TMP_Dropdown sectionDropdown;
    public TMP_InputField customNameInput;
    public TMP_Text marcherCountText; // Drag 'Number Text (TMP)' here
    public Button confirmButton;

    [Header("Dependencies")]
    public SelectedMarchers selectedMarchers;

    private void Awake()
    {
        namingPanel.SetActive(false); // Start hidden
        confirmButton.onClick.AddListener(OnConfirm);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && selectedMarchers.selectedMarchers.Count > 0)
        {
            TogglePanel();
        }

        if (namingPanel.activeSelf && selectedMarchers.selectedMarchers.Count == 0)
        {
            namingPanel.SetActive(false);
        }
    }

    private void TogglePanel()
    {
        namingPanel.SetActive(!namingPanel.activeSelf);
        if (namingPanel.activeSelf)
        {
            PopulateDropdownIfNeeded();
            customNameInput.text = "";

            int count = selectedMarchers.selectedMarchers.Count;
            marcherCountText.text = $"Selected {count} Marcher{(count == 1 ? "" : "s")}";
        }
    }

    private void PopulateDropdownIfNeeded()
    {
        if (sectionDropdown.options.Count > 0) return;

        List<string> sections = MarcherNameAssignmentService.GetStandardSections();
        sectionDropdown.ClearOptions();
        sectionDropdown.AddOptions(sections);
    }

    private void OnConfirm()
    {
        string customName = customNameInput.text.Trim();
        bool useCustom = !string.IsNullOrEmpty(customName);

        if (selectedMarchers.selectedMarchers.Count == 0)
        {
            Debug.LogWarning("Naming UI: No marchers selected.");
            return;
        }

        foreach (GameObject marcher in selectedMarchers.selectedMarchers)
        {
            if (marcher.TryGetComponent(out MarcherIdentityManager identity))
            {
                if (useCustom)
                {
                    identity.SetCustomName(customName);
                }
                else
                {
                    string section = sectionDropdown.options[sectionDropdown.value].text;
                    int number = MarcherNameAssignmentService.GetNextNumberFor(section);
                    string abbr = MarcherNameAssignmentService.GetAbbreviation(section);
                    identity.SetName(abbr, number);
                }
            }
        }

        Debug.Log("✅ Names assigned to selected marchers.");
        namingPanel.SetActive(false);
    }
}
