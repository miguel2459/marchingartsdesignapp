using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class TransformGizmoUIButtonManager : MonoBehaviour, IPointerClickHandler
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


    void Start()
    {
        SetVisualGizmoOff();
        gizmoUIButton.interactable = true; // Always interactable
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
            CycleGizmoMode();
        }
    }

    public void ActivateGizmo(GizmoMode mode)
    {
        isGizmoActive = true;
        currentModeIndex = Array.IndexOf(gizmoModes, mode);
        gizmoManager.SetMode(mode);
        SetVisualGizmoOn();
    }

    private void CycleGizmoMode()
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
