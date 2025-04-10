using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Metronome2 : MonoBehaviour
{
    public AudioClip metronomeClip;
    public Text setText;
    public Text counterText;
    public CountsProgressBar countsProgressBar; 
    public SetProgressBar setProgressBar;

    [SerializeField]
    private float beatInterval;
    private AudioSource audioSource;
    private int cycleCount = 0;
    private bool isRunning = false;

    public EnsembleDirector2 director;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = metronomeClip;
    }

    public void StartMetronome(int startSet = 1)
    {
        foreach (var marcher in director.marchers)
        {
            var controller = marcher.GetComponent<MarcherController>();
            controller.InitializeMarcher(director, SessionManager.instance.runtimeCacheSO);
            controller.ResetMarcher(startSet);
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

        // 🔁 Determine starting point for smooth interpolation from last set
        int previousSet = Mathf.Max(0, cycleCount - 1);
        int lastCount = 0;

        if (previousSet == 0)
        {
            lastCount = 0;
        }
        else if (SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(previousSet, out var prevTiming))
        {
            lastCount = prevTiming.count;
        }

        // ✅ Initialize marcher movement from correct starting position
        foreach (var marcher in director.marchers)
        {
            Vector3 fromPosition = marcher.GetPositionAtCount(previousSet, lastCount);
            marcher.GetComponent<MarcherController>().StartMarching(cycleCount, fromPosition);
        }

        while (isRunning)
        {
            if (!SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(cycleCount, out var timing))
            {
                Debug.LogWarning($"❌ No timing data for Set {cycleCount}");
                StopMetronome();
                yield break;
            }

            int totalCounts = timing.count;
            float startBPM = timing.startBPM;
            float endBPM = timing.endBPM;

            float lerpT = (totalCounts > 1) ? (count - 1f) / (totalCounts - 1f) : 0f;
            float interpolatedBPM = Mathf.Lerp(startBPM, endBPM, lerpT);
            beatInterval = 60f / interpolatedBPM;

            counterText.text = count.ToString();
            audioSource.Play();
            countsProgressBar?.HighlightCount(count - 1);

            yield return new WaitForSeconds(beatInterval);

            count++;

            if (count > totalCounts)
            {
                count = 1;
                cycleCount++;
                setText.text = $"{cycleCount}";

                setProgressBar?.HighlightSet(cycleCount);
                setProgressBar?.UpdateTimingInputsForSet(cycleCount);
                countsProgressBar?.ResetHighlight();

                if (SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(cycleCount, out var nextSet))
                {
                    countsProgressBar?.RenderCounts(cycleCount, nextSet.count);

                    // 🔁 New previous set becomes the one we just completed
                    previousSet = Mathf.Max(0, cycleCount - 1);
                    lastCount = (previousSet == 0) ? 0 : SessionManager.instance.runtimeCacheSO.SetTimingMap[previousSet].count;

                    // ✅ Start next set's movement from correct last known position
                    foreach (var marcher in director.marchers)
                    {
                        Vector3 fromPosition = marcher.GetPositionAtCount(previousSet, lastCount);
                        marcher.GetComponent<MarcherController>().StartMarching(cycleCount, fromPosition);
                    }
                }
            }

            if (cycleCount > director.numberOfSets)
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
        setText.text = $"{cycleCount}";

        foreach (var mc in FindObjectsOfType<MarcherController>())
        {
            mc.StopMarching();
        }

        cycleCount = 0;

        Debug.Log("🛑 Metronome stopped. All marchers snapped to last completed position.");
    }
}
