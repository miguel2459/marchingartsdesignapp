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
    private bool inputBlocked = false;
    [SerializeField] private SelectedMarchers selected;
    [SerializeField] private EnsembleDirector2 director;
    [SerializeField] private UICameraToggleButton topDownToggle;
    [SerializeField] private ScrollAndPinch scrollAndPinch; // Inject in inspector

    public bool IsTopDown() => isTopDownActive;

    void Start()
    {
        if (director != null)
        {
            director.GetComponent<MarcherManager>().OnMarchersReady += ActivateFlyingMode;
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isTopDownActive)
                ActivateFlyingMode();
            else
                ActivateTopDownMode();
            topDownToggle.UpdateIcon(isTopDownActive);
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
        SetActiveCamera(topDownCamComponent);
        selected.cameraFocusHandler = topDownCamera;
        if (scrollAndPinch != null)
        {
            scrollAndPinch.Camera = topDownCamComponent;
            scrollAndPinch.Rotate = false;
        }
    }

    private void ActivateFlyingMode()
    {
        // Enable flying camera, disable orthographic camera
        topDownCamComponent.gameObject.SetActive(false);
        flyingCamComponent.gameObject.SetActive(true);

        topDownCamera.Disable();
        flyingCamera.Enable();
        isTopDownActive = false;

        SetActiveCamera(flyingCamComponent);
        selected.cameraFocusHandler = flyingCamera;
        if (scrollAndPinch != null)
        {
            scrollAndPinch.Camera = flyingCamComponent;
            scrollAndPinch.Rotate = true;
        }

        //Debug.Log($"[CameraModeManager] 🔵 ActivateFlyingMode — Position: {transform.position}, Rotation: {transform.rotation}");
        flyingCamera.SetInitialTransform(
            new Vector3(-10f, 20f, 60f),
            new Quaternion(0.353553414f, 0.612372458f, -0.353553414f, 0.612372458f)
        );
        director.GetComponent<MarcherManager>().OnMarchersReady -= ActivateFlyingMode;
    }


	public void UpdateCameraFocus()
	{
    	List<GameObject> targets = selected.SelectedCount > 0
        	? selected.GetSelectionCopy()
        	: director.MarcherObjects;

    	if (targets == null || targets.Count == 0)
    	{
        	Debug.LogWarning("🎯 Camera focus failed: No marchers available.");
        	return;
    	}

    	Vector3 center = SmartReshapeService.GetFocalPoint(targets);

    	selected.cameraFocusHandler?.SetSelectedMarchers(targets);
    	selected.cameraFocusHandler?.FocusOnSelection(center);

    	Debug.Log($"📸 Camera focus updated to {(selected.SelectedCount > 0 ? "selected" : "all")} marchers.");
	}
    public void HandleZoomIntent(float delta)
    {
        HandleZoomIntent(delta, isTouch: false);
    }
    
    public void HandleZoomIntent(float delta, bool isTouch)
    {
        if (IsInputBlocked()) return;

        if (isTopDownActive)
            topDownCamera?.ApplyZoom(delta);
        else
            flyingCamera?.ApplyZoom(delta); 
    }

    public void HandlePanIntent(Vector2 delta)
    {
        if (IsInputBlocked()) return;

        if (isTopDownActive)
            topDownCamera?.ApplyPan(delta);
        else
            flyingCamera?.ApplyPan(delta); // this was missing before
    }

    public void HandleRotateIntent(Vector2 delta)
    {
        if (IsInputBlocked()) return;

        if (!isTopDownActive) // only FlyingCamera supports rotation
            flyingCamera?.ApplyRotation(delta);
    }
    
    public void HandlePivotIntent(Vector2 delta)
    {
        if (IsInputBlocked()) return;

        if (!isTopDownActive)
            flyingCamera?.ApplyPivot(delta);
    }

    public void ReapplyActiveCameraMode()
    {
        if (topDownCamComponent.gameObject.activeSelf)
        {
            ActivateTopDownMode();
            Debug.Log("🔁 Reapplied TopDown camera mode.");
        }
        else if (flyingCamComponent.gameObject.activeSelf)
        {
            ActivateFlyingMode();
            Debug.Log("🔁 Reapplied Flying camera mode.");
        }
        else
        {
            Debug.LogWarning("❓ No known active camera. Defaulting to FlyingMode.");
            ActivateFlyingMode();
        }
    }

    public void SetActiveCamera(Camera activeCam)
    {
        bool isTopDown = (activeCam.orthographic == true);

        foreach (var marcher in director.Marchers)
        {
            var billboard = marcher.GetComponentInChildren<MarcherLabelBillboard>();
            if (billboard != null)
            {
                billboard.SetCamera(activeCam);
                billboard.SetMode(isTopDown);
            }
        }

        selected.cam = activeCam;
        selected.transformGizmoManager?.SetActiveCamera(activeCam);
    }
    
    public void BlockInput(bool blocked)
    {
        inputBlocked = blocked;
    }

    public bool IsInputBlocked() => inputBlocked;

}
