using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Metronome2 : MonoBehaviour
{
    [Header("Audio & UI")]
    public AudioClip metronomeClip;
    public Text setText;
    public Text counterText;
    public CountsProgressBar countsProgressBar; 
    public SetProgressBar setProgressBar;

    [Header("Director for Previews")]
    public MonoBehaviour directorObject;          // assign your EnsembleDirector2 (for PreviewCountPosition)
    private EnsembleDirector2 director;           // cached cast
    private ISetProgressTracker setProgressTracker;

    [Header("Playback Settings")]
    [SerializeField]
    private float beatInterval;
    public AudioSource audioSource;
    private int cycleCount = 0;
    private bool isRunning = false;

    [Header("Injected Data")]
    [SerializeField] private RuntimeCacheSO runtimeCache;  // assign in Inspector

    // ▶ New: injected marcher list
    private IReadOnlyList<MarcherPositionsManager> marchers = new List<MarcherPositionsManager>();
    /// <summary>Set from outside: metronome.Marchers = marcherManager.Marchers;</summary>
    public IReadOnlyList<MarcherPositionsManager> Marchers
    {
        get => marchers;
        set => marchers = value ?? new List<MarcherPositionsManager>();
    }

    void Awake()
    {
        // preserve preview functionality
        director = directorObject as EnsembleDirector2;
        setProgressTracker = director as ISetProgressTracker;
    }

    void Start()
    {
        audioSource.clip = metronomeClip;
    }

    /// <summary>
    /// Kick off marching playback from set <paramref name="startSet"/>.
    /// </summary>
    public void StartMetronome(int startSet = 1)
    {
        // ▶ Use injected marchers
        foreach (var marcher in marchers)
        {
            var controller = marcher.GetComponent<MarcherController>();
            controller.InitializeMarcher(runtimeCache, marcher);
            controller.ResetMarcher(startSet);
        }
        
        // 🧼 Auto-hide transform gizmo before metronome playback
        if (director.TransformGizmoManager != null && director.TransformGizmoManager.HasActiveGizmo)
        {
            director.TransformGizmoManager.HideTransformGizmo();
            Debug.Log("🧽 Transform Gizmo hidden for clean metronome playback.");
        }


        if (!isRunning)
        {
            isRunning = true;
            cycleCount = startSet;
            StartCoroutine(MetronomeRoutine());
        }
    }

    IEnumerator MetronomeRoutine()
    {
        int count = 1;
        int previousSet = Mathf.Max(0, cycleCount - 1);
        int lastCount   = 0;

        // If the very first set is incomplete, bail immediately
       if (setProgressTracker != null && setProgressTracker.GetSetProgress(cycleCount) < 1f)
       {
           Debug.Log($"🛑 Metronome stopped: Set {cycleCount} is only {setProgressTracker.GetSetProgress(cycleCount):P0} complete");
           StopMetronome();
           yield break;
       }

        // Determine lastCount for seamless start
        if (previousSet == 0)
            lastCount = 0;
        else if (runtimeCache
                     .SetTimingMap.TryGetValue(previousSet, out var prevTiming))
            lastCount = prevTiming.count;

        // Initialize first marching segment
        foreach (var marcher in marchers)
        {
            Vector3 fromPos = marcher.GetPositionAtCount(previousSet, lastCount);
            marcher.GetComponent<MarcherController>()
                    .RestartMarching(cycleCount, fromPos);
        }

        while (isRunning)
        {            
            // Before each new set, verify it’s 100% done
            if (setProgressTracker != null && setProgressTracker.GetSetProgress(cycleCount) < 1f)
            {
                Debug.Log($"🛑 Metronome stopping at end of Set {cycleCount-1}: Set {cycleCount} is only {setProgressTracker.GetSetProgress(cycleCount):P0} complete");
                StopMetronome();
                director.ColorMarchersForSet(cycleCount - 1);
                yield break;
            }


            if (!runtimeCache
                    .SetTimingMap.TryGetValue(cycleCount, out var timing))
            {
                Debug.LogWarning($"❌ No timing data for Set {cycleCount}");
                StopMetronome();
                yield break;
            }

            int totalCounts = timing.count;
            float startBPM   = timing.startBPM;
            float endBPM     = timing.endBPM;

            float lerpT = (totalCounts > 1) ? (count - 1f) / (totalCounts - 1f) : 0f;
            float interpolatedBPM = Mathf.Lerp(startBPM, endBPM, lerpT);
            beatInterval = 60f / interpolatedBPM;

            counterText.text = count.ToString();
            audioSource.Play();
            countsProgressBar?.HighlightCount(count - 1);
            countsProgressBar?.ScrollToMakeCountVisible(count - 1);

            yield return new WaitForSeconds(beatInterval);

            count++;
            if (count > totalCounts)
            {
                // advance to next set
                count = 1;
                cycleCount++;
                setText.text = cycleCount.ToString();

                setProgressBar?.HighlightSet(cycleCount);
                setProgressBar?.UpdateTimingInputsForSet(cycleCount);
                countsProgressBar?.ResetHighlight();
                setProgressBar?.CenterOnSetButton(cycleCount);

                director.VisualizePathsForSet(cycleCount);

                if (runtimeCache
                        .SetTimingMap.TryGetValue(cycleCount, out var nextSet))
                {
                    countsProgressBar?.RenderCounts(cycleCount, nextSet.count);
                    countsProgressBar?.UpdateCountSubtextsForSet(cycleCount);

                    // prepare next marching segment
                    previousSet = Mathf.Max(0, cycleCount - 1);
                    lastCount   = (previousSet == 0)
                                  ? 0
                                  : runtimeCache
                                          .SetTimingMap[previousSet].count;

                    foreach (var marcher in marchers)
                    {
                        Vector3 fromPos = marcher.GetPositionAtCount(previousSet, lastCount);
                        marcher.GetComponent<MarcherController>()
                               .RestartMarching(cycleCount, fromPos);
                    }
                }
            }

            // stop after final set
            if (cycleCount > director.NumberOfSets)
            {
                StopMetronome();
                yield break;
            }
        }
    }

    public void StopMetronome()
    {
        audioSource.Play();
        isRunning = false;

        counterText.text = "00";
        setText.text = cycleCount.ToString();

        // ensure all marchers stop
        foreach (var mc in FindObjectsOfType<MarcherController>())
            mc.StopMarching();

        cycleCount = 0;
        Debug.Log("🛑 Metronome stopped. All marchers snapped to last completed position.");
    }

    public bool IsRunning()
    {
        return isRunning;
    }
}
