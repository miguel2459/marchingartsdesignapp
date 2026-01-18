using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapSceneLoader : MonoBehaviour
{
    public float minDisplayTime = 1.0f; // Prevent flash if scene loads fast
    
    private void Start()
    {
        StartCoroutine(LoadMainSceneAndWaitForExit());
    }

    private IEnumerator LoadMainSceneAndWaitForExit()
    {
        float timer = 0f;
        AsyncOperation loadMain = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive); // MainScene
        yield return loadMain;

        // Wait for ShowSelectionScene or StartupScene to finish loading
        while (!SceneManager.GetSceneByBuildIndex(3).isLoaded &&
               !SceneManager.GetSceneByBuildIndex(2).isLoaded)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure the loader stayed on screen for at least X seconds
        yield return new WaitForSeconds(Mathf.Max(0, minDisplayTime - timer));

        // TODO: Optionally check if LoginScene is visible before unloading
        SceneManager.UnloadSceneAsync(0); // BootstrapScene
    }
}