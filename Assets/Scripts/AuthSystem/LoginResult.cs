namespace LoginSystem
{
    public struct LoginResult
    {
        public bool Success;
        public string ErrorMessage;
        public string UserId;
        public string UserName;
        public string FolderId;
        public string UserSheetID;

        public LoginResult(bool success, string errorMessage, string userId = null, string userName = null, string folderId = null, string userSheetID = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            UserId = userId;
            UserName = userName;
            FolderId = folderId;
            UserSheetID = userSheetID;
        }
    }
}