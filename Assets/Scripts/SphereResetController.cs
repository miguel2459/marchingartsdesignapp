using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SphereResetController : MonoBehaviour
{
    public Button resetButton;                  // Button to reset spheres or marchers
    public SelectedMarchers marcherSelector;     // Reference to the MarcherSelector script
    public SphereSelector sphereSelector;       // Reference to the SphereSelector script

    void Start()
    {
        // Add listener to the reset button
        resetButton.onClick.AddListener(ResetSelectedItems);
        marcherSelector = FindObjectOfType<SelectedMarchers>();
    }

    void Update()
    {
        // Enable the reset button only if a set sphere is selected or if marchers are selected
        resetButton.interactable = marcherSelector.selectedMarchers.Count > 0 || sphereSelector.selectedSpheres.Count > 0;
    }

    // Reset function triggered by the button
    void ResetSelectedItems()
    {
        if (marcherSelector.selectedMarchers.Count > 0)
        {
            // Reset all position spheres for the selected marchers
            foreach (var marcher in marcherSelector.selectedMarchers)
            {
                MarcherPositionsManager marcherManager = marcher.GetComponent<MarcherPositionsManager>();
                if (marcherManager != null)
                {
                    marcherManager.ResetAllSetSpheres(); // Reset all set spheres for the marcher
                }
                Debug.Log("Reset function on selected marcher(s)");
            }
        }
        else if (sphereSelector.selectedSpheres.Count > 0)
        {
            // Reset specific selected spheres
            foreach (var sphere in sphereSelector.selectedSpheres)
            {
                // Extract the marcher name and position from the sphere's name
                string sphereName = sphere.name;
                string[] nameParts = sphereName.Split('-');

                // Extract the marcher name and trim any extra spaces
                string marcherName = nameParts[0].Trim();

                // Initialize marcherPosition with a default value
                int marcherPosition = -1;

                if (nameParts.Length > 1)
                {
                    // Extract the position part and split by space to find the position number
                    string positionPart = nameParts[1].Trim(); // e.g., "Position 1"
                    string[] positionParts = positionPart.Split(' ');

                    if (positionParts.Length > 1 && int.TryParse(positionParts[1], out int positionNumber))
                    {
                        marcherPosition = positionNumber; // Parse the position number
                    }
                }

                Debug.Log($"Marcher's name: {marcherName}, Sphere's name: {sphereName}, Marcher position: {marcherPosition}");

                // Find the marcher object in the scene
                MarcherPositionsManager marcherManager = GameObject.Find(marcherName).GetComponent<MarcherPositionsManager>();
                if (marcherManager != null)
                {
                    int index = System.Array.IndexOf(marcherManager.setSpheres, sphere);
                    if (index >= 0)
                    {
                        marcherManager.ResetSetSphere(index, marcherPosition); // Reset the specific set sphere
                    }
                }
                Debug.Log("Reset function on selected position sphere(s)");
            }
            //sphereSelector.ClearSphereSelection(); // Clear sphere selection after reset
        }
        //else
        //{
        //    // Reset all set spheres for all marchers in the scene
        //    foreach (var marcherManager in FindObjectsOfType<MarcherPositionsManager>())
        //    {
        //        marcherManager.ResetAllSetSpheres(); // Reset all set spheres for each marcher
        //    }
        //}
    }
}
