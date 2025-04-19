using UnityEngine;
using UnityEngine.UI;
using System;
  
public class EnsembleUIController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private InputField numberOfMarchersInputField;
    [SerializeField] private InputField numberOfSetsInputField;
    [SerializeField] private InputField countsPerSetInputField;
    [SerializeField] private InputField startBpmInputField;
    [SerializeField] private InputField endBpmInputField;
    [SerializeField] private InputField intervalField;

    [Header("Director (implements IMarcherProvider & ISetProgressTracker)")]
    [SerializeField] private EnsembleDirector2 director;
    private EnsembleSessionLoader sessionLoader => director.SessionLoader;
    private ISetProgressTracker setProgressTracker; 

    [Header("Progress Bars")]
    [SerializeField] private SetProgressBar setsBar;
    [SerializeField] private CountsProgressBar countsBar;

    [Header("Playback Buttons")]
    [SerializeField] private Button buttonPlaySet01;
    [SerializeField] private Button buttonPlayChunk;
    [SerializeField] private Button buttonPlayCurrentSet;

    private void Awake()
    {
        if (director == null)
        {
            Debug.LogError("EnsembleUIController: Missing reference to EnsembleDirector2!");
            enabled = false;
            return;
        }

        // cache interface
        setProgressTracker = director as ISetProgressTracker;
        if (setProgressTracker != null)
            setProgressTracker.OnSetProgressChanged += setsBar.UpdateSetProgressColor;

        // listen for any marcher-position changes to update count subtexts
        MarcherPositionsManager.OnAnyMarcherPositionUpdated += HandleAnyMarcherPositionUpdated;

        AttachInputListeners();
    }

    private void OnDestroy()
    {
        if (setProgressTracker != null)
            setProgressTracker.OnSetProgressChanged -= setsBar.UpdateSetProgressColor;

        MarcherPositionsManager.OnAnyMarcherPositionUpdated -= HandleAnyMarcherPositionUpdated;
    }

    /// <summary>
    /// Called by EnsembleDirector2.HandleSessionReady() to seed all UI fields.
    /// </summary>
    public void InitializeUI()
    {
        int last = sessionLoader.LastSet;

        numberOfMarchersInputField.text = director.numberOfMarchers.ToString();
        numberOfSetsInputField.text     = director.numberOfSets.ToString();

         if (sessionLoader.RuntimeCache.SetTimingMap.TryGetValue(last, out var timing))
         {
             countsPerSetInputField.text = timing.count.ToString();
             startBpmInputField.text          = timing.startBPM.ToString("F1");
             endBpmInputField.text          = timing.endBPM.ToString("F1");
         }

         // 3) (Re)build your Set‐bar & Count‐bar
         setsBar.InitializeSetsBar();
 
         // 4) Force‐highlight + load timing inputs for the last set
         setsBar.HighlightSet(last);
         setsBar.UpdateTimingInputsForSet(last);
 
         // 5) Counts‐bar: ensure defaults, then fill in subtexts & colors
         countsBar.EnsureSetTimingDefaults(director.numberOfSets);
    }

    public void InitializeCountsBar()
    {
    int setIndex = sessionLoader.LastSet;
    countsBar.UpdateCountSubtextsForSet(setIndex);
    countsBar.UpdateCountProgressColors(setIndex);
    }

    private void AttachInputListeners()
    {
        numberOfMarchersInputField.onEndEdit.AddListener(OnNumberOfMarchersEdited);
        numberOfSetsInputField.onEndEdit.AddListener(OnNumberOfSetsEdited);

        buttonPlaySet01.onClick.AddListener(() =>
        {
            // 1) Update the SO so sessionLoader.LastSet now returns 1
            sessionLoader.ShowState.LastSet = "1";

            // 2) Highlight Set 1 in the UI (this also updates currentSetText & re‑renders counts)
            setsBar.OnSetButtonClick(1);

            // 3) Finally, start the metronome from Set 1
            director.metronome.StartMetronome(1);
        });

        // If you implement chunk playback, wire it here:
        // buttonPlayChunk.onClick.AddListener(() => { … });

        buttonPlayCurrentSet.onClick.AddListener(() =>
            director.metronome.StartMetronome(sessionLoader.LastSet)
        );
    }

    private void OnNumberOfMarchersEdited(string value)
    {
        if (int.TryParse(value, out int parsed))
        {
            parsed = Mathf.Max(1, parsed);
            director.numberOfMarchers               = parsed;
            sessionLoader.ShowState.NumberOfMarchers = parsed;
            sessionLoader.ReloadSession();  // rebuild marchers & re‑call InitializeUI
        }
        else
        {
            numberOfMarchersInputField.text = director.numberOfMarchers.ToString();
        }
    }

    private void OnNumberOfSetsEdited(string value)
    {
        if (int.TryParse(value, out int parsed))
        {
            parsed = Mathf.Max(1, parsed);
            director.numberOfSets           = parsed;
            sessionLoader.ShowState.NumberOfSets = parsed;
            setsBar.OnTotalSetsChanged(parsed);
            sessionLoader.ReloadSession();  // rebuild marchers & re‑call InitializeUI
        }
        else
        {
            numberOfSetsInputField.text = director.numberOfSets.ToString();
        }
    }

    /// <summary>
    /// Whenever *any* marcher position is confirmed/cleared, refresh the count subtexts
    /// for the currently selected set (so you see the little numbers pop in/out).
    /// </summary>
    private void HandleAnyMarcherPositionUpdated()
    {
        int currentSet = sessionLoader.LastSet;
        countsBar.UpdateCountSubtextsForSet(currentSet);
        countsBar.UpdateCountProgressColors(currentSet);
    }
}
