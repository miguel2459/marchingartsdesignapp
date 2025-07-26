using UnityEngine;
using TMPro;
using UnityEngine.UI; // Import TextMeshPro namespace
using System;

public class ShowPanelUI : MonoBehaviour
{
    public TMP_Text showTitleText;
    public TMP_Text modifiedDateText;
    public TMP_Text groupText;
    public Button showButton;
    private SessionManager.ShowData showData;
    public GameObject editButton;
    public GameObject loadingSpinner;
    public event Action<SessionManager.ShowData> onShowSelected;

    public void SetShowData(SessionManager.ShowData data)
    {
        showData = data;
        showTitleText.text = data.showTitle;
        modifiedDateText.text = "Last Saved: " + data.lastModified;
        groupText.text = "Group: " + data.group;

        showButton.onClick.AddListener(() => onShowSelected?.Invoke(showData));
        showButton.interactable = false; // ⛔ Disable at startup
    }
    
    public void ShowLoadingSpinner(bool show)
    {
        if (loadingSpinner != null)
            loadingSpinner.SetActive(show);
    }
    
    public void ShowReady()
    {
        ShowLoadingSpinner(false);
        if (editButton != null)
            editButton.SetActive(true);
        
        showButton.interactable = true; 
    }
    
    void Update()
    {
        if (loadingSpinner?.activeSelf == true)
        {
            loadingSpinner.transform.Rotate(Vector3.forward * -300f * Time.deltaTime);
        }
    }

}
