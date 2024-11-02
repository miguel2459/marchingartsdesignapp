using System;
using System.Collections.Generic;
using UnityEngine;

public class MarcherPositionsManager : MonoBehaviour
{
    public GameObject[] PositionSpheres; // Public property to access the position spheres
    public GameObject[] setSpheres;
    public Vector3[] positions; // The positions for each set
    public bool[] isSet; // Array to track if each position sphere has been set
    public bool[] responsibilities; // Array to hold the responsibilities for each set (true = travel, false = hold)

    public Dictionary<GameObject, Color> sphereOriginalColors = new Dictionary<GameObject, Color>(); // Store original colors of spheres

    private int totalSets;
    private int currentSet;

    public void Initialize(int sets, int counts, Color color, GameObject spherePrefab)
    {
        totalSets = sets;
        positions = new Vector3[sets];
        PositionSpheres = new GameObject[sets];
        setSpheres = new GameObject[sets];
        isSet = new bool[sets]; // Initialize the isSet array
        responsibilities = new bool[sets]; // Initialize the responsibilities array

        // Create position spheres and set their color
        for (int i = 0; i < sets; i++)
        {
            AddPositionSphere(i, spherePrefab, color);
            responsibilities[i] = true; // Default to travel (you can set this based on your needs)
            isSet[i] = false; // Initially, no sphere is set
        }

        // Set the marcher's position to the first position
        //transform.position = positions[0];
        currentSet = 0;
    }

    public void AddPositionSphere(int index, GameObject spherePrefab, Color color)
    {
        Debug.Log("inside Add PositionShpere_MarcherPositionManager");
        // Resize the positions and PositionSpheres arrays to accommodate the new sphere
        Array.Resize(ref positions, positions.Length + 1);
        Array.Resize(ref PositionSpheres, PositionSpheres.Length + 1);
        Array.Resize(ref setSpheres, setSpheres.Length + 1);
        Array.Resize(ref isSet, isSet.Length + 1);
        Array.Resize(ref responsibilities, responsibilities.Length + 1);

        // Shift the existing elements up in the arrays to make space for the new sphere at the specified index
        for (int i = positions.Length - 1; i > index; i--)
        {
            positions[i] = positions[i - 1];
            PositionSpheres[i] = PositionSpheres[i - 1];
            isSet[i] = isSet[i - 1]; // Shift isSet values as well
            responsibilities[i] = responsibilities[i - 1]; // Shift responsibility values as wellif (PositionSpheres[i] != null)

            if (PositionSpheres[i] != null)
            {
                PositionSpheres[i].name = $"{transform.name} - Position {i + 1}";
            }
        }

        // Instantiate the new sphere
        GameObject sphere = Instantiate(spherePrefab, transform);

        // Set the sphere's color
        sphere.GetComponent<Renderer>().material.color = color;
        sphereOriginalColors[sphere] = color; // Store the original color


        // Position the sphere along the negative z-axis (front)
        Vector3 localPosition = new Vector3(0, 0, 0); // Adjust the distance between positions as needed
        sphere.transform.localPosition = localPosition; // Local position relative to the marcher

        // Store the new sphere's position and add it to the arrays
        positions[index] = sphere.transform.localPosition;
        PositionSpheres[index] = sphere;
        isSet[index] = false; // New sphere is not set by default
        responsibilities[index] = true; // Default to travel for new set (you can change this as needed)// Set the name of the new sphere

        // Set the name of the new sphere
        sphere.name = $"{transform.name} - Position {index + 1}";
        sphere.gameObject.SetActive(false);
    }

    public void RemovePositionSphere(int index)
    {
        if (index >= 0 && index < PositionSpheres.Length)
        {
            if (PositionSpheres[index] != null)
            {
                DestroyImmediate(PositionSpheres[index]);
            }

            // Shift the elements after the removed index down by one position
            for (int i = index; i < PositionSpheres.Length - 1; i++)
            {
                positions[i] = positions[i + 1];
                PositionSpheres[i] = PositionSpheres[i + 1];
                setSpheres[i] = setSpheres[i + 1];
                isSet[i] = isSet[i + 1]; // Shift isSet values as well
                responsibilities[i] = responsibilities[i + 1]; // Shift responsibility values as well// Update the name of the shifted spheres to reflect their new indexif (PositionSpheres[i] != null)

                // Update the name of the shifted spheres to reflect their new index
                if (PositionSpheres[i] != null)
                {
                    PositionSpheres[i].name = $"{transform.name} - Position {i + 1}";
                }
            }

            // Resize the arrays to remove the last element, which is now duplicated
            Array.Resize(ref positions, positions.Length - 1);
            Array.Resize(ref PositionSpheres, PositionSpheres.Length - 1);
            Array.Resize(ref setSpheres, setSpheres.Length - 1);
            Array.Resize(ref isSet, isSet.Length - 1);
            Array.Resize(ref responsibilities, responsibilities.Length - 1);
        }
    }

    public void SetPositionSphere()
    {
        Debug.Log("Positions Manager: SetPositionSphere");

        // Iterate through all elements in the PositionSpheres array
        for (int i = 0; i < PositionSpheres.Length; i++)
        {
            // Check if the current element is not null
            if (PositionSpheres[i] != null)
            {
                // Set the position of the current position sphere to the current position of the marcher
                positions[i] = transform.position;
                PositionSpheres[i].transform.position = transform.position;

                // Determine the name of the parent object for set spheres
                string setPositionsName = $"{gameObject.name}SetPositions";

                // Find the existing parent object or create a new one if it doesn't exist
                GameObject setPositionsParent = GameObject.Find(setPositionsName);
                if (setPositionsParent == null)
                {
                    setPositionsParent = new GameObject(setPositionsName);
                }

                // Unparent the sphere and set its new parent to the setPositionsParent object
                PositionSpheres[i].transform.SetParent(setPositionsParent.transform);

                // Find the "Set Positions Manager" object and parent the setPositionsParent to it
                GameObject setPositionsManager = GameObject.Find("Set Positions Manager");
                if (setPositionsManager != null)
                {
                    setPositionsParent.transform.SetParent(setPositionsManager.transform);
                }
                else
                {
                    Debug.LogWarning("Set Positions Manager not found in the scene. Creating setPositionsParent as a root object.");
                }

                // Mark the sphere as set
                isSet[i] = true;

                // Move the current position sphere to the corresponding index in the setSpheres array
                setSpheres[i] = PositionSpheres[i];
                setSpheres[i].gameObject.SetActive(true);

                // Ensure the set sphere is in the sphereOriginalColors dictionary
                if (!sphereOriginalColors.ContainsKey(setSpheres[i]))
                {
                    Color originalColor = setSpheres[i].GetComponent<Renderer>().material.color;
                    sphereOriginalColors[setSpheres[i]] = originalColor;
                    Debug.Log($"Added set sphere {setSpheres[i].name} to sphereOriginalColors with color: {originalColor}");
                }

                // Clear the current element in the PositionSpheres array and reset associated values
                PositionSpheres[i] = null;
                //positions[i] = Vector3.zero;
                //responsibilities[i] = false;

                Debug.Log($"{gameObject.name}: PositionSphere at index {i} set and moved to setSpheres array.");
                break;
            }
            else
            {
                Debug.Log($"{gameObject.name}: PositionSphere at index {i} is null, skipping.");
            }
        }

        LogSphereColors(); // Log the contents of the sphereOriginalColors dictionary
    }

    public void LogSphereColors()
    {
        Debug.Log("Logging sphere colors:");
        foreach (var kvp in sphereOriginalColors)
        {
            Debug.Log($"Sphere: {kvp.Key.name}, Color: {kvp.Value}");
        }
    }

    public void ResetAllSetSpheres()
    {
        for (int i = 0; i < setSpheres.Length; i++)
        {
            if (setSpheres[i] != null)
            {
                //ResetSetSphere(i);
            }
        }
    }

    // Resets a specific set sphere back to its original position sphere
    public void ResetSetSphere(int index, int position)
    {
        Debug.Log("Positions Manager: ResetSetSphere, index: " + index + " SetSpheres.Length: " + setSpheres.Length.ToString());
        if (index >= 0 && index < setSpheres.Length && setSpheres[index] != null)
        {           
            GameObject sphere = setSpheres[index];

            // Ensure the parent marcher is correctly identified
            Transform marcherTransform = transform; // Assuming this script is on the marcher object
            if (sphere.transform.parent != marcherTransform)
            {
                sphere.transform.SetParent(marcherTransform); // Reparent sphere back to the marcher
            }

            // Reset the position of the sphere relative to the marcher
            sphere.transform.localPosition = Vector3.zero;

            // Restore the original color if available
            if (sphereOriginalColors.ContainsKey(sphere))
            {
                sphere.GetComponent<Renderer>().material.color = sphereOriginalColors[sphere];
            }

            // Update arrays to reflect the reset state
            positions[index] = sphere.transform.localPosition;
            PositionSpheres[index] = sphere;  // Reset position sphere reference
            setSpheres[index] = null;         // Clear the set sphere since it's reset
            isSet[index] = false;             // Mark as not set

            sphere.gameObject.SetActive(false);

            Debug.Log($"{marcherTransform.name}: Set sphere {index + 1} reset to its original position and parented back.");

            // Ensure the lowest position sphere is at PositionSpheres[0]
            //EnsureLowestPositionFirst(position, sphere);
        }
    }

    // Function to ensure the lowest position sphere is always in PositionSpheres[0]
    private void EnsureLowestPositionFirst(int position, GameObject sphere)
    {
        // Loop through the PositionSpheres array to compare positions
        for (int i = 0; i < PositionSpheres.Length; i++)
        {
            if (PositionSpheres[i] != null)
            {
                // Extract the position from the sphere's name
                string sphereName = PositionSpheres[i].name;
                string[] nameParts = sphereName.Split('-');
                if (nameParts.Length > 1)
                {
                    string positionPart = nameParts[1].Trim(); // e.g., "Position 1"
                    string[] positionParts = positionPart.Split(' ');

                    if (positionParts.Length > 1 && int.TryParse(positionParts[1], out int currentPos))
                    {
                        // If the new position is lower than the current position at PositionSpheres[0], swap them
                        if (position < currentPos)
                        {
                            // Swap the current position sphere with the sphere at index 0
                            GameObject temp = PositionSpheres[0];
                            PositionSpheres[0] = PositionSpheres[i];
                            PositionSpheres[i] = temp;

                            Debug.Log($"Swapped sphere positions: {PositionSpheres[0].name} is now at index 0.");
                        }
                    }
                }
            }
        }
    }

}
