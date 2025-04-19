using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SetProgressBar : MonoBehaviour
{
    [Header("Director")]
    [SerializeField] private EnsembleDirector2 director;  // drag in your EnsembleDirector2
    private ISetProgressTracker progressSource => director as ISetProgressTracker;        // cached cast
    public GameObject sectionPrefab;
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
    private List<SetButtonWrapper> setButtonWrappers = new List<SetButtonWrapper>();
    private int totalSets = 1;
    private int lastSet = 1;
    private void Awake()
    {
        if (director == null)
            Debug.LogError("SetProgressBar: please assign your EnsembleDirector2!");
        else if (progressSource != null)
            progressSource.OnSetProgressChanged += UpdateSetProgressColor;

        setButtonWrappers.Clear();

        foreach (Transform child in scrollRect.content)
        {
            GameObject buttonObj = child.gameObject;

            if (buttonObj.TryGetComponent(out Button btn))
            {
                Transform overlay = buttonObj.transform.Find("HighlightOverlay");

                setButtonWrappers.Add(new SetButtonWrapper
                {
                    button = btn,
                    highlightOverlay = overlay != null ? overlay.gameObject : null
                });
            }
        }
        

        Debug.Log($"✅ Awake initialized {setButtonWrappers.Count} pre-existing set buttons.");
    }

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
        totalSets = director.NumberOfSets;
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
            Destroy(child.gameObject);

        setButtonWrappers.Clear(); // 🧼 clear wrappers instead of plain buttons
    }


    private void UpdateSetBar()
    {
        ClearButtons();   // Clears scroll content and list
        LoadLastSet();    // Sets lastSet and updates currentSetText

        for (int i = 0; i < totalSets; i++)
        {
            GameObject newSection = Instantiate(sectionPrefab, scrollRect.content.transform);
            Button sectionButton = newSection.GetComponent<Button>();

            // 🔍 Try to find the HighlightOverlay child
            Transform overlayTransform = newSection.transform.Find("HighlightOverlay");
            GameObject overlay = overlayTransform != null ? overlayTransform.gameObject : null;

            if (overlay != null) overlay.SetActive(false); // Hide by default

            // 🏷️ Update label text
            Text buttonText = newSection.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (i + 1).ToString();
            }

            int setIndex = i + 1;
            sectionButton.onClick.AddListener(() => OnSetButtonClick(setIndex));

            // ✅ Store wrapper
            setButtonWrappers.Add(new SetButtonWrapper
            {
                button = sectionButton,
                highlightOverlay = overlay
            });
        }

        // 🧮 Auto-adjust scroll content size
        float buttonWidth = sectionPrefab.GetComponent<RectTransform>().sizeDelta.x;
        scrollRect.content.sizeDelta = new Vector2(totalSets * (buttonWidth + 10), scrollRect.content.sizeDelta.y);

        // ✨ Highlight currently selected set
        HighlightSet(lastSet);
    }


    private void LoadLastSet()
    {
        if (SessionManager.instance != null)
        {
            string lastSetStr = director.SessionLoader.ShowState.LastSet;

            if (int.TryParse(lastSetStr, out int parsedSet))
            {
                lastSet = parsedSet;
                currentSetText.text = lastSet.ToString();
                Debug.Log($"🔁 Loaded lastSet = {lastSet} from session");
            }
            else
            {
                lastSet = 1;
                currentSetText.text = "1";
                Debug.LogWarning($"⚠️ Invalid LastSet '{lastSetStr}' in session — defaulting to 1");
            }
        }
        else
        {
            lastSet = 1;
            Debug.LogWarning("⚠️ SessionManager is null — defaulting lastSet to 1");
        }
    }
    
    public void OnSetButtonClick(int setNumber)
    {
        Debug.Log($"🟦 OnSetButtonClick called for Set {setNumber}");

        currentSetIndex = setNumber;
        director.SessionLoader.ShowState.LastSet = setNumber.ToString();
        currentSetText.text = setNumber.ToString();

        countsProgressBar?.ResetHighlight();

        Debug.Log($"🔁 Repositioning marchers to Set {setNumber}");
        director.RepositionMarchersToSet(setNumber);
        director.ColorMarchersForSet(setNumber);

        HighlightSet(setNumber);

        var map = director.SessionLoader.RuntimeCache.SetTimingMap;
        if (map.TryGetValue(setNumber, out var timing))
        {
            cachedCount = timing.count;
            cachedStartBPM = timing.startBPM;
            cachedEndBPM = timing.endBPM;

            setCountsInput.text = cachedCount.ToString();
            startBPMInput.text = cachedStartBPM.ToString();
            endBPMInput.text = cachedEndBPM.ToString();

            if (cachedCount <= 0)
            {
                Debug.Log($"ℹ️ Set {setNumber} has 0 counts — skipping count button rendering.");
                countsProgressBar?.ClearCounts();
                return;
            }

            Debug.Log($"✅ Timing Data for Set {setNumber}: Counts = {cachedCount}, Start BPM = {cachedStartBPM}, End BPM = {cachedEndBPM}");
            countsProgressBar?.RenderCounts(setNumber, cachedCount);
            countsProgressBar?.UpdateCountSubtextsForSet(setNumber);
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
                director.SessionLoader.RuntimeCache.SetTimingMap[currentSetIndex].count = parsedCount;
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
                director.SessionLoader.RuntimeCache.SetTimingMap[currentSetIndex].startBPM = parsedStartBPM;
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
                director.SessionLoader.RuntimeCache.SetTimingMap[currentSetIndex].endBPM = parsedEndBPM;
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
        if (director.SessionLoader.RuntimeCache.SetTimingMap.TryGetValue(setIndex, out var timing))
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
        director.SessionLoader.ShowState.LastSet = setNumber.ToString();

        for (int i = 0; i < setButtonWrappers.Count; i++)
        {
            Text buttonText = setButtonWrappers[i].button.GetComponentInChildren<Text>();
            bool isActive = int.TryParse(buttonText.text, out int buttonSetNumber) && buttonSetNumber == setNumber;

            if (setButtonWrappers[i].highlightOverlay != null)
                setButtonWrappers[i].highlightOverlay.SetActive(isActive);
        }
    }


    public void UpdateSetProgressColor(int setIndex, float percent)
    {
        if (setIndex < 1 || setIndex > setButtonWrappers.Count) return;

        Color color;
        if (percent >= 0.999f)
        {
            color = Color.white;
        }
        else if (percent > 0f)
        {
            Color softYellow = new Color(1f, 0.96f, 0.75f);  // pastel yellow
            Color brightYellow = new Color(1f, 1f, 0f);      // full yellow
            color = Color.Lerp(softYellow, brightYellow, percent);
        }
        else
        {
            color = new Color(0.7f, 0.7f, 0.7f); // soft grey
        }

        setButtonWrappers[setIndex - 1].button.image.color = color; 
    }

    private class SetButtonWrapper
    {
        public Button button;
        public GameObject highlightOverlay;
    }
}
