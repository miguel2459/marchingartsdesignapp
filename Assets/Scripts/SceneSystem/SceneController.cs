using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;
    public bool isSessionInitialized;
    private bool isSceneLoading = false; // Prevent duplicate scene loading

    // Scene build indices
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
            isSessionInitialized = false;
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
            StartCoroutine(WaitForSessionInitialization());
        }
        else
        {
            if (!IsSceneCurrentlyLoaded(STARTUP_SCENE) && !isSceneLoading)
            {
                LoadSceneAdditive(STARTUP_SCENE);
            }
        }
    }

    public IEnumerator WaitForSessionInitialization()
    {
        Debug.Log("⏳ Waiting for SessionManager to initialize...");

        while (!isSessionInitialized)
        {
            yield return null;
        }

        Debug.Log("✅ SessionManager initialized. Loading Show Selection Scene...");
        SwitchScene(SHOW_SELECTION_SCENE);
    }

    public void OnSessionInitialized()
    {
        isSessionInitialized = true;
    }

    public void LoadSceneAdditive(int sceneIndex)
    {
        if (!SceneExists(sceneIndex) || IsSceneCurrentlyLoaded(sceneIndex) || isSceneLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneIndex));
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex)
    {
        isSceneLoading = true;
        yield return SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
        isSceneLoading = false;
        Debug.Log($"✅ Scene {sceneIndex} loaded additively.");
    }

    public bool IsSceneCurrentlyLoaded(int sceneIndex)
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

    public bool IsSceneCurrentlyLoading(int sceneIndex)
    {
        return isSceneLoading && sceneIndex == SHOW_SELECTION_SCENE; // Ensure only the requested scene is checked
    }


    public void SwitchScene(int newSceneIndex)
    {
        if (!SceneExists(newSceneIndex) || IsSceneCurrentlyLoaded(newSceneIndex) || isSceneLoading)
        {
            Debug.Log($"⚠ Scene {newSceneIndex} is already loaded or is in the process of loading. Skipping duplicate load.");
            return;
        }

        StartCoroutine(SwitchSceneRoutine(newSceneIndex));
    }


    public IEnumerator SwitchSceneRoutine(int newSceneIndex)
    {
        if (!SceneExists(newSceneIndex))
        {
            Debug.LogError($"❌ Scene {newSceneIndex} does not exist in Build Settings.");
            yield break;
        }

        if (IsSceneCurrentlyLoaded(newSceneIndex))
        {
            Debug.Log($"⚠ Scene {newSceneIndex} is already loaded.");
            yield break;
        }

        if (isSceneLoading)
        {
            Debug.Log($"⏳ Another scene is currently loading. Waiting...");
            yield break;
        }

        Debug.Log($"🔄 Switching to Scene: {newSceneIndex}");
        isSceneLoading = true;

        yield return UnloadAllScenesExceptMain();

        Debug.Log("📥 Adding ShowManagerScene...");
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(newSceneIndex, LoadSceneMode.Additive);
        yield return loadOp;

        isSceneLoading = false;

        if (loadOp.isDone)
            Debug.Log($"✅ Successfully switched to Scene: {newSceneIndex}");
        else
            Debug.LogError($"❌ Scene {newSceneIndex} failed to load.");
    }


    private IEnumerator UnloadAllScenesExceptMain()
    {
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != MAIN_SCENE)
            {
                Debug.Log($"🗑 Unloading Scene: {scene.name}");
                yield return SceneManager.UnloadSceneAsync(scene.buildIndex);
            }
        }
        yield return null;
    }

    private bool SceneExists(int sceneIndex)
    {
        return sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings;
    }
}
