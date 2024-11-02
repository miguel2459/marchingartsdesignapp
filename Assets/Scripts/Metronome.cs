using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Metronome : MonoBehaviour
{
    public AudioClip metronomeClip; // The audio clip for the metronome
    public float bpm; // Beats per minute, can be adjusted as needed
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
        // Set BPM on each MarcherController in the scene

        MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();
        foreach (var marcherController in marcherControllers)
        {
            marcherController.SetStepDuration(bpm);
        }

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

            // Run through each marcher and place them at position[0] transform
            MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();
            foreach (var marcherController in marcherControllers)
            {
                if (marcherController.positions.Length > 0)
                {
                    marcherController.transform.position = marcherController.positions[0].position;
                    marcherController.CalculateStepPositions();
                    Debug.Log($"Marcher {marcherController.name} placed at position[0] {marcherController.positions[0].position}");
                }
                else
                {
                    Debug.LogWarning($"Marcher {marcherController.name} has no positions set.");
                }
            }

        }
    }

    IEnumerator MetronomeRoutine()
    {
        int count = 1;
        MarcherController[] marcherControllers = FindObjectsOfType<MarcherController>();

        while (isRunning)
        {
            counterText.text = count.ToString();
            audioSource.Play();

            if (count == 1)
            {
                cycleCount++;

                // Start marching for all marcherControllers
                foreach (var marcherController in marcherControllers)
                {
                    marcherController.StartMarching(cycleCount);
                }
            }

            yield return new WaitForSeconds(beatInterval);
            count++;

            // Reset the count after reaching the set number of steps for each marcherController
            if (count > marcherControllers[0].stepsPerLine)
            {
                count = 1;
            }

            // Stop the metronome if maxSets is reached for any marcherController
            if (cycleCount > marcherControllers[0].maxSets + 1)
            {
                break; // Exit the loop when the max number of repeats is reached
            }
        }
    }


    public void StopMetronome()
    {
        audioSource.Play();
        isRunning = false; // Stop the metronome
        startButton.gameObject.SetActive(true);
        startButton.onClick.AddListener(StartMetronome);

        // Reset the counter and audio when maxRepeats is reached
        counterText.text = "Done";

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
