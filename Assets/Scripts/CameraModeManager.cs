using UnityEngine;
using System.Collections.Generic;

public class CameraModeManager : MonoBehaviour
{
    
    public FlyingCameraController flyingCamera;
    public TopDownCameraController topDownCamera;
    [Header("Camera GameObjects")]
    public Camera flyingCamComponent;
    public Camera topDownCamComponent;
    private bool isTopDownActive = false;
    private List<GameObject> selectedMarchers = new List<GameObject>();
    [SerializeField] private SelectedMarchers selected;

    void Start()
    {
        //Debug.Log($"[CameraModeManager] 🔵 Awake — Position: {transform.position}, Rotation: {transform.rotation}");
        ActivateFlyingMode();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isTopDownActive)
                ActivateFlyingMode();
            else
                ActivateTopDownMode();
        }
    }

    private void ActivateTopDownMode()
    {
        // Enable orthographic camera, disable flying camera
        flyingCamComponent.gameObject.SetActive(false);
        topDownCamComponent.gameObject.SetActive(true);

        flyingCamera.Disable();
        topDownCamera.Enable();
        isTopDownActive = true;
        selected.SetActiveCamera(topDownCamComponent);
        selected.cameraFocusHandler = topDownCamera;

    }

    private void ActivateFlyingMode()
    {
        // Enable flying camera, disable orthographic camera
        topDownCamComponent.gameObject.SetActive(false);
        flyingCamComponent.gameObject.SetActive(true);

        topDownCamera.Disable();
        flyingCamera.Enable();
        isTopDownActive = false;

        selected.SetActiveCamera(flyingCamComponent);
        selected.cameraFocusHandler = flyingCamera;

        //Debug.Log($"[CameraModeManager] 🔵 ActivateFlyingMode — Position: {transform.position}, Rotation: {transform.rotation}");
        flyingCamera.SetInitialTransform(
            new Vector3(-10f, 20f, 60f),
            new Quaternion(0.2418447f, 0.664463f, -0.2418447f, 0.664463f)
        );
    }
}
