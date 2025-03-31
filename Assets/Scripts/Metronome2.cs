using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Metronome2 : MonoBehaviour
{
    public AudioClip metronomeClip;
    public Text setText;
    public Text counterText;
    public CountsProgressBar countsProgressBar; // ✅ Injected reference

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

    public void StartMetronome()
    {
        if (!isRunning)
        {
            isRunning = true;
            cycleCount = 1;
            StartCoroutine(MetronomeRoutine());
        }
    }

    IEnumerator MetronomeRoutine()
    {
        int count = 1;

        // Reset all marcher controllers
        foreach (var marcher in director.marchers)
        {
            marcher.GetComponent<MarcherController>().ResetMarcher();
        }

        while (isRunning)
        {
            // 🔁 Get current set and timing data
            if (!SessionManager.instance.SessionState.SetTimingMap.TryGetValue(cycleCount, out var timing))
            {
                Debug.LogWarning($"❌ No timing data for Set {cycleCount}");
                StopMetronome();
                yield break;
            }

            int totalCounts = timing.count;
            float startBPM = timing.startBPM;
            float endBPM = timing.endBPM;

            // Compute current interpolated BPM (for acceleration/deceleration)
            float lerpT = (totalCounts > 1) ? (count - 1f) / (totalCounts - 1f) : 0f;
            float interpolatedBPM = Mathf.Lerp(startBPM, endBPM, lerpT);
            beatInterval = 60f / interpolatedBPM;

            // 🔊 Update UI and audio
            counterText.text = count.ToString();
            setText.text = $"{cycleCount}";
            audioSource.Play();

            // 🎯 Highlight the count in the UI
            countsProgressBar?.HighlightCount(count - 1);

            // 🥁 Start marching on first count of first playable set
            if (count == 1 && cycleCount == 1)
            {
                foreach (var mc in FindObjectsOfType<MarcherController>())
                    mc.StartMarching(cycleCount);
            }

            yield return new WaitForSeconds(beatInterval);

            count++;

            if (count > totalCounts)
            {
                count = 1;
                cycleCount++;

                // Reset count bar highlight
                countsProgressBar?.ResetHighlight();

                // Render next set's count bar if exists
                if (SessionManager.instance.SessionState.SetTimingMap.TryGetValue(cycleCount, out var nextSet))
                {
                    countsProgressBar?.RenderCounts(cycleCount, nextSet.count);
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
        cycleCount = 0;

        counterText.text = "Complete";
        setText.text = "";

        countsProgressBar?.ResetHighlight();

        foreach (var mc in FindObjectsOfType<MarcherController>())
        {
            if (mc.HasSetPositions())
                mc.ResetMarcher();
        }

        Debug.Log("🛑 Metronome stopped.");
    }
}
