using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SphereResetController : MonoBehaviour
{
    public Button resetButton;                  // Button to reset spheres or marchers
    public SelectedMarchers marcherSelector;     // Reference to the MarcherSelector script
    //public SphereSelector sphereSelector;       // Reference to the SphereSelector script

    void Start()
    {
        // Add listener to the reset button
        //resetButton.onClick.AddListener(ResetSelectedItems);
        marcherSelector = FindObjectOfType<SelectedMarchers>();
    }

    void Update()
    {
        // Enable the reset button only if a set sphere is selected or if marchers are selected
        //resetButton.interactable = marcherSelector.selectedMarchers.Count > 0 || sphereSelector.selectedSpheres.Count > 0;
    }

    // Reset function triggered by the button

}
