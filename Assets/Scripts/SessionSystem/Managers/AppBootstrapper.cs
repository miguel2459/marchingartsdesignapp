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
            Debug.Log("✅ Config ready. Now triggering SessionManager.Initialize().");

            SessionManager.instance.InitializeSessionState();
        }
        else
        {
            Debug.LogWarning("⚠️ ConfigLoader not assigned to AppBootstrapper.");
        }
            
    }
}