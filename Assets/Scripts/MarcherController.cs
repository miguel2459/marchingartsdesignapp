using UnityEngine;

/// <summary>
/// Controls animation/movement of a marcher during performance mode.
/// Interpolates across confirmed set positions based on BPM and count structure.
/// </summary>
public class MarcherController : MonoBehaviour
{
    public MarcherPositionsManager marcherPositionsManager;
    public EnsembleDirector2 director;

    private Vector3[] setPositions;
    private Vector3[] stepPositions;
    private float[] stepDurations;

    private int currentStep = 0;
    private int completedRepeats = 0;
    private float elapsedTime = 0f;

    private bool isMarching = false;

    /// <summary>
    /// Inject EnsembleDirector and initialize marcher.
    /// </summary>
    public void InitializeMarcher(EnsembleDirector2 directorReference)
    {
        director = directorReference;

        // Get all confirmed set positions from marcher
        setPositions = marcherPositionsManager.GetAllSetPositionsSorted();

        if (setPositions.Length > 0)
        {
            transform.position = setPositions[0];
        }
        else
        {
            Debug.LogWarning($"{name} has no setPositions yet.");
        }
    }

    /// <summary>
    /// Called by Metronome at the beginning of the marching cycle.
    /// </summary>
    public void StartMarching(int cycle)
    {
        if (cycle >= 1 && completedRepeats < director.numberOfSets - 1)
        {
            if (SessionManager.instance.SessionState.SetTimingMap.TryGetValue(cycle, out var timing))
            {
                isMarching = true;
                PrepareNextStepPositions(timing);
            }
            else
            {
                Debug.LogWarning($"{name} ❌ No SetTimingData for Set {cycle}");
                isMarching = false;
            }
        }
    }

    /// <summary>
    /// Resets marcher state to beginning.
    /// </summary>
    public void ResetMarcher()
    {
        isMarching = false;
        completedRepeats = 0;
        currentStep = 0;
        elapsedTime = 0f;

        if (setPositions != null && setPositions.Length > 0)
        {
            transform.position = setPositions[0];
        }
    }

    /// <summary>
    /// Prepare interpolated step positions between two set positions.
    /// </summary>
    private void PrepareNextStepPositions(SessionState.SetTimingData timing)
    {
        int from = completedRepeats;
        int to = completedRepeats + 1;

        if (setPositions == null || from >= setPositions.Length - 1)
        {
            Debug.LogWarning($"{name}: Not enough set positions for marching from Set {from}.");
            isMarching = false;
            return;
        }

        int steps = timing.count;
        stepPositions = new Vector3[steps];
        stepDurations = new float[steps];

        for (int i = 0; i < steps; i++)
        {
            float t = (steps > 1) ? (float)i / (steps - 1) : 0f;
            float bpm = Mathf.Lerp(timing.startBPM, timing.endBPM, t);
            float duration = 60f / bpm;

            stepPositions[i] = Vector3.Lerp(setPositions[from], setPositions[to], (float)(i + 1) / steps);
            stepDurations[i] = duration;
        }

        currentStep = 0;
        elapsedTime = 0f;
    }

    public bool HasSetPositions()
    {
        return marcherPositionsManager.GetAllSetPositionsSorted().Length > 0;
    }

    private void Update()
    {
        if (!isMarching || stepPositions == null || currentStep >= stepPositions.Length) return;

        elapsedTime += Time.deltaTime;
        float t = elapsedTime / stepDurations[currentStep];

        Vector3 start = currentStep == 0 ? setPositions[completedRepeats] : stepPositions[currentStep - 1];
        Vector3 end = stepPositions[currentStep];

        transform.position = Vector3.Lerp(start, end, t);

        if (t >= 1f)
        {
            currentStep++;
            elapsedTime = 0f;

            if (currentStep >= stepPositions.Length)
            {
                completedRepeats++;

                if (completedRepeats >= setPositions.Length - 1)
                {
                    isMarching = false;
                    FindObjectOfType<Metronome2>()?.StopMetronome();
                }
                else
                {
                    // 🧠 Get next set timing again for next transition
                    int nextSet = completedRepeats + 1;
                    if (SessionManager.instance.SessionState.SetTimingMap.TryGetValue(nextSet, out var timing))
                    {
                        PrepareNextStepPositions(timing);
                        isMarching = true;
                    }
                    else
                    {
                        Debug.LogWarning($"{name} ❌ No SetTimingData for Set {nextSet}");
                        isMarching = false;
                    }
                }
            }
        }
    }
}
