using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SetProgressBar : MonoBehaviour
{
    public GameObject sectionPrefab;  // Button prefab for each set
    public EnsembleDirector2 director;
    public ScrollRect scrollRect; // Reference to the Scroll View
    public Text currentSetText; // UI Text to display the selected set
    public Color selectedColor = new Color(0.7f, 0.85f, 1f); // Light blue
    public Color defaultColor = Color.white;

    public List<Button> setButtons = new List<Button>();

    private int totalSets = 0;
    private int lastSet = 0; // Track last selected set

    void Start()
    {
        // Attach a listener to update when input field changes
        director = FindObjectOfType<EnsembleDirector2>();
        totalSets = director.numberOfSets;

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
        // Clear existing buttons
        foreach (Transform child in scrollRect.content)
        {
            Destroy(child.gameObject);
        }
        setButtons.Clear();
    }

    // Updates the bar with the correct number of set buttons
    private void UpdateSetBar()
    {
        ClearButtons();

        // Populate with the current number of sets
        for (int i = 0; i < totalSets; i++)
        {
            GameObject newSection = Instantiate(sectionPrefab, scrollRect.content.transform);
            Button sectionButton = newSection.GetComponent<Button>();
            setButtons.Add(sectionButton);

            // Set button label with set number
            Text buttonText = newSection.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (i + 1).ToString();
            }

            // Add click listener
            int setIndex = i + 1; // Convert zero-based index to one-based set number
            sectionButton.onClick.AddListener(() => OnSetButtonClick(setIndex));
        }

        // Adjust content width based on total sets
        RectTransform contentRect = scrollRect.content;
        float buttonWidth = sectionPrefab.GetComponent<RectTransform>().sizeDelta.x;
        contentRect.sizeDelta = new Vector2(totalSets * (buttonWidth + 10), contentRect.sizeDelta.y);

        // Load last selected set (if available)
        HighlightSet(lastSet);
    }

    private void LoadLastSet()
    {
        if (SessionManager.instance != null)
        {
            lastSet = int.Parse(SessionManager.instance.lastSet);
        }
        else
        {
            lastSet = 1; // Default to set 1
        }
    }

    private void OnSetButtonClick(int setNumber)
    {
        Debug.Log($"Clicked on set {setNumber}");

        // Update UI Text
        if (currentSetText != null)
        {
            currentSetText.text = setNumber.ToString();
        }

        // Highlight selected button
        HighlightSet(setNumber);

        // Store last selected set in SessionManager
        if (SessionManager.instance != null)
        {
            SessionManager.instance.lastSet = setNumber.ToString();
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
