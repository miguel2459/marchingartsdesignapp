using UnityEngine;
using UnityEngine.UI;

public class EnsembleUIController : MonoBehaviour
{
    SessionManager session = SessionManager.instance;

    [Header("Input Fields")]
    public InputField numberOfMarchersInputField;
    public InputField numberOfSetsInputField;
    public InputField countsPerSetInputField;
    public InputField bpmInputField;
    public InputField intervalField;

    [Header("Target Reference")]
    public EnsembleDirector2 director;
    public SetProgressBar setsBar;
    public CountsProgressBar countsBar;

    public Button buttonPlaySet01;
    public Button buttonPlayChunk;
    public Button buttonPlayCurrentSet;
    

    private void Start()
    {
        if (director == null)
        {
            Debug.LogError("❌ EnsembleUIController: Missing reference to EnsembleDirector2!");
            return;
        }
        AttachInputListeners();
    }

    public void InitializeUI()
    {
        numberOfMarchersInputField.text = director.numberOfMarchers.ToString();
        numberOfSetsInputField.text = director.numberOfSets.ToString();
        countsPerSetInputField.text = director.countsPerSet.ToString();
        bpmInputField.text = director.bpm.ToString();
        intervalField.text = director.interval.ToString();
        
        setsBar.InitializeSetsBar();
        countsBar.EnsureSetTimingDefaults(director.numberOfSets);
    }

    private void AttachInputListeners()
    {
        numberOfMarchersInputField.onEndEdit.AddListener((value) => {
            if (Input.GetKeyDown(KeyCode.Return))
                ValidateAndUpdateMarchers(value);
            else
                RevertToPreviousValue(numberOfMarchersInputField, director.numberOfMarchers);
        });

        numberOfSetsInputField.onEndEdit.AddListener((value) => {
            if (Input.GetKeyDown(KeyCode.Return))
                ValidateAndUpdateSets(value);
            else
                RevertToPreviousValue(numberOfSetsInputField, director.numberOfSets);
        });

         buttonPlaySet01.onClick.AddListener(() =>
        {
            director.metronome.StartMetronome(1);
        });

        // buttonPlayChunk.onClick.AddListener(() =>
        // {
        //     int chunkStart = GetChunkStartSet(); // You define this logic
        //     director.metronome.StartMetronome(chunkStart);
        // });

        buttonPlayCurrentSet.onClick.AddListener(() =>
        {
            int currentSet = int.Parse(session.showStateSO.LastSet);
            director.metronome.StartMetronome(currentSet);
        });
    }

    private int GetChunkStartSet()
{
    // You can return a value based on dropdown, toggle, or internal logic
    return 3; // Example: chunk starts at set 3
}


    private void RevertToPreviousValue(InputField field, int previousValue)
    {
        field.text = previousValue.ToString();
    }

    private void ValidateAndUpdateMarchers(string value)
    {
        if (int.TryParse(value, out int parsedValue))
        {
            director.numberOfMarchers = Mathf.Max(1, parsedValue);
            session.showStateSO.NumberOfMarchers = director.numberOfMarchers;
            director.PopulateMarchers();
        }
        else
        {
            RevertToPreviousValue(numberOfMarchersInputField, director.numberOfMarchers);
        }
    }

    private void ValidateAndUpdateSets(string value)
    {
        if (int.TryParse(value, out int parsedValue))
        {
            director.numberOfSets = Mathf.Max(1, parsedValue);
            session.showStateSO.NumberOfSets = director.numberOfSets;
            director.setBar.OnTotalSetsChanged(director.numberOfSets); // Update SetProgressBar
            director.PopulateMarchers(); // Adjust marcher spheres
        }
        else
        {
            RevertToPreviousValue(numberOfSetsInputField, director.numberOfSets);
        }
    }
}
