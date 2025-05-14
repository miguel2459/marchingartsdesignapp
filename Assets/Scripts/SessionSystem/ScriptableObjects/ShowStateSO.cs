using UnityEngine;

[CreateAssetMenu(fileName = "ShowStateSO", menuName = "MADA/Show State")]
public class ShowStateSO : ScriptableObject
{
    public string CurrentShowID;
    public string ShowTitle;
    public string GroupName;
    public string CreatedBy;
    public string FieldType;
    public string ProductionYear;
    public int NumberOfMarchers;
    public int NumberOfSets;
    public int NumberOfProps;
    public string LastModified;
    public string ShowStatus;
    public string LastSet;
    public string JSONMarchersURL;
    public string JSONSetTimingURL;

    public void Clear()
    {
        CurrentShowID = "";
        ShowTitle = "";
        GroupName = "";
        CreatedBy = "";
        FieldType = "";
        ProductionYear = "";
        NumberOfMarchers = 0;
        NumberOfSets = 0;
        NumberOfProps = 0;
        LastModified = "";
        ShowStatus = "";
        LastSet = "";
        JSONMarchersURL = "";
        JSONSetTimingURL = "";
    }
}
