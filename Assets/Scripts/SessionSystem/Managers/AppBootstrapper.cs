using UnityEngine;
using System.Collections;


public class AppBootstrapper : MonoBehaviour
{
    [SerializeField] private ConfigLoader configLoader;

    private IEnumerator Start()
    {
        Debug.Log("🔧 AppBootstrapper: Starting early config fetch...");

        if (configLoader != null)
        {
            yield return StartCoroutine(configLoader.LoadAndStore());

            // Wait for validation to complete
            yield return new WaitUntil(() => SessionStateValidator.HasValidationRun);

            Debug.Log("✅ AppBootstrapper: Config & SessionStateValidator finished.");

            SessionManager.instance.InitializeSessionState();

            if (SessionStateValidator.WasSessionValid)
            {
                Debug.Log("🔁 AppBootstrapper: Session is valid. Attempting AutoLogin...");
                SessionManager.instance.AutoLogin();
                // Wait for user shows to finish loading before switching
                yield return new WaitUntil(() => SceneController.instance.isSessionInitialized);
                SceneController.instance.SwitchScene(3); // Show Selection Scene
            }
            else
            {
                Debug.Log("🚪 AppBootstrapper: No session found. Awaiting login scene.");
                SceneController.instance.LoadSceneAdditive(1); // Startup Scene
            }
        }
        else
        {
            Debug.LogWarning("⚠️ AppBootstrapper: ConfigLoader not assigned.");
        }
    }
}