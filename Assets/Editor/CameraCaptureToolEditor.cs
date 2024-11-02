using UnityEngine;
using UnityEditor;

public class CameraCaptureToolEditor : Editor
{
    // Hotkey combination (Ctrl + Alt + Shift + X) to create a new camera
    [MenuItem("Tools/Capture Camera Transform %#&x")]
    private static void CaptureCameraTransform()
    {
        // Find the scene view camera
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogError("No active SceneView found!");
            return;
        }

        // Get the transform of the scene camera
        Transform sceneCameraTransform = sceneView.camera.transform;

        // Find or create the CameraManager in the scene
        CameraManager cameraManager = GameObject.FindObjectOfType<CameraManager>();
        if (cameraManager == null)
        {
            GameObject cameraManagerObj = new GameObject("CameraManager");
            cameraManager = cameraManagerObj.AddComponent<CameraManager>();
        }

        // Create a new camera in the scene and add it to the CameraManager
        cameraManager.CreateCamera(sceneCameraTransform);
    }
}
