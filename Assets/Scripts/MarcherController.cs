using System;
using UnityEngine;
using UnityEngine.UI;

public class MarcherController : MonoBehaviour
{
    public Transform[] positions; // Array to store the positions
    public int maxSets; // Public variable to control how many cycles (sets) before stopping
    public int stepsPerLine; // Public variable to control the number of steps between each set
    //public Text positionsText; // Reference to the UI Text component to display positions

    public Vector3[] stepPositions; // Array to store the positions for each step
    public int currentStep = 0;
    public bool isMarching = false;
    public float elapsedTime = 0f;
    public int completedRepeats = 0;
    public float stepDuration; // Duration of each step in seconds

    private MarcherPositionsManager marcherPositionsManager; // Reference to the MarcherPositionsManager script
    private EnsembleDirector2 director;

    void Start()
    {
        // Reference to the MarcherPositionsManager
        marcherPositionsManager = GetComponent<MarcherPositionsManager>();
        director = FindObjectOfType<EnsembleDirector2>();
        // Populate the positions array with the positions of setSpheres
        SetStepDuration(director.bpm);

        // Ensure the number of positions doesn't exceed maxRepeats
        if (positions.Length > maxSets + 1)
        {
            Debug.LogError("Number of positions exceeds maxRepeats. Adjust maxRepeats to match the number of positions.");
            return;
        }        
    }

    public void SetStepDuration(float bpm)
    {
        stepDuration = 60f / bpm; // Calculate step duration based on the BPM
        stepsPerLine = director.countsPerSet;
        maxSets = director.numberOfSets;
        FillPositionsArrayWithSetSpheres();
    }

    void FillPositionsArrayWithSetSpheres()
    {
        if (marcherPositionsManager != null)
        {
            // Initialize the positions array with the size of setSpheres
            positions = new Transform[marcherPositionsManager.setSpheres.Length];

            for (int i = 0; i < marcherPositionsManager.setSpheres.Length; i++)
            {
                if (marcherPositionsManager.setSpheres[i] != null)
                {
                    positions[i] = marcherPositionsManager.setSpheres[i].transform;
                }
                else
                {
                    Debug.LogWarning($"MarcherController: setSpheres[{i}] is null. Set Position before running Metronome." + gameObject.name);
                }
            }
            CalculateStepPositions();
        }
        else
        {
            Debug.LogError("MarcherController: MarcherPositionsManager reference is missing.");
        }
    }

    public void CalculateStepPositions()
    {
        int currentSet = completedRepeats;
        stepPositions = new Vector3[stepsPerLine];

        for (int i = 0; i < stepsPerLine; i++)
        {
            if(positions[currentSet] != null)
            {
                // Calculate the positions based on the current set (between positions[currentSet] and positions[currentSet + 1])          
                stepPositions[i] = Vector3.Lerp(positions[currentSet].position, positions[currentSet + 1].position, (float)(i + 1) / stepsPerLine);
                Debug.Log($"Set {currentSet + 1}, Step {i + 1} Position: {stepPositions[i]}" + gameObject.name); // Log the positions to the console
            }
            else
            {
                //Debug.LogError(gameObject.name + "There are no set positions yet to calculate the steps");
            }

        }
    }

    //void DisplayStepPositions()
    //{
    //    if (positionsText != null)
    //    {
    //        string positionsString = "Step Positions:\n";
    //        for (int i = 0; i < stepPositions.Length; i++)
    //        {
    //            positionsString += $"Step {i + 1}: {stepPositions[i]}\n";
    //        }
    //        positionsText.text = positionsString;
    //    }
    //}

    public void StartMarching(int cycle)
    {
        //Debug.Log("is cycle:" + cycle + " > 1? && is completedRepeats:" + completedRepeats + " < maxSets:" + maxSets);
        if (cycle >= 1 && completedRepeats < maxSets) // Start moving on the first count of the second cycle
        {
            isMarching = true;
            Debug.Log("isMarching set to true");
            if (cycle == 1)
            {
                CalculateStepPositions(); // Ensure step positions are calculated for the first cycle
            }
        }
    }

    public void ResetMarcher()
    {
        isMarching = false;
        completedRepeats = 0;
        currentStep = 0; // Start from the first step
        elapsedTime = 0f;
    }

    public void ResetMarcherPosition()
    {
        transform.position = positions[0].transform.position;
    }

    void Update()
    {
        if (isMarching && currentStep < stepsPerLine)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / stepDuration;

            // Lerp to the current target step position
            transform.position = Vector3.Lerp(
                currentStep == 0 ? positions[completedRepeats].position : stepPositions[currentStep - 1],
                stepPositions[currentStep],
                t
            );

            if (t >= 1f)
            {
                // Move to the next step
                currentStep++;
                elapsedTime = 0f;

                if (currentStep >= stepsPerLine)
                {
                    isMarching = false; // Stop marching after completing all steps
                    completedRepeats++; // Increment the repeat counter after a full cycle is completed
                    Debug.Log($"Marcher has completed set {completedRepeats}");

                    if (completedRepeats + 1 >= maxSets)
                    {
                        // Notify the Metronome to stop counting
                        FindObjectOfType<Metronome2>().StopMetronome();
                    }
                    else
                    {
                        currentStep = 0; // Reset step counter for the next set
                        CalculateStepPositions(); // Recalculate step positions for the next set
                        isMarching = true; // Continue marching to the next set
                    }
                }
            }
        }
    }
}
