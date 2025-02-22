using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneCameraManager : MonoBehaviour
{
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisableOtherCameras();
    }

    private void DisableOtherCameras()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in cameras)
        {
            if (cam != Camera.main) // Keep the MainScene camera on
            {
                cam.gameObject.SetActive(false);
            }
        }
    }
}
