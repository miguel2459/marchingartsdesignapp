using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class MarcherDeleteConfirmationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject marcherListItemPrefab;
    [SerializeField] private Transform marcherListContentParent;

    private List<GameObject> selectedMarchers = new List<GameObject>();
    private Action<List<GameObject>> onConfirmCallback;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        Hide();
    }

    public void Show(List<GameObject> selected, Action<List<GameObject>> onConfirm)
    {
        selectedMarchers = new List<GameObject>(selected);
        onConfirmCallback = onConfirm;

        gameObject.SetActive(true);
        ClearList();

        foreach (GameObject m in selectedMarchers)
        {
            if (m.TryGetComponent(out MarcherIdentityManager identity))
            {
                GameObject item = Instantiate(marcherListItemPrefab, marcherListContentParent);
                foreach (var tf in item.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (tf.name.Contains("Abbreviation"))
                        tf.text = "-" + identity.GetFullName();  // returns "FL1", "TP5", etc.

                    else if (tf.name.Contains("FullSection"))
                    {
                        var id = identity.GetIdentity();
                        tf.text = $"{id.section} {id.number}";
                    }
                }
            }
        }

        int count = selectedMarchers.Count;
        string label = count == 1 ? "Delete 1 Marcher?" : $"Delete {count} Marchers?";
        titleText.text = label;

    }

    public void Hide()
    {
        gameObject.SetActive(false);
        selectedMarchers.Clear();
        onConfirmCallback = null;
    }

    private void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke(selectedMarchers);
        Hide();
    }

    private void ClearList()
    {
        foreach (Transform child in marcherListContentParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void CancelByClickingAway() => Hide();
}
