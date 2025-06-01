using UnityEngine;

namespace LoginSystem
{
    public static class ServiceLocator
    {
        public static IAuthService AuthService { get; private set; }

        public static void Initialize(MonoBehaviour host, bool useMock = false)
        {
            if (useMock)
            {
                AuthService = new MockAuthService();
            }
            else
            {
                AuthService = new BackendAuthService(SessionManager.backendURL, host);
            }

            Debug.Log($"✅ ServiceLocator: AuthService initialized as {(useMock ? "Mock" : "Backend")}.");
        }
    }
}