using UnityEngine;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    public KeyCode screenshotKey = KeyCode.P; // The key to trigger the screenshot
    public int screenshotWidth = 1920; // Width of the screenshot
    public int screenshotHeight = 1080; // Height of the screenshot

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            CaptureScreenshot();
        }
    }

    void CaptureScreenshot()
    {
        // Create a RenderTexture with the desired dimensions
        RenderTexture rt = new RenderTexture(screenshotWidth, screenshotHeight, 24);
        Camera.main.targetTexture = rt;

        // Create a new Texture2D with the same dimensions as the RenderTexture
        Texture2D screenShot = new Texture2D(screenshotWidth, screenshotHeight, TextureFormat.RGB24, false);

        // Render the camera's view
        Camera.main.Render();

        // Set the active RenderTexture and read the pixels
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, screenshotWidth, screenshotHeight), 0, 0);
        screenShot.Apply();

        // Reset the camera's target texture and the active RenderTexture
        Camera.main.targetTexture = null;
        RenderTexture.active = null;

        // Clean up the RenderTexture
        Destroy(rt);

        // Encode the texture to PNG
        byte[] bytes = screenShot.EncodeToPNG();

        // Define the screenshot file name and path
        string filePath = Path.Combine(Application.dataPath, "Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

        // Write the screenshot to the specified file
        File.WriteAllBytes(filePath, bytes);

        // Log the success message
        Debug.Log("Screenshot successfully saved to: " + filePath);
    }
}
