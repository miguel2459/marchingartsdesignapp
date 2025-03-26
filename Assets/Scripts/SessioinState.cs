using System.Collections.Generic;
using UnityEngine;

public class SessionState
{
    // User Info
    public string UserId;
    public string UserEmail;
    public string UserName;
    public string AccountSheetID;
    public string UserFolderId;

    // Show Info
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

    // Google Sheets Data
    public List<string> SetsData = new List<string>();
    public List<string> MarchersData = new List<string>();
    public Dictionary<string, Vector3> MarchersCoordinates = new Dictionary<string, Vector3>();
}
