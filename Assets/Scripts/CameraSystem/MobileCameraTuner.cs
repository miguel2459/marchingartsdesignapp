using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MobileCameraTuner : MonoBehaviour
{
    [Header("Flying Camera")]
    public FlyingCameraController flyingCam;
    public TMP_InputField rotationSpeedInput;
    public TMP_InputField zoomSpeedInput;
    public TMP_InputField panSpeedInput;
    public TMP_InputField flyingTouchZoomMultiplierInput;

    [Header("TopDown Camera")]
    public TopDownCameraController topdownCam;
    public TMP_InputField tdZoomSpeedInput;
    public TMP_InputField tdPanSpeedInput;
    public TMP_InputField tdTouchZoomMultiplierInput;

    [Header("Scroll And Pinch")]
    public ScrollAndPinch scrollAndPinch;
    public TMP_InputField pinchZoomSensitivityInput;
    public TMP_InputField upperBoundInput;
    public TMP_InputField lowerBoundInput;
    public TMP_InputField panDivisorInput;

    void Start()
    {
        ApplyInitialValues();
    }

    public void ApplyValues()
    {
        // FlyingCam
        float.TryParse(rotationSpeedInput.text, out flyingCam.rotationSpeed);
        float.TryParse(zoomSpeedInput.text, out flyingCam.zoomSpeed);
        float.TryParse(panSpeedInput.text, out flyingCam.panSpeed);
        float.TryParse(flyingTouchZoomMultiplierInput.text, out flyingCam.touchZoomMultiplier);

        // TopDown
        float.TryParse(tdZoomSpeedInput.text, out topdownCam.zoomSpeed);
        float.TryParse(tdPanSpeedInput.text, out topdownCam.panSpeed);
        float.TryParse(tdTouchZoomMultiplierInput.text, out topdownCam.touchZoomMultiplier);

        // Scroll & Pinch
        float.TryParse(pinchZoomSensitivityInput.text, out scrollAndPinch.zoomSensitivity);
        float.TryParse(upperBoundInput.text, out scrollAndPinch.CameraUpperHeightBound);
        float.TryParse(lowerBoundInput.text, out scrollAndPinch.CameraLowerHeightBound);
        float.TryParse(panDivisorInput.text, out scrollAndPinch.DecreaseCameraPanSpeed);
    }

    void ApplyInitialValues()
    {
        rotationSpeedInput.text = flyingCam.rotationSpeed.ToString();
        zoomSpeedInput.text = flyingCam.zoomSpeed.ToString();
        panSpeedInput.text = flyingCam.panSpeed.ToString();
        flyingTouchZoomMultiplierInput.text = flyingCam.touchZoomMultiplier.ToString();

        tdZoomSpeedInput.text = topdownCam.zoomSpeed.ToString();
        tdPanSpeedInput.text = topdownCam.panSpeed.ToString();
        tdTouchZoomMultiplierInput.text = topdownCam.touchZoomMultiplier.ToString();

        pinchZoomSensitivityInput.text = scrollAndPinch.zoomSensitivity.ToString();
        upperBoundInput.text = scrollAndPinch.CameraUpperHeightBound.ToString();
        lowerBoundInput.text = scrollAndPinch.CameraLowerHeightBound.ToString();
        panDivisorInput.text = scrollAndPinch.DecreaseCameraPanSpeed.ToString();
    }
}
