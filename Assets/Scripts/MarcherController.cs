using System;
using UnityEngine;
using System.Collections;

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

    public MarcherPositionsManager marcherPositionsManager; // Reference to the MarcherPositionsManager script
    public EnsembleDirector2 director;

    // Inject director dependency
    public void InitializeMarcher(EnsembleDirector2 directorReference)
    {
        this.director = directorReference;
        marcherPositionsManager = GetComponent<MarcherPositionsManager>();
        SetStepDuration(director.bpm);
        if (marcherPositionsManager.setSpheres.Length == 0)
        {
            Debug.LogError($"{gameObject.name}: setSpheres array is empty.");
            return;
        }

        //transform.position = marcherPositionsManager.setSpheres[0].transform.position;
    }

    public void SetStepDuration(float bpm)
    {
        stepDuration = 60f / bpm; // Calculate step duration based on the BPM
        stepsPerLine = director.countsPerSet;
        maxSets = director.numberOfSets;
        //FillPositionsArrayWithSetSpheres();
    }

    void FillPositionsArrayWithSetSpheres()
    {
        if (marcherPositionsManager != null)
        {
            positions = new Transform[marcherPositionsManager.setSpheres.Length];

            for (int i = 0; i < marcherPositionsManager.setSpheres.Length; i++)
            {
                if (marcherPositionsManager.setSpheres[i] != null)
                {
                    positions[i] = marcherPositionsManager.setSpheres[i].transform;
                }
                else
                {
                    Debug.LogError($"{gameObject.name}: setSpheres[{i}] is NULL! Cannot set positions.");
                }
            }

            // Ensure the marcher is placed at the first valid position
            if (positions.Length > 0 && positions[0] != null)
            {
                transform.position = positions[0].position;
                Debug.Log($"{gameObject.name} snapped to first position at {transform.position}");
            }
            else
            {
                Debug.LogError($"{gameObject.name}: No valid set positions available.");
            }

            // Calculate step positions for movement
            CalculateStepPositions();
        }
        else
        {
            Debug.LogError($"{gameObject.name}: MarcherPositionsManager reference is missing.");
        }
    }

    public void CalculateStepPositions()
    {
        int currentSet = completedRepeats;
        stepPositions = new Vector3[stepsPerLine];

        for (int i = 0; i < stepsPerLine; i++)
        {
            if (positions.Length > currentSet + 1 && positions[currentSet] != null && positions[currentSet + 1] != null)
            {
                // Calculate positions based on grid-structured positions between sets          
                stepPositions[i] = Vector3.Lerp(
                    positions[currentSet].position, 
                    positions[currentSet + 1].position, 
                    (float)(i + 1) / stepsPerLine
                );
                ///Debug.Log($"{gameObject.name} - Set {currentSet + 1}, Step {i + 1} Position: {stepPositions[i]}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: Insufficient set positions available for step calculation.");
            }
        }
    }


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
