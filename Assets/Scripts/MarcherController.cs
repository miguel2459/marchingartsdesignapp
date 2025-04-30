using UnityEngine;

/// <summary>
/// Controls animation/movement of a marcher during performance mode.
/// Interpolates across confirmed count positions based on BPM and count structure.
/// </summary>
public class MarcherController : MonoBehaviour
{
    public RuntimeCacheSO runtimeCacheSO;
    public MarcherPositionsManager marcherPositionsManager;        // cached cast

    private Vector3[] countPositions;
    private float[] stepDurations;

    private int currentStep = 0;
    private float elapsedTime = 0f;
    private bool isMarching = false;
    private int currentSetIndex = 1;

    /// <summary>
    /// Inject EnsembleDirector and initialize marcher.
    /// </summary>
    public void InitializeMarcher(RuntimeCacheSO cacheSO)
    {
        runtimeCacheSO = cacheSO;
    }

    /// <summary>
    /// Called by Metronome at the beginning of the marching cycle.
    /// </summary>
    public void StartMarching(int setIndex, Vector3 fromPosition)
    {
        currentSetIndex = setIndex; // 🔧 store correct marching set

        if (runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing))
        {
            PrepareCountStepPositions(setIndex, timing, fromPosition);
            isMarching = true;
        }
        else
        {
            Debug.LogWarning($"{name} ❌ No SetTimingData for Set {setIndex}");
            isMarching = false;
        }
    }

    /// <summary>
    /// Prepare interpolated step positions for the current set using count-level data.
    /// </summary>
    private void PrepareCountStepPositions(int setIndex, RuntimeCacheSO.SetTimingData timing, Vector3 fromPosition)
    {
        int totalCounts = timing.count;
        countPositions = marcherPositionsManager.GetInterpolatedPath(setIndex, timing.count, fromPosition);
        stepDurations = new float[countPositions.Length - 1]; // one duration per segment

        for (int i = 0; i < stepDurations.Length; i++)
        {
            float t = (totalCounts > 1) ? (float)i / (totalCounts - 1) : 0f;
            float bpm = Mathf.Lerp(timing.startBPM, timing.endBPM, t);
            stepDurations[i] = 60f / bpm;
        }

        currentStep = 0;
        elapsedTime = 0f;
    }

    public void ResetMarcher(int startSetIndex = 1)
    {
        isMarching = false;
        currentStep = 0;
        elapsedTime = 0f;

        if (marcherPositionsManager.HasPositionAtCount(startSetIndex, 1))
        {
            transform.position = marcherPositionsManager.GetPositionAtCount(startSetIndex, 1);
        }
    }

    public void StopMarching()
    {
        isMarching = false;
    }
    private void Update()
    {
        if (!isMarching || countPositions == null || currentStep >= stepDurations.Length)
            return;

        // ✅ Color logic BEFORE animation
        int set = currentSetIndex;
        int count = currentStep + 1;

        GetComponent<MarcherVisualStateController>()?.ApplyHoldColorIfEligible(set, count);

        elapsedTime += Time.deltaTime;
        float t = elapsedTime / stepDurations[currentStep];
        t = Mathf.Clamp01(t); // prevent overshoot

        Vector3 start = countPositions[currentStep];
        Vector3 end = countPositions[currentStep + 1];

        transform.position = Vector3.Lerp(start, end, t);

        if (t >= 1f)
        {
            currentStep++;
            elapsedTime = 0f;

            if (currentStep >= countPositions.Length)
                isMarching = false;
        }
    }
}