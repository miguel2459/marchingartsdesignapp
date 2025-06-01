using UnityEngine;

namespace LoginSystem
{
    public class MockAuthService : IAuthService
    {
        public void Login(string email, string password, System.Action<LoginResult> onComplete)
        {
            Debug.Log($"[Mock] Simulating login for {email}");
            onComplete?.Invoke(new LoginResult(true, null, "MockUserID", "MockUser", "MockFolderID", "MockSheetID"));
        }

        public void SignUp(string name, string email, string password, System.Action<SignUpResult> onComplete)
        {
            Debug.Log($"[Mock] Simulating signup for {email}");
            onComplete?.Invoke(new SignUpResult(true, null, "MockUserID", "MockFolderID", "MockSheetID"));
        }
    }
}