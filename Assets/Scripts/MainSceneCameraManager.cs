using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneCameraManager : MonoBehaviour
{
    private Camera mainCamera;
    private GameObject flyingCameraInstance;
    
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        mainCamera = Camera.main; // Cache the main camera
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Call DisableOtherCameras() immediately when a new scene loads
        DisableOtherCameras();

        if (scene.buildIndex == 4) // ShowManagerScene
        {
            //EnableFlyingCamera();
        }
        else
        {
            EnableMainCamera();
        }
    }

    private void DisableOtherCameras()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();

        foreach (Camera cam in cameras)
        {
            if (cam.CompareTag("FlyingCamera")) // ✅ Ensure the Flying Camera has the correct tag
            {
                Debug.Log("🚀 Skipping Flying Camera, keeping it active.");
                mainCamera.gameObject.SetActive(false);
                continue; // Skip disabling the Flying Camera
            }

            if (cam != mainCamera) // Keep Main Camera on when outside ShowManagerScene
            {
                cam.gameObject.SetActive(false);
                Debug.Log($"📷 Disabling camera: {cam.gameObject.name}");
            }
        }
    }

    private void EnableFlyingCamera()
    {
        if (flyingCameraInstance != null && flyingCameraInstance.activeSelf)
        {
            Debug.Log("🚀 Flying Camera is already enabled, skipping duplicate activation.");
            return;
        }

        Debug.Log("✈️ Enabling Flying Camera.");
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(false);
        }

        if (flyingCameraInstance == null)
        {
            flyingCameraInstance = GameObject.FindGameObjectWithTag("FlyingCamera");
            if (flyingCameraInstance == null)
            {
                Debug.LogError("❌ Flying Camera prefab not found! Ensure it exists in the scene.");
                return;
            }
        }

        flyingCameraInstance.SetActive(true);
    }

    private void EnableMainCamera()
    {
        Debug.Log("📷 Re-enabling Main Camera.");
        if (flyingCameraInstance != null)
        {
            flyingCameraInstance.SetActive(false);
        }

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Main Camera not found!");
        }
    }
}
