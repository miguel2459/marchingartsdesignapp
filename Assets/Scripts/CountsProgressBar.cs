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

    private List<GameObject> countButtons = new List<GameObject>();
    private int activeCountIndex = -1;
    public EnsembleDirector2 director; // Or set a reference

    private void Awake()
    {
        countButtons.Clear();

        foreach (Transform child in contentArea)
        {
            GameObject buttonObj = child.gameObject;

            if (buttonObj.GetComponent<Button>() != null)
            {
                countButtons.Add(buttonObj);
                //Debug.Log($"🔍 Found existing count button in content: {buttonObj.name}");
            }
        }

        //Debug.Log($"✅ Awake initialized {countButtons.Count} pre-existing count buttons.");
    }


    /// <summary>
    /// Renders the given number of counts as buttons.
    /// </summary>
    public void RenderCounts(int setNumber, int countTotal)
    {
        //Debug.Log($"🔄 RenderCounts() called for Set {setNumber} with {countTotal} counts");

        ClearCounts();

        float spacing = 10f;
        float viewportWidth = contentArea.parent.GetComponent<RectTransform>().rect.width;
        float availableWidth = viewportWidth - ((countTotal - 1) * spacing);

        int visibleCountLimit = 8;
        float buttonWidth;

        if (countTotal <= visibleCountLimit)
        {
            buttonWidth = availableWidth / countTotal;
            //Debug.Log($"🧮 Using full width: Button width = {buttonWidth:F2}");
        }
        else
        {
            float maxVisibleWidth = viewportWidth - ((visibleCountLimit - 1) * spacing);
            buttonWidth = maxVisibleWidth / visibleCountLimit;
            //Debug.Log($"🧮 Using limited width: Button width = {buttonWidth:F2}");
        }

        // Update layout spacing
        HorizontalLayoutGroup layout = contentArea.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = spacing;
            //Debug.Log($"📏 Horizontal spacing set to {spacing}");
        }

        for (int i = 0; i < countTotal; i++)
        {
            GameObject buttonObj = Instantiate(countButtonPrefab, contentArea);
            Text label = buttonObj.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.text = (i + 1).ToString();
            }

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(buttonWidth, rt.sizeDelta.y);
            }

            Image bg = buttonObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = defaultColor;
            }

            int countIndex = i;

            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                //Debug.Log($"🧠 Count Button Listener Triggered → Set {setNumber}, Count {countIndex + 1}");
                OnCountButtonClicked(setNumber, countIndex + 1);
                HighlightCount(countIndex); 
            });

            countButtons.Add(buttonObj);
        }

        //Debug.Log($"✅ Rendered {countButtons.Count} count buttons for Set {setNumber}");
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
        }
    }


    /// <summary>
    /// Clears all count buttons.
    /// </summary>
    public void ClearCounts()
    {
        foreach (var btn in countButtons)
        {
            Destroy(btn);
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

        // Clear previous highlight
        if (activeCountIndex >= 0 && activeCountIndex < countButtons.Count)
        {
            var img = countButtons[activeCountIndex].GetComponent<Image>();
            if (img != null) img.color = defaultColor;
        }

        // Apply new highlight
        var currentImg = countButtons[countIndex].GetComponent<Image>();
        if (currentImg != null)
        {
            currentImg.color = highlightColor;
        }

        activeCountIndex = countIndex;
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

    public void ForceUnhighlight()
    {
        if (activeCountIndex >= 0 && activeCountIndex < countButtons.Count)
        {
            var img = countButtons[activeCountIndex].GetComponent<Image>();
            if (img != null) img.color = defaultColor;
        }

        activeCountIndex = -1;
    }


    /// <summary>
    /// Optional reset for when the metronome ends.
    /// </summary>
    public void ResetHighlight()
    {
        if (activeCountIndex >= 0 && activeCountIndex < countButtons.Count)
        {
            var img = countButtons[activeCountIndex].GetComponent<Image>();
            if (img != null) img.color = defaultColor;
        }
        activeCountIndex = -1;
    }
}
