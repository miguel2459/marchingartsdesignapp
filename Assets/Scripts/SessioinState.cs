using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // 👈 Add this so Unity can display it
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
    public string JSONMarchersPositions;
    public string JSONSetTiming;
    public static string marcherJsonText;
    public static string timingJsonText;


    // Google Sheets Data
    public List<string> SetsData = new List<string>();
    public List<string> MarchersData = new List<string>();
    public Dictionary<string, Vector3> MarchersCoordinates = new Dictionary<string, Vector3>();

    public Dictionary<int, SetTimingData> SetTimingMap = new Dictionary<int, SetTimingData>();


    [System.Serializable]
    public class SetTimingData
    {
        public int setIndex;
        public int count;
        public float startBPM;
        public float endBPM;

        public SetTimingData(int setIndex, int count, float startBPM, float endBPM)
        {
            this.setIndex = setIndex;
            this.count = count;
            this.startBPM = startBPM;
            this.endBPM = endBPM;
        }
    }

}
