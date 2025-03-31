using UnityEngine;
using UnityEngine.UI;

public class EnsembleUIController : MonoBehaviour
{
    [Header("Input Fields")]
    public InputField numberOfMarchersInputField;
    public InputField numberOfSetsInputField;
    public InputField countsPerSetInputField;
    public InputField bpmInputField;
    public InputField intervalField;

    [Header("Target Reference")]
    public EnsembleDirector2 director;
    public SetProgressBar setsBar;

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
            SessionManager.instance.SessionState.NumberOfMarchers = director.numberOfMarchers;
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
            SessionManager.instance.SessionState.NumberOfSets = director.numberOfSets;
            director.setBar.OnTotalSetsChanged(director.numberOfSets); // Update SetProgressBar
            director.PopulateMarchers(); // Adjust marcher spheres
        }
        else
        {
            RevertToPreviousValue(numberOfSetsInputField, director.numberOfSets);
        }
    }
}
