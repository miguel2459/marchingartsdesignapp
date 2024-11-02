using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Metronome2 : MonoBehaviour
{
    public AudioClip metronomeClip; // The audio clip for the metronome
    public Text setText; // UI Text component for displaying the current set
    public Text counterText; // UI Text component for displaying the current count

    [SerializeField]
    private float beatInterval;
    private AudioSource audioSource;
    private int cycleCount = 0;
    private bool isRunning = false; // Set initial state to false
    private MarcherController[] marchers;

    public EnsembleDirector2 director;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = metronomeClip;
    }

    // This function updates BPM and recalculates beat interval
    public void UpdateBPM(float bpm)
    {
        beatInterval = 60f / bpm;
        MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();
        foreach (var marcherController in marcherControllers)
        {
            marcherController.SetStepDuration(bpm);
        }
    }

    public void StartMetronome()
    {
        if (!isRunning)
        {
            isRunning = true;
            cycleCount = 0; // Reset cycle count at start
            director.ApplyUpdates();
            StartCoroutine(MetronomeRoutine());
        }
    }

    IEnumerator MetronomeRoutine()
    {
        int count = 1;
        MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();
        foreach (var marcher in director.marchers)
        {
            marcher.GetComponent<MarcherController>().ResetMarcherPosition();
        }
        Debug.Log("Metronome started");

        while (isRunning)
        {
            if (count <= director.countsPerSet)
            {
                counterText.text = count.ToString(); // Update counter text with the current count
                audioSource.Play(); // Play the metronome sound
                setText.text = $"Set {cycleCount}"; // Update set text with the current set
                Debug.Log("count: " + count + " cycle: " + cycleCount);

                if (count == 1 && cycleCount == 1)
                {
                    //Debug.Log("Triggering StartMarching for all marcherControllers " + marcherControllers.Length);
                    // Start marching for all marcherControllers
                    foreach (var marcherController in marcherControllers)
                    {
                        marcherController.StartMarching(cycleCount);
                    }
                }
            }

            yield return new WaitForSeconds(beatInterval);
            count++;

            // Reset the count after reaching the set number of steps for each marcherController
            if (count > director.countsPerSet)
            {
                count = 1;
                cycleCount++;
            }

            // Stop the metronome if maxSets is reached for any marcherController
            if (cycleCount > director.numberOfSets)
            {
                StopMetronome(); // Stop and reset metronome if the cycle count exceeds maxSets
                break; // Exit the loop when the max number of repeats is reached
            }
        }
    }

    public void StopMetronome()
    {
        audioSource.Play();
        isRunning = false; // Stop the metronome

        // Reset the counter and set text when the metronome stops
        counterText.text = "Complete";
        setText.text = "Sets: " + cycleCount.ToString();

        cycleCount = 0;

        // Reset all marcher controllers to their starting positions
        MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();
        foreach (var marcherController in marcherControllers)
        {
            if (marcherController.positions.Length > 0)
            {
                marcherController.ResetMarcher(); // Assuming you have a reset function for marchers
            }
        }

        Debug.Log("Metronome stopped and reset.");
    }
}
