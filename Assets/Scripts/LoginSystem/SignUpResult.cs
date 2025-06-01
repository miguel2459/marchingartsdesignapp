namespace LoginSystem
{
    public struct SignUpResult
    {
        public bool Success;
        public string ErrorMessage;
        public string UserId;
        public string FolderId;
        public string UserSheetID;

        public SignUpResult(bool success, string errorMessage, string userId = null, string folderId = null, string userSheetID = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
            UserId = userId;
            FolderId = folderId;
            UserSheetID = userSheetID;
        }
    }
}