using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteInEditMode]
public class EnsembleDirector2 : MonoBehaviour
{
    [Header("Marcher Settings")]
    public int numberOfMarchers;
    public int numberOfSets;
    public int countsPerSet;
    public float bpm;
    public float interval;

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

    public List<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();

    void Start()
    {
        InitializeSession();
        InitializeUI();
        setBar.OnTotalSetsChanged(numberOfSets);
        shapeMarchers.InitializeShapeManagers(marcherPrefab, positionSpherePrefab, interval);
    }

    private void InitializeSession(){
        numberOfMarchers = SessionManager.instance.numberOfMarchers;
        numberOfSets = SessionManager.instance.numberOfSets;
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

        setBar.OnTotalSetsChanged(numberOfSets);
        PopulateMarchers();
        metronome.UpdateBPM(bpm);

        Debug.Log($"Updated: Marchers = {numberOfMarchers}, Sets = {numberOfSets}, Counts = {countsPerSet}, BPM = {bpm}");
    }

    public void PopulateMarchers()
    {
        marchers = new List<MarcherPositionsManager>(GetComponentsInChildren<MarcherPositionsManager>());
        int currentMarcherCount = marchers.Count;

        if (currentMarcherCount < numberOfMarchers)
            AddMarchers(currentMarcherCount);
        else if (currentMarcherCount > numberOfMarchers)
            RemoveExcessMarchers(currentMarcherCount);

        UpdateMarcherSets();
        UpdateShapeMarchers();
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

    private void UpdateShapeMarchers()
    {
        if (shapeMarchers == null)
        {
            Debug.LogWarning("ShapeMarchers is null; cannot arrange formation.");
            return;
        }

        shapeGroup.marchers = new List<GameObject>(marchers.Count);
        foreach (var marcher in marchers)
            shapeGroup.marchers.Add(marcher.gameObject);

        shapeMarchers.ArrangeFormation(
            shapeUI.GetCurrentShape(),
            intervalManager.GetIntervalType(interval),
            shapeGroup.marchers
        );
    }

    private void CreateMarcher(int index, Color color, int sets)
    {
        GameObject marcherObject = Instantiate(marcherPrefab, Vector3.zero, Quaternion.identity, transform);
        marcherObject.name = $"Marcher{index + 1}";

        MarcherPositionsManager marcher = marcherObject.GetComponent<MarcherPositionsManager>();
        marcher.Initialize(sets, countsPerSet, color, positionSpherePrefab);

        for (int i = 1; i < sets; i++)
            marcher.PositionSpheres[i].name = $"{marcherObject.name} - Position {i + 1}";

        marchers.Add(marcher);
    }

    private void UpdateMarcherSets()
    {
        foreach (var marcher in marchers)
        {
            int currentSets = marcher.PositionSpheres.Length;
            int difference = numberOfSets - currentSets;
            Color sphereColor = GetMarcherColor(marcher);

            if (difference > 0)
                for (int i = currentSets; i < numberOfSets; i++)
                    marcher.AddPositionSphere(i, positionSpherePrefab, sphereColor);
            else if (difference < 0)
                for (int i = currentSets - 1; i >= numberOfSets; i--)
                    marcher.RemovePositionSphere(i);
        }
    }

    private Color GetMarcherColor(MarcherPositionsManager marcher)
    {
        foreach (var sphere in marcher.PositionSpheres)
            if (sphere?.GetComponent<Renderer>() is Renderer renderer)
                return renderer.sharedMaterial.color;

        foreach (var sphere in marcher.setSpheres)
            if (sphere?.GetComponent<Renderer>() is Renderer renderer)
                return renderer.sharedMaterial.color;

        return Color.white;
    }
}
