using TMPro;
using UnityEngine;

/// <summary>
/// Stores and manages the identity of a marcher, including display name and label updates.
/// </summary>
[System.Serializable]
public class MarcherIdentity
{
    public string section;
    public string abbr;
    public int number;
}

public class MarcherIdentityManager : MonoBehaviour
{
    [Header("Name Settings")]
    [SerializeField] private MarcherIdentity identity = new MarcherIdentity();

    [Header("Label Reference")]
    public TextMeshProUGUI nameLabel;

    private void Start()
    {
        UpdateLabel();
    }

    public void SetName(string abbr, int number)
    {
        identity.abbr = abbr;
        identity.number = number;
        identity.section = MarcherNameAssignmentService.GetSectionByAbbreviation(abbr);
        UpdateLabel();
    }

    public void SetCustomName(string custom)
    {
        identity.abbr = custom;
        identity.number = 0;
        identity.section = custom;
        UpdateLabel();
    }

    public string GetFullName()
    {
        return identity.number > 0 ? $"{identity.abbr}{identity.number}" : identity.abbr;
    }

    private void UpdateLabel()
    {
        if (nameLabel != null)
            nameLabel.text = GetFullName();
    }

    public MarcherIdentity GetIdentity() => identity;

    public void LoadIdentity(string section, string abbr, int number)
    {
        identity.section = section;
        identity.abbr = abbr;
        identity.number = number;
        UpdateLabel();
    }
}
