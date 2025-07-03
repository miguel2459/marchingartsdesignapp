using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class TransformGizmoUIButtonManager : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Dependencies")]
    public TransformGizmoManager gizmoManager;
    public SelectedMarchers selectedMarchers;

    [Header("UI References")]
    public Button gizmoUIButton;
    public Image buttonImage;

    [Header("Mode Icons")]
    public Sprite positionIcon;
    public Sprite rotateIcon;
    public Sprite scaleIcon;

    private GizmoMode[] gizmoModes = new[] { GizmoMode.Position, GizmoMode.Scale, GizmoMode.Rotate };
    private int currentModeIndex = 0;
    private bool isGizmoActive = false;

    // Hold detection
    private float holdDuration = 0.4f;
    private float holdTimer = 0f;
    private bool isPointerDown = false;
    private bool hasTriggeredHold = false;
    void Start()
    {
        SetVisualGizmoOff();
        gizmoUIButton.interactable = true; // Always interactable
    }
    
    void Update()
    {
        if (isPointerDown)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (!hasTriggeredHold && holdTimer >= holdDuration)
            {
                if (isGizmoActive)
                {
                    gizmoManager.HideTransformGizmo();
                    SetVisualGizmoOff();
                    hasTriggeredHold = true;
                    Debug.Log("📴 Gizmo turned OFF by holding button.");
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasTriggeredHold) return;
        
        if (!isGizmoActive)
        {
            if (selectedMarchers.SelectedCount > 0)
            {
                ActivateGizmo(gizmoModes[currentModeIndex]);
            }
            else
            {
                Debug.Log("🛑 Gizmo button clicked, but no marchers are selected.");
            }
        }
        else
        {
            CycleToNextMode();
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        holdTimer = 0f;
        hasTriggeredHold = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    public void ActivateGizmo(GizmoMode mode)
    {
        isGizmoActive = true;
        currentModeIndex = Array.IndexOf(gizmoModes, mode);
        gizmoManager.SetMode(mode);
        SetVisualGizmoOn();
    }

    private void CycleToNextMode()
    {
        currentModeIndex = (currentModeIndex + 1) % gizmoModes.Length;
        gizmoManager.SetMode(gizmoModes[currentModeIndex]);
        UpdateButtonImage();
    }


    private void SetVisualGizmoOn()
    {
        UpdateButtonImage();
        buttonImage.color = Color.white;
    }

    public void SetVisualGizmoOff()
    {
        //buttonImage.sprite = positionIcon;
        buttonImage.color = new Color(1f, 1f, 1f, 0.35f); // faded
        isGizmoActive = false;
    }
    
    public void UpdateVisualFromExternalMode(GizmoMode mode)
    {
        if (isGizmoActive)
        {
            currentModeIndex = Array.IndexOf(gizmoModes, mode);
            UpdateButtonImage(); // this updates image and color
        }
        else
        {
            ActivateGizmoExternal(mode);
        }

    }

    public void ActivateGizmoExternal(GizmoMode mode)
    {
        isGizmoActive = true;
        currentModeIndex = Array.IndexOf(gizmoModes, mode);
        SetVisualGizmoOn();
    }

    private void UpdateButtonImage()
    {
        if (!buttonImage) return;
        buttonImage.sprite = gizmoModes[currentModeIndex].ToIcon(this);
    }

}
