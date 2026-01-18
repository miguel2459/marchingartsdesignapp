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

        public void RequestPasswordReset(string email, System.Action<bool, string> onComplete)
        {
            Debug.Log($"[Mock] Simulating password reset for {email}");
            bool fakeSuccess = email.Contains("@"); // Simulate basic check
            if (fakeSuccess)
            {
                onComplete?.Invoke(true, "password_reset_email_sent");
            }
            else
            {
                onComplete?.Invoke(false, "account_not_found");
            }
        }
        
        public void GuestLogin(string email, System.Action<LoginResult> onComplete)
        {
            Debug.Log($"[Mock] Simulating guest login for {email}");
            onComplete?.Invoke(new LoginResult(true, null, "MockGuestUserID", "Guest", "MockGuestFolderID", "MockGuestSheetID"));
        }

    }
}