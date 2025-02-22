using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    // Define scene build indices for clarity
    private const int MAIN_SCENE = 0;
    private const int STARTUP_SCENE = 1;
    private const int LOGIN_SCENE = 2;
    private const int SHOW_SELECTION_SCENE = 3;
    private const int SHOW_MANAGER_SCENE = 4;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep SceneController alive across all scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // If StartupScene is not already loaded, load it
        if (!IsSceneCurrentlyLoaded(STARTUP_SCENE))
        {
            LoadSceneAdditive(STARTUP_SCENE);
        }
    }

    public void LoadSceneAdditive(int sceneIndex)
    {
        if (!SceneExists(sceneIndex)) return;
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
    }

    private bool IsSceneCurrentlyLoaded(int sceneIndex)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (loadedScene.buildIndex == sceneIndex)
            {
                return true; // Scene is already loaded
            }
        }
        return false;
    }

    public void UnloadScene(int sceneIndex)
    {
        if (!SceneExists(sceneIndex)) return;
        SceneManager.UnloadSceneAsync(sceneIndex);
    }

    public void SwitchScene(int newSceneIndex)
    {
        if (!SceneExists(newSceneIndex)) return;
        StartCoroutine(SwitchSceneRoutine(newSceneIndex));
    }

    private IEnumerator SwitchSceneRoutine(int newSceneIndex)
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.FadeToBlack(); // Optional transition effect
        }

        yield return new WaitForSeconds(1f);

        // Unload all scenes except the main scene
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != MAIN_SCENE)
            {
                SceneManager.UnloadSceneAsync(scene.buildIndex);
            }
        }

        SceneManager.LoadScene(newSceneIndex, LoadSceneMode.Additive);

        if (UIManager.instance != null)
        {
            UIManager.instance.FadeFromBlack();
        }
    }

    private bool SceneExists(int sceneIndex)
    {
        return sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings;
    }
}
