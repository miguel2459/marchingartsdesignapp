using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CountsProgressBar : MonoBehaviour
{
    SessionManager session = SessionManager.instance;
    public GameObject countButtonPrefab; // Prefab for a single count button
    public RectTransform contentArea; // The container for all buttons
    public Color defaultColor = Color.white;
    public Color highlightColor = new Color(0.843f, 0.510f, 0.973f, 1.0f);// Soft pinkish purple
    [Header("Count‑Progress Colors")]
    [Tooltip("All marchers ↦ white")]
    [SerializeField] private Color countFullProgressColor    = Color.white;
    [Tooltip("Some marchers ↦ pastel yellow")]
    [SerializeField] private Color countPartialProgressColor = new Color(1f, 0.96f, 0.75f);
    [Tooltip("No marchers ↦ grey")]
    [SerializeField] private Color countNoProgressColor      = new Color(0.6f, 0.6f, 0.6f);

    private List<CountButtonWrapper> countButtons = new List<CountButtonWrapper>();

    private int activeCountIndex = -1;
    public MonoBehaviour directorObject;          // assign the same object in Inspector
    private IMarcherProvider director;           // cached cast
    public EnsembleDirector2 ensemble;
    private int currentSetNumber;
    private int renderedSetNumber = -1;
    [SerializeField] private TransformGizmoManager transformGizmoManager;
    [SerializeField] private MarcherPositionHistory positionHistory;

    private void Awake()
    {
        director = directorObject as IMarcherProvider;
        countButtons.Clear();

        foreach (Transform child in contentArea)
        {
            GameObject buttonObj = child.gameObject;

            if (buttonObj.TryGetComponent(out Button btn))
            {
                TextMeshProUGUI[] labels = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();
                TextMeshProUGUI main = null;
                TextMeshProUGUI sub = null;

                foreach (var label in labels)
                {
                    if (label.name.Contains("Count Text")) main = label;
                    else if (label.name.Contains("SubText")) sub = label;
                }

                int countIndex = countButtons.Count + 1;

                countButtons.Add(new CountButtonWrapper
                {
                    buttonObj = buttonObj,
                    mainText = main,
                    subText = sub,
                    countIndex = countIndex
                });

                // Debug.Log($"✅ Rehydrated CountButton {countIndex} from existing child: {buttonObj.name}");
            }
        }
        //Debug.Log($"✅ Awake initialized {countButtons.Count} pre-existing count buttons.");
    }


    /// <summary>
    /// Renders the given number of counts as buttons.
    /// </summary>
    public void RenderCounts(int setNumber, int countTotal)
    {
        if (setNumber == renderedSetNumber && countButtons.Count == countTotal)
        {
            // Already rendered — just update progress and subtexts
            UpdateCountProgressColors(setNumber);
            UpdateCountSubtextsForSet(setNumber);
            return;
        }

        renderedSetNumber = setNumber;
        currentSetNumber = setNumber;

        ClearCounts(); // only if not already rendered

        float spacing = 10f;
        float viewportWidth = contentArea.parent.GetComponent<RectTransform>().rect.width;
        float availableWidth = viewportWidth - ((countTotal - 1) * spacing);

        int visibleCountLimit = 8;
        float buttonWidth;

        if (countTotal <= visibleCountLimit)
            buttonWidth = availableWidth / countTotal;
        else
            buttonWidth = (viewportWidth - ((visibleCountLimit - 1) * spacing)) / visibleCountLimit;

        HorizontalLayoutGroup layout = contentArea.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;

        for (int i = 0; i < countTotal; i++)
        {
            GameObject buttonObj = Instantiate(countButtonPrefab, contentArea);
            TextMeshProUGUI[] labels = buttonObj.GetComponentsInChildren<TextMeshProUGUI>();

            TextMeshProUGUI main = null;
            TextMeshProUGUI sub = null;

            foreach (var label in labels)
            {
                if (label.name.Contains("Count Text")) main = label;
                else if (label.name.Contains("Sub Text")) sub = label;
            }

            if (main != null) main.text = (i + 1).ToString();
            if (sub != null)
            {
                sub.text = "";
                sub.gameObject.SetActive(true);
            }

            buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(buttonWidth, buttonObj.GetComponent<RectTransform>().sizeDelta.y);
            buttonObj.GetComponent<Image>().color = defaultColor;

            int countIndex = i;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnCountButtonClicked(setNumber, countIndex + 1);
            });

            countButtons.Add(new CountButtonWrapper
            {
                buttonObj = buttonObj,
                mainText = main,
                subText = sub,
                countIndex = countIndex + 1
            });
        }

        UpdateCountProgressColors(setNumber);
        UpdateCountSubtextsForSet(setNumber);
    }


    public void UpdateCountProgressColors(int setNumber)
    {
        if (director == null) return;
        int totalMarchers = director.Marchers.Count;

        foreach (var wrapper in countButtons)
        {
            if (wrapper.countIndex - 1 == activeCountIndex)
                continue;

            int countIndex = wrapper.countIndex;
            int confirmed = 0;

            // tally confirmed/inferred positions at this count
            foreach (var marcher in director.Marchers)
            {
                if (marcher.countPositions.TryGetValue(setNumber, out var setData) &&
                    setData.TryGetValue(countIndex, out var entry) &&
                    (entry.IsConfirmed || entry.IsInferred))
                {
                    confirmed++;
                }
            }

            var img = wrapper.buttonObj.GetComponent<Image>();
            if (img == null) continue;

            if (confirmed == 0)
                img.color = countNoProgressColor;
            else if (confirmed < totalMarchers)
                img.color = countPartialProgressColor;
            else
                img.color = countFullProgressColor;
        }
    }
    public int GetActiveCountIndex()
    {
        return activeCountIndex;
    }

    public void OnCountButtonClicked(int setNumber, int clickedCount)
    {
        if (director != null)
        {
            //Debug.Log($"🎯 Count Button Clicked — Set: {setNumber}, Count: {clickedCount}");
            director.PreviewCountPosition(setNumber, clickedCount);
            if (transformGizmoManager.HasActiveGizmo)
                transformGizmoManager.HideTransformGizmo();

            foreach (var marcher in director.Marchers)
            {
                if (marcher.TryGetComponent(out Unit unit))
                {
                    bool hasConfirmedPosition = marcher.TryGetConfirmedPosition(setNumber, clickedCount, out _);


                    marcher.GetComponent<MarcherVisualStateController>()?.ApplyHoldColorIfEligible(setNumber, clickedCount);
                    
                    bool isHolding = marcher.IsHoldingAtCount(setNumber, clickedCount);
                    unit.SetSelector(hasConfirmedPosition, isHolding);
                }
            }
            HighlightCount(clickedCount-1);
            positionHistory.SetActiveEditContext(setNumber, clickedCount);
            ensemble.selectedMarchers.ReCacheAnchorsForSelected();
        }        
    }


    /// <summary>
    /// Clears all count buttons.
    /// </summary>
    public void ClearCounts()
    {
        foreach (var wrapper in countButtons)
        {
            if (wrapper.buttonObj != null)
                Destroy(wrapper.buttonObj);
        }
        countButtons.Clear();
        activeCountIndex = -1;
    }

    /// <summary>
    /// Highlights a specific count button during metronome playback.
    /// </summary>
    public void HighlightCount(int countIndex)
    {
         if (countIndex < 0 || countIndex >= countButtons.Count) return;
 
         // 1) Mark this as the new active index *before* recoloring
         activeCountIndex = countIndex;
 
         // 2) Recolor all buttons except the new active one
         UpdateCountProgressColors(currentSetNumber);
 
         // 3) Finally, paint the new button purple
         var currentImg = countButtons[countIndex].buttonObj.GetComponent<Image>();
         if (currentImg != null)
             currentImg.color = highlightColor;
    }

    public void EnsureSetTimingDefaults(int totalSets)
    {
        var map = session.runtimeCacheSO.SetTimingMap;

        // Ensure default timing for all sets up to totalSets
        for (int i = 1; i <= totalSets; i++)
        {
            if (!map.ContainsKey(i))
            {
                map[i] = new RuntimeCacheSO.SetTimingData(i, 8, 140f, 140f);
                //Debug.Log($"🆕 Default timing added for Set {i}: 8 counts @ 140 BPM");
            }
        }

        // Get the current set number from SessionState
        if (int.TryParse(session.showStateSO.LastSet, out int currentSet))
        {
            if (map.TryGetValue(currentSet, out var timing))
            {
                RenderCounts(currentSet, timing.count); // ✅ Initialize the visual
            }
        }
        else
        {
            //Debug.LogWarning("⚠️ Could not parse current set from SessionState.LastSet.");
        }
    }

    public void UpdateCountSubtextsForSet(int setIndex)
    {
        if (director == null) return;
        if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing)) return;

        foreach (var wrapper in countButtons)
        {
            int countIndex = wrapper.countIndex;
            int confirmed = 0;

            foreach (var marcher in director.Marchers)
            {
                if (marcher.countPositions.TryGetValue(setIndex, out var setData))
                {
                    if (setData.TryGetValue(countIndex, out var entry) && entry.IsConfirmed)
                        confirmed++;
                }
            }

            if (wrapper.subText != null)
            {
                if (confirmed > 0)
                {
                    wrapper.subText.text = confirmed.ToString();
                    wrapper.subText.gameObject.SetActive(true);
                }
                else
                {
                    wrapper.subText.text = "";
                    wrapper.subText.gameObject.SetActive(false); // Optional: hide completely
                }
            }
        }
    }
    /// <summary>
    /// Optional reset for when the metronome ends.
    /// </summary>
    public void ResetHighlight()
    {
        if (activeCountIndex >= 0 && activeCountIndex < countButtons.Count)
        {
            var img = countButtons[activeCountIndex].buttonObj.GetComponent<Image>();
            if (img != null) img.color = defaultColor;
        }
        activeCountIndex = -1;
    }

    private class CountButtonWrapper
    {
        public GameObject buttonObj;
        public TextMeshProUGUI mainText;
        public TextMeshProUGUI subText;
        public int countIndex;
    }
}
