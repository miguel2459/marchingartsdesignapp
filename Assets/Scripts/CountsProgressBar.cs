using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CountsProgressBar : MonoBehaviour
{
    public GameObject countButtonPrefab; // Prefab for a single count button
    public RectTransform contentArea; // The container for all buttons
    public Color defaultColor = Color.white;
    public Color highlightColor = new Color(1f, 0.8f, 0.3f); // Soft yellow

    private List<GameObject> countButtons = new List<GameObject>();
    private int activeCountIndex = -1;

    /// <summary>
    /// Renders the given number of counts as buttons.
    /// </summary>
    public void RenderCounts(int setNumber, int countTotal)
    {
        ClearCounts();

        for (int i = 0; i < countTotal; i++)
        {
            GameObject buttonObj = Instantiate(countButtonPrefab, contentArea);
            Text label = buttonObj.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.text = (i + 1).ToString();
            }

            Image bg = buttonObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = defaultColor;
            }

            countButtons.Add(buttonObj);
        }

        // Optionally adjust container size based on number of buttons
        float buttonWidth = countButtonPrefab.GetComponent<RectTransform>().sizeDelta.x;
        contentArea.sizeDelta = new Vector2(countTotal * (buttonWidth + 10), contentArea.sizeDelta.y);
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
