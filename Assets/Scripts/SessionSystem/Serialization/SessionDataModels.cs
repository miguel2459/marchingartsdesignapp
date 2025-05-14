using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserStateData {
    public string UserId, UserEmail, UserName, AccountSheetID, UserFolderId;
}

[System.Serializable]
public class ShowStateData {
    public string CurrentShowID, ShowTitle, GroupName, CreatedBy, FieldType, ProductionYear;
    public int NumberOfMarchers, NumberOfSets, NumberOfProps;
    public string LastModified, ShowStatus, LastSet, JSONMarchersURL, JSONSetTimingURL;
}

[System.Serializable]
public class RuntimeCacheData {
    public string CachedMarcherJSON, CachedTimingJSON;
    public Dictionary<string, Vector3> MarchersCoordinates = new Dictionary<string, Vector3>();
    public Dictionary<int, RuntimeCacheSO.SetTimingData> SetTimingMap = new Dictionary<int, RuntimeCacheSO.SetTimingData>();
}

[System.Serializable]
public class FullSessionState {
    public UserStateData user;
    public ShowStateData show;
    public RuntimeCacheData cache;
}
