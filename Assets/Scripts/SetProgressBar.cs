using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SetProgressBar : MonoBehaviour
{
    SessionManager session = SessionManager.instance;
    public GameObject sectionPrefab;
    public EnsembleDirector2 director;
    public ScrollRect scrollRect;
    public Text currentSetText;

    public InputField setCountsInput;
    public InputField startBPMInput;
    public InputField endBPMInput;

    private int currentSetIndex = 1;
    private int cachedCount = 8;
    private float cachedStartBPM = 140f;
    private float cachedEndBPM = 140f;

    public CountsProgressBar countsProgressBar; // ⬅️ Reference to the counts bar

    public Color selectedColor = new Color(0.7f, 0.85f, 1f);
    public Color defaultColor = Color.white;

    public List<Button> setButtons = new List<Button>();

    private int totalSets = 1;
    private int lastSet = 1;

    private void Start()
    {
        AttachListeners();
    }

    private void AttachListeners()
    {
        setCountsInput.onEndEdit.AddListener(HandleCountEdit);
        startBPMInput.onEndEdit.AddListener(HandleStartBPMEdit);
        endBPMInput.onEndEdit.AddListener(HandleEndBPMEdit);
    }


    public void InitializeSetsBar()
    {
        totalSets = director.numberOfSets;
        UpdateSetBar();
    }

    public void OnTotalSetsChanged(int newValue)
    {
        totalSets = newValue;
        UpdateSetBar();
        countsProgressBar.EnsureSetTimingDefaults(totalSets);
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
        LoadLastSet();

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
            lastSet = int.Parse(session.showStateSO.LastSet);
            currentSetText.text = lastSet.ToString();
        }
        else
        {
            lastSet = 1;
        }
    }

    public void OnSetButtonClick(int setNumber)
    {
        currentSetIndex = setNumber;
        session.showStateSO.LastSet = setNumber.ToString();
        currentSetText.text = setNumber.ToString();
        director.RepositionMarchersToSet(setNumber);
        HighlightSet(setNumber);

        var map = session.runtimeCacheSO.SetTimingMap;
        if (map.TryGetValue(setNumber, out var timing))
        {
            cachedCount = timing.count;
            cachedStartBPM = timing.startBPM;
            cachedEndBPM = timing.endBPM;

            setCountsInput.text = cachedCount.ToString();
            startBPMInput.text = cachedStartBPM.ToString();
            endBPMInput.text = cachedEndBPM.ToString();

            countsProgressBar?.RenderCounts(setNumber, cachedCount);
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

    private void HandleCountEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (int.TryParse(value, out int parsedCount))
            {
                parsedCount = Mathf.Max(1, parsedCount);
                session.runtimeCacheSO.SetTimingMap[currentSetIndex].count = parsedCount;
                cachedCount = parsedCount;
                countsProgressBar.RenderCounts(currentSetIndex, parsedCount);
            }
            else
            {
                setCountsInput.text = cachedCount.ToString();
            }
        }
        else
        {
            setCountsInput.text = cachedCount.ToString();
        }
    }

    private void HandleStartBPMEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (float.TryParse(value, out float parsedStartBPM))
            {
                session.runtimeCacheSO.SetTimingMap[currentSetIndex].startBPM = parsedStartBPM;
                cachedStartBPM = parsedStartBPM;
            }
            else
            {
                startBPMInput.text = cachedStartBPM.ToString();
            }
        }
        else
        {
            startBPMInput.text = cachedStartBPM.ToString();
        }
    }

    private void HandleEndBPMEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (float.TryParse(value, out float parsedEndBPM))
            {
                session.runtimeCacheSO.SetTimingMap[currentSetIndex].endBPM = parsedEndBPM;
                cachedEndBPM = parsedEndBPM;
            }
            else
            {
                endBPMInput.text = cachedEndBPM.ToString();
            }
        }
        else
        {
            endBPMInput.text = cachedEndBPM.ToString();
        }
    }

    public void UpdateTimingInputsForSet(int setIndex)
    {
        if (session.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing))
        {
            setCountsInput.text = timing.count.ToString();
            startBPMInput.text = timing.startBPM.ToString();
            endBPMInput.text = timing.endBPM.ToString();
        }
    }


    /// <summary>
    /// Highlights the button for the given set number and resets others to default.
    /// Also updates the currentSetText UI.
    /// </summary>
    public void HighlightSet(int setNumber)
    {
        currentSetIndex = setNumber;
        currentSetText.text = setNumber.ToString();
        session.showStateSO.LastSet = setNumber.ToString();

        for (int i = 0; i < setButtons.Count; i++)
        {
            Text buttonText = setButtons[i].GetComponentInChildren<Text>();
            if (buttonText != null && int.TryParse(buttonText.text, out int buttonSetNumber))
            {
                bool isActive = (buttonSetNumber == setNumber);
                setButtons[i].image.color = isActive ? selectedColor : defaultColor;
            }
        }
    }
}
