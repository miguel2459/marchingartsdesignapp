using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteInEditMode] // Allows the script to run in the Unity Editor outside of Play mode
public class EnsembleDirector2 : MonoBehaviour
{
    public int numberOfMarchers; // Number of marchers to create
    public int numberOfSets; // Total number of sets
    public int countsPerSet; // Number of counts per set
    public float bpm; // Beats per minute for the metronome
    public float interval;
    public GameObject marcherPrefab; // Prefab of the marcher
    public GameObject positionSpherePrefab; // Prefab for the position spheres
    public ShapeMarchers shapeMarchers; // Reference to ShapeMarchers script
    public ShapeGroup shapeGroup;
    public ShapeUIManager shapeUI;
    public IntervalManager intervalManager;

    public InputField numberOfMarchersInputField; // UI InputField for number of marchers
    public InputField numberOfSetsInputField; // UI InputField for number of sets
    public InputField countsPerSetInputField; // UI InputField for counts per set
    public InputField bpmInputField; // UI InputField for BPM
    public InputField intervalField;

    public Metronome2 metronome; // Reference to the Metronome script
    public SnapToGridLines snapToGrid;
    public SetProgressBar setBar;

    public List<MarcherPositionsManager> marchers; // List of instantiated marchers

    void Start()
    {
        // Initialize the input fields with current values
        numberOfMarchersInputField.text = numberOfMarchers.ToString();
        numberOfSetsInputField.text = numberOfSets.ToString();
        countsPerSetInputField.text = countsPerSet.ToString();
        bpmInputField.text = bpm.ToString();
        intervalField.text = interval.ToString();

        snapToGrid = FindObjectOfType<SnapToGridLines>();

        // Initialize ShapeMarchers
        shapeMarchers = FindObjectOfType<ShapeMarchers>();
        shapeUI = FindObjectOfType<ShapeUIManager>();
        intervalManager = FindObjectOfType<IntervalManager>();
        setBar = FindObjectOfType<SetProgressBar>();
        shapeMarchers.InitializeShapeManagers(marcherPrefab, positionSpherePrefab, interval);
        setBar.OnTotalSetsChanged(numberOfSets);
    }

    public void ApplyUpdates()
    {
        // Retrieve and validate new values from input fields
        int updatedNumberOfMarchers = Mathf.Max(1, int.Parse(numberOfMarchersInputField.text));
        int updatedNumberOfSets = Mathf.Max(1, int.Parse(numberOfSetsInputField.text));
        int updatedCountsPerSet = Mathf.Max(1, int.Parse(countsPerSetInputField.text)); // Ensure at least one count per set
        float updatedBpm = Mathf.Clamp(float.Parse(bpmInputField.text), 20f, 300f); // Clamp BPM to a reasonable range
        float updateInterval = Mathf.Clamp(float.Parse(intervalField.text),1f, 4f);


        // Apply new values
        numberOfMarchers = updatedNumberOfMarchers;
        numberOfSets = updatedNumberOfSets;
        countsPerSet = updatedCountsPerSet;
        bpm = updatedBpm;
        interval = updateInterval;

        setBar.OnTotalSetsChanged(numberOfSets);


        // Update the marchers and metronome settings based on the new values
        PopulateMarchers();
        metronome.UpdateBPM(bpm); // Update BPM in the metronome

        Debug.Log($"Updated: Number of Marchers = {numberOfMarchers}, Number of Sets = {numberOfSets}, Counts per Set = {countsPerSet}, BPM = {bpm}");
    }


    // Populate marchers based on the current settings
    public void PopulateMarchers()
    {
        // Find all existing marchers in the scene
        marchers = new List<MarcherPositionsManager>(GetComponentsInChildren<MarcherPositionsManager>());

        int currentNumberOfMarchers = marchers.Count;

        // Compare current number of marchers with the desired number
        if (currentNumberOfMarchers < numberOfMarchers)
        {
            // Add more marchers
            for (int i = currentNumberOfMarchers; i < numberOfMarchers; i++)
            {
                CreateMarcher(i, new Color(Random.value, Random.value, Random.value), numberOfSets);
            }
        }
        else if (currentNumberOfMarchers > numberOfMarchers)
        {
            // Remove excess marchers
            for (int i = currentNumberOfMarchers - 1; i >= numberOfMarchers; i--)
            {
                DestroyImmediate(marchers[i].gameObject);
                marchers.RemoveAt(i);
            }
        }

        // Update the number of sets and counts per set for all existing marchers
        UpdateMarcherSets();

        // Ensure the number of marchers in the inspector matches the actual number in the scene
        numberOfMarchers = marchers.Count;

        if (shapeMarchers != null)
        {
            // Initialize shapeMarchers.marchers and copy over the GameObjects from marchers
            shapeGroup.marchers = new List<GameObject>(marchers.Count);
            foreach (var marcher in marchers)
            {
                shapeGroup.marchers.Add(marcher.gameObject);
                Debug.Log($"Added marcher: {marcher.gameObject.name} to ShapeMarchers list.");
            }

            // Set the default shape, e.g., a box shape
            shapeMarchers.ArrangeFormation(
                shapeUI.GetCurrentShape(),  // We need to get latest shape update from ShapeUIManager
                intervalManager.GetIntervalType(interval), shapeGroup.marchers  //We need to get latest interval update from inputfield then from interval manager
            );
        }
        else
        {
            Debug.LogWarning("ShapeMarchers is null; cannot initialize or arrange formation.");
        }
    }

    void CreateMarcher(int index, Color color, int sets)
    {
        // Instantiate the marcher at the snapped position
        GameObject marcherObject = Instantiate(marcherPrefab, Vector3.zero, Quaternion.identity, transform);
        marcherObject.name = "Marcher" + (index + 1); // Name the marcher for identification

        // Get the marcher controller and initialize it
        MarcherPositionsManager marcher = marcherObject.GetComponent<MarcherPositionsManager>();
        marcher.Initialize(sets, countsPerSet, color, positionSpherePrefab);

        // Name the position spheres to link them to their marcher
        for (int i = 1; i < sets; i++)
        {
            marcher.PositionSpheres[i].name = marcherObject.name + " - Position " + (i + 1);
        }

        marchers.Add(marcher);
    }

    void UpdateMarcherSets()
    {
        foreach (var marcher in marchers)
        {
            int currentSets = marcher.PositionSpheres.Length;

            // Calculate how many spheres need to be added or removed
            int difference = numberOfSets - currentSets;

            // Determine the color to use for new spheres
            Color sphereColor = GetMarcherColor(marcher);

            if (difference > 0)
            {
                // Add new position spheres
                for (int i = currentSets; i < numberOfSets; i++)
                {
                    marcher.AddPositionSphere(i, positionSpherePrefab, sphereColor);
                }
            }
            else if (difference < 0)
            {
                // Remove excess position spheres
                for (int i = currentSets - 1; i >= numberOfSets; i--)
                {
                    marcher.RemovePositionSphere(i);
                }
            }
        }
    }

    Color GetMarcherColor(MarcherPositionsManager marcher)
    {
        // First, check the PositionSpheres array for a valid sphere
        foreach (var sphere in marcher.PositionSpheres)
        {
            if (sphere != null)
            {
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Use sharedMaterial instead of material to avoid creating new instances
                    return renderer.sharedMaterial.color;
                }
            }
        }

        // If no valid sphere found in PositionSpheres, check setSpheres
        foreach (var sphere in marcher.setSpheres)
        {
            if (sphere != null)
            {
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Use sharedMaterial here as well
                    return renderer.sharedMaterial.color;
                }
            }
        }

        // Default color if no sphere is found (adjust as needed)
        return Color.white;
    }
}
