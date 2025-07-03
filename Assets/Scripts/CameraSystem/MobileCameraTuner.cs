using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobileCameraTuner : MonoBehaviour
{
    [Header("Flying Camera")]
    public FlyingCameraController flyingCam;
    public TMP_InputField rotationSpeedInput;
    public TMP_InputField zoomSpeedInput;
    public TMP_InputField zoomMultiplierInput;
    public TMP_InputField panSpeedInput;

    [Header("TopDown Camera")]
    public TopDownCameraController topdownCam;
    public TMP_InputField tdZoomSpeedInput;
    public TMP_InputField tdZoomMultiplierInput;
    public TMP_InputField tdPanSpeedInput;

    [Header("Scroll And Pinch (Touch Settings)")]
    public ScrollAndPinch scrollAndPinch;
    public TMP_InputField touchPanSpeedInput;
    public TMP_InputField touchZoomSensitivityInput;
    public TMP_InputField touchYawSpeedInput;
    public TMP_InputField touchPitchSpeedInput;

    [Header("Scroll And Pinch (Pitch Clamp Limits)")]
    public TMP_InputField minPitchClampInput;
    public TMP_InputField maxPitchClampInput;

    [Header("Scroll And Pinch (Zoom Clamp Bounds)")]
    public TMP_InputField maxZoomOutDistanceInput;
    public TMP_InputField maxZoomInDistanceInput;

    void Start()
    {
        ApplyInitialValues();
    }

    public void ApplyValues()
    {
        // FlyingCam
        float.TryParse(rotationSpeedInput.text, out flyingCam.rotationSpeed);
        float.TryParse(zoomSpeedInput.text, out flyingCam.zoomSpeed);
        float.TryParse(zoomMultiplierInput.text, out flyingCam.zoomMultiplier);
        float.TryParse(panSpeedInput.text, out flyingCam.panSpeed);

        // TopDownCam
        float.TryParse(tdZoomSpeedInput.text, out topdownCam.zoomSpeed);
        float.TryParse(tdZoomMultiplierInput.text, out topdownCam.zoomMultiplier);
        float.TryParse(tdPanSpeedInput.text, out topdownCam.panSpeed);

        // Scroll & Pinch – Touch Movement
        float.TryParse(touchPanSpeedInput.text, out scrollAndPinch.touchPanSpeed);
        float.TryParse(touchZoomSensitivityInput.text, out scrollAndPinch.touchZoomSensitivity);

        
    }

    void ApplyInitialValues()
    {
        // FlyingCam
        rotationSpeedInput.text = flyingCam.rotationSpeed.ToString("F2");
        zoomSpeedInput.text = flyingCam.zoomSpeed.ToString("F2");
        zoomMultiplierInput.text = flyingCam.zoomMultiplier.ToString("F2");
        panSpeedInput.text = flyingCam.panSpeed.ToString("F2");

        // TopDownCam
        tdZoomSpeedInput.text = topdownCam.zoomSpeed.ToString("F2");
        tdZoomMultiplierInput.text = topdownCam.zoomMultiplier.ToString("F2");
        tdPanSpeedInput.text = topdownCam.panSpeed.ToString("F2");

        // Scroll & Pinch – Touch Movement
        touchPanSpeedInput.text = scrollAndPinch.touchPanSpeed.ToString("F2");
        touchZoomSensitivityInput.text = scrollAndPinch.touchZoomSensitivity.ToString("F2");

        
    }
}
