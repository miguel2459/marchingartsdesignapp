using UnityEngine;

/// <summary>
/// Handles animation of a marcher during performance playback.
/// Interpolates per-count positions based on dynamic tempo (BPM).
/// </summary>
[RequireComponent(typeof(MarcherVisualStateController))]
public class MarcherController : MonoBehaviour
{
    // ───── Dependencies ─────
    [SerializeField] private RuntimeCacheSO runtimeCacheSO;
    [SerializeField] private MarcherPositionsManager marcherPositionsManager;
    [SerializeField] private MarcherVisualStateController visualController;

    // ───── Marching State ─────
    private Vector3[] countPositions;
    private float[] stepDurations;
    private int currentStep = 0;
    private float elapsedTime = 0f;
    private bool isMarching = false;
    private int currentSetIndex = 1;

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Injects dependencies. Must be called before use.
    /// </summary>
    public void InitializeMarcher(RuntimeCacheSO cacheSO, MarcherPositionsManager manager)
    {
        runtimeCacheSO = cacheSO;
        marcherPositionsManager = manager;
        visualController = GetComponent<MarcherVisualStateController>();
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Begins marching sequence from a specified position and set index.
    /// </summary>
    public void StartMarching(int setIndex, Vector3 fromPosition)
    {
        if (isMarching)
        {
            Debug.LogWarning($"{name} is already marching — call RestartMarching() to override.");
            return;
        }

        currentSetIndex = setIndex;

        if (runtimeCacheSO.SetTimingMap.TryGetValue(setIndex, out var timing))
        {
            PrepareInterpolatedPath(setIndex, timing, fromPosition);
            isMarching = true;
        }
        else
        {
            Debug.LogWarning($"{name} ❌ No timing data found for Set {setIndex}");
        }
    }

    /// <summary>
    /// Stops current marching sequence and clears step state.
    /// </summary>
    public void StopMarching()
    {
        isMarching = false;
        currentStep = 0;
        elapsedTime = 0f;
    }

    /// <summary>
    /// Cleanly stops and restarts marching sequence.
    /// </summary>
    public void RestartMarching(int setIndex, Vector3 fromPosition)
    {
        if (isMarching)
        {
            // Debug.Log($"🔄 Restarting march for {name} at Set {setIndex}");
            StopMarching();
        }

        StartMarching(setIndex, fromPosition);
    }

    /// <summary>
    /// Repositions marcher to set-start point (used during metronome reset).
    /// </summary>
    public void ResetMarcher(int startSetIndex = 1)
    {
        StopMarching();

        if (marcherPositionsManager.HasPositionAtCount(startSetIndex, 1))
        {
            Vector3 fallback = marcherPositionsManager.GetPositionAtCount(startSetIndex, 1);
            transform.position = fallback;

            // Debug.Log($"{name} reset to Set {startSetIndex}, Count 1 → {fallback}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prepares interpolated positions and step durations based on tempo curve.
    /// </summary>
    private void PrepareInterpolatedPath(int setIndex, RuntimeCacheSO.SetTimingData timing, Vector3 fromPosition)
    {
        int count = timing.count;
        countPositions = marcherPositionsManager.GetInterpolatedPath(setIndex, count, fromPosition);

        if (countPositions == null || countPositions.Length < 2)
        {
            Debug.LogWarning($"{name} ❌ Insufficient path data to animate.");
            StopMarching();
            return;
        }

        stepDurations = new float[countPositions.Length - 1];

        for (int i = 0; i < stepDurations.Length; i++)
        {
            float t = (count > 1) ? (float)i / (count - 1) : 0f;
            float bpm = Mathf.Lerp(timing.startBPM, timing.endBPM, t);
            stepDurations[i] = 60f / bpm;

            // Debug.Log($"{name} ⏱ Step {i} → {stepDurations[i]:F2}s @ {bpm:F1} BPM");
        }

        currentStep = 0;
        elapsedTime = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isMarching || countPositions == null || currentStep >= stepDurations.Length)
            return;

        AnimateMarching();
    }

    /// <summary>
    /// Interpolates marcher along calculated path and updates color state.
    /// </summary>
    private void AnimateMarching()
    {
        int set = currentSetIndex;
        int count = currentStep + 1;

        visualController?.ApplyHoldColorIfEligible(set, count);

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / stepDurations[currentStep]);

        Vector3 start = countPositions[currentStep];
        Vector3 end = countPositions[currentStep + 1];
        transform.position = Vector3.Lerp(start, end, t);

        if (t >= 1f)
        {
            currentStep++;
            elapsedTime = 0f;

            if (currentStep >= stepDurations.Length)
            {
                isMarching = false;

                // Debug.Log($"{name} ✅ Finished marching Set {set}");
            }
        }
    }
}
