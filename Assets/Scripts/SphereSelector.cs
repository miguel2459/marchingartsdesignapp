using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // Include this namespace for EventSystem

public class SphereSelector : MonoBehaviour
{
    public Color highlightColor = Color.cyan;
    public LayerMask sphereLayer; // Layer mask for set spheres
    public SelectedMarchers marcherSelector; // Reference to the MarcherSelector script

    private Camera cam;
    public List<GameObject> selectedSpheres = new List<GameObject>(); // List of selected set spheres

    void Start()
    {
        cam = Camera.main;

        // Ensure the MarcherSelector reference is assigned
        if (marcherSelector == null)
        {
            marcherSelector = FindObjectOfType<SelectedMarchers>();
        }
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        // Check if the mouse is over a UI element
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // Return early if the mouse is over UI to prevent deselection
            return;
        }

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, sphereLayer))
            {
                GameObject hitObject = hit.collider.gameObject;

                // Check if the sphere is set and handle selection
                if (hitObject.CompareTag("SetSphere"))
                {
                    // Deselect any marchers if a sphere is selected
                    marcherSelector.ClearSelection();
                    

                    // Toggle selection of the sphere
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        // Allow multiple selections with Shift
                        if (selectedSpheres.Contains(hitObject))
                        {
                            DeselectSphere(hitObject);
                        }
                        else
                        {
                            SelectSphere(hitObject);
                        }
                    }
                    else
                    {
                        // Clear previous selection unless Shift is held
                        ClearSphereSelection();
                        SelectSphere(hitObject);
                    }
                }
            }
            else
            {
                // Clear selection if clicking outside of spheres
                ClearSphereSelection();
            }
        }
    }

    // Select a specific set sphere
    public void SelectSphere(GameObject sphere)
    {
        if (!selectedSpheres.Contains(sphere))
        {
            selectedSpheres.Add(sphere);
            sphere.GetComponent<Renderer>().material.color = highlightColor; // Highlight the selected sphere
            Debug.Log($"SphereSelector: Sphere {sphere.name} selected and highlighted.");
        }
    }

    public void DeselectSphere(GameObject sphere)
    {
        if (selectedSpheres.Contains(sphere))
        {
            selectedSpheres.Remove(sphere);

            // Extract the marcher name from the sphere's name
            string sphereName = sphere.name;
            string marcherName = sphereName.Split('-')[0].Trim();
            Debug.Log("marcher's name:" + marcherName + "   sphere's name:" + sphereName);

            // Find the marcher object in the scene
            GameObject marcherObject = GameObject.Find(marcherName);

            if (marcherObject != null)
            {
                MarcherPositionsManager marcherManager = marcherObject.GetComponent<MarcherPositionsManager>();

                if (marcherManager != null)
                {
                    // Ensure we are looking for the sphere in the correct dictionary
                    if (marcherManager.sphereOriginalColors.ContainsKey(sphere))
                    {
                        // Restore the original color
                        Color originalColor = marcherManager.sphereOriginalColors[sphere];
                        sphere.GetComponent<Renderer>().material.color = originalColor;
                        Debug.Log($"SphereSelector: Sphere {sphere.name} deselected and color reverted to {originalColor}.");
                    }
                    else
                    {
                        Debug.LogWarning($"SphereSelector: Original color not found for {sphere.name}. Dictionary entry missing.");
                    }
                }
                else
                {
                    Debug.LogWarning($"SphereSelector: No MarcherPositionsManager found on {marcherName}.");
                }
            }
            else
            {
                Debug.LogWarning($"SphereSelector: No marcher object found with name {marcherName}.");
            }
        }
    }

    // Clear all selected spheres using a copy of the list
    public void ClearSphereSelection()
    {
        // Create a copy of the list to iterate over
        var spheresCopy = new List<GameObject>(selectedSpheres);

        foreach (var sphere in spheresCopy)
        {
            DeselectSphere(sphere);
        }

        selectedSpheres.Clear(); // Clear the list after deselecting all spheres
        //Debug.Log("SphereSelector: All spheres deselected.");
    }

}
