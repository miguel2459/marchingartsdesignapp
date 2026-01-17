using UnityEngine;

namespace LoginSystem
{
    public static class ServiceLocator
    {
        public static IAuthService AuthService { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static bool IsUsingMock { get; private set; }

        /// <summary>
        /// Initializes services once for the scene lifetime.
        /// If already initialized, this will no-op unless forceReinitialize is true.
        /// </summary>
        public static void Initialize(MonoBehaviour host, bool useMock = false, bool forceReinitialize = false)
        {
            if (IsInitialized && !forceReinitialize)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"✅ ServiceLocator: Already initialized as {(IsUsingMock ? "Mock" : "Backend")}. Skipping re-init.");
#endif
                return;
            }

            IsUsingMock = useMock;

            AuthService = useMock
                ? new MockAuthService()
                : new BackendAuthService(SessionManager.backendURL, host);

            IsInitialized = true;

            Debug.Log($"✅ ServiceLocator: AuthService initialized as {(useMock ? "Mock" : "Backend")}.");
        }
    }
}