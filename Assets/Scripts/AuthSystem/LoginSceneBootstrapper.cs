using UnityEngine;

namespace LoginSystem
{
    /// <summary>
    /// Single composition root for the Login scene.
    /// Place this on a dedicated GameObject in the LoginScene.
    /// </summary>
    public sealed class LoginSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool useMockAuth = false;

        private void Awake()
        {
            // Ensure this runs once, deterministically, before UI controllers use AuthService.
            ServiceLocator.Initialize(this, useMock: useMockAuth, forceReinitialize: true);
        }
    }
}