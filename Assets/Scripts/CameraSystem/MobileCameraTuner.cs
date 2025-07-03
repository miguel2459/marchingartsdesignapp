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
    public TMP_InputField touchRotateSpeedInput;

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

        // Scroll & Pinch – Touch Gestures
        float.TryParse(touchPanSpeedInput.text, out scrollAndPinch.touchPanSpeed);
        float.TryParse(touchZoomSensitivityInput.text, out scrollAndPinch.touchZoomSensitivity);
        float.TryParse(touchRotateSpeedInput.text, out scrollAndPinch.touchRotateSpeed);

        // Scroll & Pinch – Clamp Bounds
        float.TryParse(maxZoomOutDistanceInput.text, out scrollAndPinch.maxZoomOutDistance);
        float.TryParse(maxZoomInDistanceInput.text, out scrollAndPinch.maxZoomInDistance);
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

        // Scroll & Pinch – Touch Gestures
        touchPanSpeedInput.text = scrollAndPinch.touchPanSpeed.ToString("F2");
        touchZoomSensitivityInput.text = scrollAndPinch.touchZoomSensitivity.ToString("F2");
        touchRotateSpeedInput.text = scrollAndPinch.touchRotateSpeed.ToString("F2");

        // Scroll & Pinch – Clamp Bounds
        maxZoomOutDistanceInput.text = scrollAndPinch.maxZoomOutDistance.ToString("F2");
        maxZoomInDistanceInput.text = scrollAndPinch.maxZoomInDistance.ToString("F2");
    }
}
