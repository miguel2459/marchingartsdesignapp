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
        if (namingPanel != null) namingPanel.SetActive(false); // Start hidden
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
    }

    private void Update()
    {
        // Auto-close if selection becomes empty
        if (namingPanel != null && namingPanel.activeSelf && selectedMarchers != null && selectedMarchers.SelectedCount == 0)
        {
            namingPanel.SetActive(false);
        }
    }

    // === Public API for external triggers ===
    public void TogglePanel()
    {
        if (namingPanel == null || selectedMarchers == null) return;

        // Only allow opening if at least one marcher is selected
        if (!namingPanel.activeSelf && selectedMarchers.SelectedCount == 0) return;

        namingPanel.SetActive(!namingPanel.activeSelf);

        if (namingPanel.activeSelf)
        {
            PopulateDropdownIfNeeded();
            customNameInput.text = "";

            int count = selectedMarchers.SelectedCount;
            if (marcherCountText != null)
                marcherCountText.text = $"Selected {count} Marcher{(count == 1 ? "" : "s")}";
        }
    }

    private void PopulateDropdownIfNeeded()
    {
        if (sectionDropdown == null) return;
        if (sectionDropdown.options.Count > 0) return;

        List<string> sections = MarcherNameAssignmentService.GetStandardSections();
        sectionDropdown.ClearOptions();
        sectionDropdown.AddOptions(sections);
    }

    private void OnConfirm()
    {
        string customName = customNameInput.text.Trim();
        bool useCustom = !string.IsNullOrEmpty(customName);

        if (selectedMarchers == null || selectedMarchers.SelectedCount == 0)
        {
            Debug.LogWarning("Naming UI: No marchers selected.");
            return;
        }

        foreach (GameObject marcher in selectedMarchers.GetSelectionCopy())
        {
            if (marcher != null && marcher.TryGetComponent(out MarcherIdentityManager identity))
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
