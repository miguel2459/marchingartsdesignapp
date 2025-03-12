using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    // Scene build indices for clarity
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        HandleInitialSceneLoad();
    }

    private void HandleInitialSceneLoad()
    {
        if (PlayerPrefs.GetInt("IsLoggedIn", 0) == 1)
        {
            SessionManager.instance.AutoLogin();

            Debug.Log($"🔄 Resuming session, loading Show Selection Scene");
            int sceneToLoad = 3;            
            SwitchScene(sceneToLoad);
        }
        else
        {
            if (!IsSceneCurrentlyLoaded(STARTUP_SCENE))
            {
                LoadSceneAdditive(STARTUP_SCENE);
            }
        }
    }

    public void LoadSceneAdditive(int sceneIndex)
    {
        if (!SceneExists(sceneIndex) || IsSceneCurrentlyLoaded(sceneIndex)) return;
        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
    }

    private bool IsSceneCurrentlyLoaded(int sceneIndex)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).buildIndex == sceneIndex)
            {
                return true;
            }
        }
        return false;
    }

    public void UnloadScene(int sceneIndex)
    {
        if (!SceneExists(sceneIndex) || !IsSceneCurrentlyLoaded(sceneIndex)) return;
        SceneManager.UnloadSceneAsync(sceneIndex);
    }

    public void SwitchScene(int newSceneIndex)
    {
        if (!SceneExists(newSceneIndex) || IsSceneCurrentlyLoaded(newSceneIndex)) return;
        StartCoroutine(SwitchSceneRoutine(newSceneIndex));
    }

    private IEnumerator SwitchSceneRoutine(int newSceneIndex)
    {
        Debug.Log($"🔄 Switching to Scene: {newSceneIndex}");

        // Optional: Implement fade-out UI effect here
        yield return new WaitForSeconds(1f);

        // Unload all scenes except MAIN_SCENE
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != MAIN_SCENE)
            {
                SceneManager.UnloadSceneAsync(scene.buildIndex);
            }
        }

        SceneManager.LoadScene(newSceneIndex, LoadSceneMode.Additive);

        // Save last visited scene for auto-login redirection
        PlayerPrefs.SetString("LastScene", newSceneIndex.ToString());
        PlayerPrefs.Save();

        // Optional: Implement fade-in UI effect here
    }

    private bool SceneExists(int sceneIndex)
    {
        return sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings;
    }
}
