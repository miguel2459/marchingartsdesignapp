using UnityEngine;

[CreateAssetMenu(fileName = "UserStateSO", menuName = "MADA/User State")]
public class UserStateSO : ScriptableObject
{
    public string UserId;
    public string UserEmail;
    public string UserName;
    public string AccountSheetID;
    public string UserFolderId;

    public void Clear()
    {
        UserId = "";
        UserEmail = "";
        UserName = "";
        AccountSheetID = "";
        UserFolderId = "";
    }
}
