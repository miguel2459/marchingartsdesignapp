using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Metronome1 : MonoBehaviour
{
    public AudioClip metronomeClip; // The audio clip for the metronome
    public float bpm = 240f; // Beats per minute, can be adjusted as needed
    public Text counterText; // UI Text component for the counter
    public MarcherController marcherController; // Reference to the MarcherController
    public Button startButton; // Reference to the UI Button

    private float beatInterval;
    private AudioSource audioSource;
    private int cycleCount = 0;
    private bool isRunning = false; // Set initial state to false

    void Start()
    {
        beatInterval = 60f / bpm;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = metronomeClip;
        marcherController.SetStepDuration(bpm); // Pass the BPM to the marcher controller

        // Set up the button to call StartMetronome when clicked
        startButton.onClick.AddListener(StartMetronome);
    }

    public void StartMetronome()
    {
        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(MetronomeRoutine());
            startButton.gameObject.SetActive(false); // Hide the button after starting
        }
    }

    IEnumerator MetronomeRoutine()
    {
        int count = 1;
        while (isRunning)
        {
            counterText.text = count.ToString();
            audioSource.Play();

            if (count == 1)
            {
                cycleCount++;
                marcherController.StartMarching(cycleCount);
            }

            yield return new WaitForSeconds(beatInterval);
            count++;

            if (count > marcherController.stepsPerLine )
            {
                count = 1; // Reset the count after reaching the set number of steps
            }

            // Stop the metronome if maxRepeats is reached
            if (cycleCount > marcherController.maxSets + 1)
            {
                break; // Exit the loop when the max number of repeats is reached
            }
        }
    }

    public void StopMetronome()
    {
        audioSource.Play();
        isRunning = false; // Stop the metronome
        // Stop the counter and audio when maxRepeats is reached
        counterText.text = "Done";
    }
}
