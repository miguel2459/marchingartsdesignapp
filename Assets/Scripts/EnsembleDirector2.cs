using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

[ExecuteInEditMode]
public class EnsembleDirector2 : MonoBehaviour
{
    [Header("Marcher Settings")]
    public int numberOfMarchers;
    public int numberOfSets;
    public int lastSet;
    public int countsPerSet;
    public float bpm;
    public float interval;
    public Vector3 fieldCenter;

    [Header("Prefabs")]
    public GameObject marcherPrefab;
    public GameObject positionSpherePrefab;

    [Header("UI References")]
    public InputField numberOfMarchersInputField;
    public InputField numberOfSetsInputField;
    public InputField countsPerSetInputField;
    public InputField bpmInputField;
    public InputField intervalField;

    [Header("Managers & Components")]
    public Metronome2 metronome;
    public SnapToGridLines snapToGrid;
    public SetProgressBar setBar;
    public ShapeMarchers shapeMarchers;
    public ShapeGroup shapeGroup;
    public ShapeUIManager shapeUI;
    public IntervalManager intervalManager;
    public FieldGridManager fieldManager;

    public List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();

    void Start()
    {
        SnapToGridLines.OnGridReady += OnGridReadyHandler; 
        if (SessionManager.instance != null) OnSessionReady();
    }

    void OnSessionReady()
    {
        InitializeSession();
        InitializeUI();
        ApplyUpdates();
        setBar.OnTotalSetsChanged(numberOfSets);
        shapeMarchers.InitializeShapeManagers(marcherPrefab, positionSpherePrefab, interval);
        fieldCenter = fieldManager.GetFieldCenter();
    }

    void OnGridReadyHandler()
    {
        PopulateMarchers();
    }

    private void InitializeSession(){
        numberOfMarchers = SessionManager.instance.numberOfMarchers;
        numberOfSets = SessionManager.instance.numberOfSets;
        lastSet = int.Parse(SessionManager.instance.lastSet);
    }

    private void InitializeUI()
    {
        numberOfMarchersInputField.text = numberOfMarchers.ToString();
        numberOfSetsInputField.text = numberOfSets.ToString();
        countsPerSetInputField.text = countsPerSet.ToString();
        bpmInputField.text = bpm.ToString();
        intervalField.text = interval.ToString();
    }

    public void ApplyUpdates()
    {
        numberOfMarchers = Mathf.Max(1, int.Parse(numberOfMarchersInputField.text));
        numberOfSets = Mathf.Max(1, int.Parse(numberOfSetsInputField.text));
        countsPerSet = Mathf.Max(1, int.Parse(countsPerSetInputField.text));
        bpm = Mathf.Clamp(float.Parse(bpmInputField.text), 20f, 300f);
        interval = Mathf.Clamp(float.Parse(intervalField.text), 1f, 4f);

        SessionManager.instance.numberOfMarchers = numberOfMarchers;
        SessionManager.instance.numberOfSets = numberOfSets;

        setBar.OnTotalSetsChanged(numberOfSets);
        metronome.UpdateBPM(bpm);

        Debug.Log($"Updated: Marchers = {numberOfMarchers}, Sets = {numberOfSets}, Counts = {countsPerSet}, BPM = {bpm}");
    }


    public void PopulateMarchers()
    {
        SnapToGridLines.OnGridReady -= PopulateMarchers; // Unsubscribe to prevent multiple calls
        // Retrieve the current list of marchers already present in the scene.
        marchers = new List<MarcherPositionsManager>(GetComponentsInChildren<MarcherPositionsManager>());
        int currentMarcherCount = marchers.Count;

        // Adjust the marcher count to match the user-specified number.
        if (currentMarcherCount < numberOfMarchers)
            AddMarchers(currentMarcherCount);
        else if (currentMarcherCount > numberOfMarchers)
            RemoveExcessMarchers(currentMarcherCount);

        // Ensure each marcher has the correct number of set positions.
        UpdateMarcherSets();

        // Arrange the newly created marchers in a square formation at the center of the field.
        ArrangeMarchersInSquare();
    }

    private void AddMarchers(int currentCount)
    {
        for (int i = currentCount; i < numberOfMarchers; i++)
            CreateMarcher(i, new Color(Random.value, Random.value, Random.value), numberOfSets);
    }

    private void RemoveExcessMarchers(int currentCount)
    {
        for (int i = currentCount - 1; i >= numberOfMarchers; i--)
        {
            DestroyImmediate(marchers[i].gameObject);
            marchers.RemoveAt(i);
        }
    }

    private void CreateMarcher(int index, Color color, int sets)
    {
        //determine position
        //Vector3 position = fieldCenter + new Vector3(col * spacing, 0, row * spacing);
        Vector3 position = Vector3.zero;

        GameObject marcherObject = Instantiate(marcherPrefab, position, Quaternion.identity, transform);
        marcherObject.name = $"Marcher{index + 1}";

        MarcherPositionsManager marcher = marcherObject.GetComponent<MarcherPositionsManager>();
        MarcherController marcherController = marcherObject.GetComponent<MarcherController>();
        
        marcher.InitializeSets(sets, countsPerSet, color, positionSpherePrefab); 
        marcherController.InitializeMarcher(this);
        marchers.Add(marcher); 
    }


    private void ArrangeMarchersInSquare()
    {
        if (shapeMarchers == null)
        {
            Debug.LogWarning("ShapeMarchers is null; cannot arrange formation.");
            return;
        }

        // Convert the list of marcher components into a list of GameObjects.
        List<GameObject> marcherObjects = new List<GameObject>();
        foreach (var marcher in marchers)
        {
            marcherObjects.Add(marcher.gameObject);
        }

        // Utilize ShapeMarchers to position marchers in a square.
        shapeMarchers.ArrangeFormation(
            ShapeMarchers.ShapeType.Box,  // Use a Box formation to create a square shape.
            intervalManager.GetIntervalType(interval),  // Set spacing based on the interval manager.
            marcherObjects  // List of all marcher GameObjects.
        );
    }

    /// <summary>
    /// Updates each marcher to ensure they have the correct number of sets.
    /// Adds or removes position spheres as necessary.
    /// </summary>
    private void UpdateMarcherSets()
    {
        foreach (var marcher in marchers)
        {
            int currentSets = marcher.PositionSpheres.Length;
            int difference = numberOfSets - currentSets;
            Color sphereColor = GetMarcherColor(marcher);

            if (difference > 0)
            {
                // If more sets are needed, add position spheres.
                for (int i = currentSets; i < numberOfSets; i++)
                {
                    marcher.AddPositionSphere(i, positionSpherePrefab, sphereColor);
                }
            }
            else if (difference < 0)
            {
                // If fewer sets are needed, remove extra position spheres.
                for (int i = currentSets - 1; i >= numberOfSets; i--)
                {
                    marcher.RemovePositionSphere(i);
                }
            }
        }
    }

    /// <summary>
    /// Determines the color of the marcher by checking its existing spheres.
    /// </summary>
    /// <param name="marcher">The marcher whose color is being determined.</param>
    /// <returns>The color of the marcher.</returns>
    private Color GetMarcherColor(MarcherPositionsManager marcher)
    {
        foreach (var sphere in marcher.PositionSpheres)
        {
            if (sphere?.GetComponent<Renderer>() is Renderer renderer)
            {
                return renderer.sharedMaterial.color;
            }
        }

        foreach (var sphere in marcher.setSpheres)
        {
            if (sphere?.GetComponent<Renderer>() is Renderer renderer)
            {
                return renderer.sharedMaterial.color;
            }
        }

        return Color.white; // Default to white if no color is found.
    }
}
