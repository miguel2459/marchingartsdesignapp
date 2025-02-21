using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SetProgressBar : MonoBehaviour
{
    public GameObject sectionPrefab;  // Button prefab for each set
    public EnsembleDirector2 director;
    public ScrollRect scrollRect; // Reference to the Scroll View
    public List<Button> setButtons = new List<Button>();

    private int totalSets = 0;

    void Start()
    {
        // Attach a listener to update when input field changes
        director = FindObjectOfType<EnsembleDirector2>();
        totalSets = director.numberOfSets;
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
            Destroy(child.gameObject); // Destroy each child (button) under content
            Debug.Log("Destroying Button");
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

            // Optional: Set button label with set number
            Text buttonText = newSection.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (i + 1).ToString();
            }

            // Add click listener
            int setIndex = i; // Local copy of index for closure
            sectionButton.onClick.AddListener(() => OnSetButtonClick(setIndex));
        }

        // Adjust content width based on total sets
        RectTransform contentRect = scrollRect.content;
        float buttonWidth = sectionPrefab.GetComponent<RectTransform>().sizeDelta.x;
        contentRect.sizeDelta = new Vector2(totalSets * (buttonWidth + 10), contentRect.sizeDelta.y);
    }

    // Handle click on a set button
    private void OnSetButtonClick(int setIndex)
    {
        Debug.Log($"Clicked on set {setIndex + 1}");
        // Handle set selection, e.g., update the active set in your director
    }
}
