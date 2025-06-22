using UnityEngine;
using UnityEngine.UI;

public class UICameraToggleButton : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CameraModeManager cameraModeManager;

    [Header("Button Image")]
    [SerializeField] private Image buttonImage;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite flyingCamSprite;
    [SerializeField] private Sprite topDownCamSprite;

    private void Start()
    {
        if (cameraModeManager == null || buttonImage == null)
        {
            Debug.LogError("UICameraToggleButton: Missing dependencies.");
            return;
        }

        // Initial icon state
        UpdateIcon(cameraModeManager.IsTopDown());
    }

    public void OnToggleCameraPressed()
    {
        // Mirror T key toggle behavior
        if (cameraModeManager.IsTopDown())
        {
            cameraModeManager.SendMessage("ActivateFlyingMode");
            UpdateIcon(false);
        }
        else
        {
            cameraModeManager.SendMessage("ActivateTopDownMode");
            UpdateIcon(true);
        }
    }

    public void UpdateIcon(bool isTopDown)
    {
        if (buttonImage == null) return;
        buttonImage.sprite = isTopDown ? topDownCamSprite : flyingCamSprite;
    }
}