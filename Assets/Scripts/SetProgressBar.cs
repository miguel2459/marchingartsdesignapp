using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SetProgressBar : MonoBehaviour
{
    public GameObject sectionPrefab;
    public EnsembleDirector2 director;
    public ScrollRect scrollRect;
    public Text currentSetText;

    public InputField setCountsInput;
    public InputField startBPMInput;
    public InputField endBPMInput;

    public CountsProgressBar countsProgressBar; // ⬅️ Reference to the counts bar

    public Color selectedColor = new Color(0.7f, 0.85f, 1f);
    public Color defaultColor = Color.white;

    public List<Button> setButtons = new List<Button>();

    private int totalSets = 1;
    private int lastSet = 1;

    public void InitializeSetsBar()
    {
        totalSets = director.numberOfSets;
        currentSetText.text = SessionManager.instance.SessionState.LastSet.ToString();
        LoadLastSet();
        ClearButtons();
    }

    public void OnTotalSetsChanged(int newValue)
    {
        totalSets = newValue;
        UpdateSetBar();
    }

    public void ClearButtons()
    {
        foreach (Transform child in scrollRect.content)
        {
            Destroy(child.gameObject);
        }
        setButtons.Clear();
    }

    private void UpdateSetBar()
    {
        ClearButtons();

        for (int i = 0; i < totalSets; i++)
        {
            GameObject newSection = Instantiate(sectionPrefab, scrollRect.content.transform);
            Button sectionButton = newSection.GetComponent<Button>();
            setButtons.Add(sectionButton);

            Text buttonText = newSection.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (i + 1).ToString();
            }

            int setIndex = i + 1;
            sectionButton.onClick.AddListener(() => OnSetButtonClick(setIndex));
        }

        float buttonWidth = sectionPrefab.GetComponent<RectTransform>().sizeDelta.x;
        scrollRect.content.sizeDelta = new Vector2(totalSets * (buttonWidth + 10), scrollRect.content.sizeDelta.y);

        HighlightSet(lastSet);
    }

    private void LoadLastSet()
    {
        if (SessionManager.instance != null)
        {
            lastSet = int.Parse(SessionManager.instance.SessionState.LastSet);
        }
        else
        {
            lastSet = 1;
        }
    }

    private void OnSetButtonClick(int setNumber)
    {
        Debug.Log($"Clicked on set {setNumber}");

        if (currentSetText != null)
        {
            currentSetText.text = setNumber.ToString();
        }

        HighlightSet(setNumber);

        if (SessionManager.instance != null)
        {
            SessionManager.instance.SessionState.LastSet = setNumber.ToString();
            director.RepositionMarchersToSet(setNumber);

            // Load SetTimingData
            var setMap = SessionManager.instance.SessionState.SetTimingMap;
            if (setMap.TryGetValue(setNumber, out var timing))
            {
                setCountsInput.text = timing.count.ToString();
                startBPMInput.text = timing.startBPM.ToString();
                endBPMInput.text = timing.endBPM.ToString();
                countsProgressBar?.RenderCounts(setNumber, timing.count); // ⬅️ Re-render the count buttons
            }
            else
            {
                Debug.LogWarning($"⚠️ No timing data found for set {setNumber}");
                setCountsInput.text = "";
                startBPMInput.text = "";
                endBPMInput.text = "";
                countsProgressBar?.ClearCounts();
            }
        }
    }

    private void HighlightSet(int setNumber)
    {
        for (int i = 0; i < setButtons.Count; i++)
        {
            Text buttonText = setButtons[i].GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                int buttonSetNumber = int.Parse(buttonText.text);
                setButtons[i].image.color = (buttonSetNumber == setNumber) ? selectedColor : defaultColor;
            }
        }
    }
}
