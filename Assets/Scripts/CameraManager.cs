using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class CameraManager : MonoBehaviour
{
    // List to store the created cameras
    public List<Camera> createdCameras = new List<Camera>();

    // Method to create a new camera and add it to the list
    public void CreateCamera(Transform sceneCameraTransform)
    {
        // Find the "Camera Shots" object in the scene
        GameObject cameraShotsParent = GameObject.Find("Camera Shots");
        if (cameraShotsParent == null)
        {
            Debug.LogError("Camera Shots object not found in the scene.");
            return;
        }

        // Create a new GameObject with a Camera component
        GameObject newCameraObj = new GameObject("CapturedCamera_" + createdCameras.Count);
        Camera newCamera = newCameraObj.AddComponent<Camera>();

        // Set the transform of the new camera to match the scene camera
        newCamera.transform.position = sceneCameraTransform.position;
        newCamera.transform.rotation = sceneCameraTransform.rotation;

        // Set the parent of the new camera to "Camera Shots"
        newCameraObj.transform.SetParent(cameraShotsParent.transform);

        // Basic camera settings
        newCamera.fieldOfView = Camera.main.fieldOfView;
        newCamera.nearClipPlane = Camera.main.nearClipPlane;
        newCamera.farClipPlane = Camera.main.farClipPlane;

        // Add the new camera to the list
        createdCameras.Add(newCamera);

        Debug.Log("Camera captured and created: " + newCamera.name);
    }

    // References to the alive and dead backdrops
    public GameObject aliveBackdrops;
    public GameObject deadBackdrops;

    private int totalRenders;
    private int currentRender;

    private string batchDirectory;

    void Start()
    {
        ValidateCameraList();

        if (createdCameras.Count == 0)
        {
            Debug.LogError("No cameras available for rendering.");
            EditorApplication.isPlaying = false;
            return;
        }

        // Create a new batch directory
        batchDirectory = $"Assets/Rendered Shots/Batch_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        if (!Directory.Exists(batchDirectory))
        {
            Directory.CreateDirectory(batchDirectory);
        }

        totalRenders = createdCameras.Count * 2; // Each camera renders twice (Alive and Dead states)
        currentRender = 0;

        EditorApplication.update += RenderNextCamera;
    }

    // Ensures all cameras in the list still exist
    private void ValidateCameraList()
    {
        createdCameras.RemoveAll(cam => cam == null);

        if (createdCameras.Count == 0)
        {
            Debug.LogError("No valid cameras found in the list.");
        }
    }

    private void RenderNextCamera()
    {
        if (currentRender < totalRenders)
        {
            int cameraIndex = currentRender / 2;
            bool isAliveState = currentRender % 2 == 0;

            if (cameraIndex < createdCameras.Count && createdCameras[cameraIndex] != null)
            {
                ToggleBackdrops(isAliveState);
                RenderCamera(createdCameras[cameraIndex], cameraIndex + 1, isAliveState ? "Alive" : "Dead");
            }
            else
            {
                Debug.LogError($"Camera at index {cameraIndex} is missing or has been deleted.");
            }

            currentRender++;

            // Update progress bar
            float progress = (float)currentRender / totalRenders;
            EditorUtility.DisplayProgressBar("Rendering Cameras", "Rendering scene...", progress);
        }
        else
        {
            EditorApplication.update -= RenderNextCamera;
            EditorUtility.ClearProgressBar();
            EditorApplication.isPlaying = false;
            Debug.Log("Rendering completed, exiting play mode.");
        }
    }

    private void ToggleBackdrops(bool showAlive)
    {
        if (aliveBackdrops != null && deadBackdrops != null)
        {
            aliveBackdrops.SetActive(showAlive);
            deadBackdrops.SetActive(!showAlive);
        }
    }

    private void RenderCamera(Camera camera, int index, string state)
    {
        if (camera == null)
        {
            Debug.LogError("Attempted to render a null camera.");
            return;
        }

        // Set up a higher-resolution render texture (e.g., 4K resolution)
        RenderTexture renderTexture = new RenderTexture(3840, 2160, 24); // 4K resolution: 3840x2160
        camera.targetTexture = renderTexture;
        Texture2D renderResult = new Texture2D(3840, 2160, TextureFormat.RGB24, false);

        // Render the camera
        camera.Render();

        // Save the render as a JPG image
        RenderTexture.active = renderTexture;
        renderResult.ReadPixels(new Rect(0, 0, 3840, 2160), 0, 0);
        byte[] byteArray = renderResult.EncodeToJPG(100); // Maximum quality

        // Save the file in the batch directory with a name indicating the state (Alive/Dead)
        string filePath = $"{batchDirectory}/Render_{index}_{state}.jpg";
        File.WriteAllBytes(filePath, byteArray);

        // Clean up
        RenderTexture.active = null;
        camera.targetTexture = null;
        DestroyImmediate(renderTexture);
        DestroyImmediate(renderResult);

        Debug.Log($"Camera rendered and saved as: {filePath}");
    }
}