using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class IntervalManager : MonoBehaviour
{
    // Define constants for each interval step size based on the 8-5 marching standard
    private const float ONE_STEP_SIZE = 1f*(5f/8f); 
    private const float TWO_STEP_SIZE = 2f*(5f/8f); 
    private const float THREE_STEP_SIZE = 3f*(5f/8f); 
    private const float FOUR_STEP_SIZE = 4f*(5f/8f); 
    private const float FIVE_STEP_SIZE = 5f*(5f/8f); 

    public enum IntervalType 
    { 
        OneStep, 
        TwoStep, 
        ThreeStep, 
        FourStep, 
        FiveStep 
    }

    // Retrieves the spacing value associated with a given interval type
    public float GetIntervalSpacing(IntervalType intervalType)
    {
        switch (intervalType)
        {
            case IntervalType.OneStep:
                return ONE_STEP_SIZE;
            case IntervalType.TwoStep:
                return TWO_STEP_SIZE;
            case IntervalType.ThreeStep:
                return THREE_STEP_SIZE;
            case IntervalType.FourStep:
                return FOUR_STEP_SIZE;
            case IntervalType.FiveStep:
                return FIVE_STEP_SIZE;
            default:
                Debug.LogWarning("Unrecognized interval type. Defaulting to ONE_STEP_SIZE.");
                return ONE_STEP_SIZE;
        }
    }

    public IntervalType EstimateIntervalType(List<GameObject> marchers)
    {
        if (marchers == null || marchers.Count < 2)
        {
            Debug.LogWarning("Not enough marchers to estimate spacing. Defaulting to TwoStep.");
            return IntervalType.TwoStep;
        }

        List<float> distances = new List<float>();

        for (int i = 0; i < marchers.Count - 1; i++)
        {
            for (int j = i + 1; j < marchers.Count; j++)
            {
                float dist = Vector3.Distance(marchers[i].transform.position, marchers[j].transform.position);
                if (dist > 0)
                    distances.Add(dist);
            }
        }

        float avg = distances.Count > 0 ? distances.Average() : TWO_STEP_SIZE;

        Debug.Log($"IntervalManager: Estimated average spacing: {avg}");

        // Find the closest interval
        IntervalType closestType = IntervalType.OneStep;
        float closestDiff = Mathf.Abs(avg - ONE_STEP_SIZE);

        foreach (IntervalType type in System.Enum.GetValues(typeof(IntervalType)))
        {
            float spacing = GetIntervalSpacing(type);
            float diff = Mathf.Abs(avg - spacing);

            if (diff < closestDiff)
            {
                closestDiff = diff;
                closestType = type;
            }
        }

        Debug.Log($"IntervalManager: Closest matched IntervalType: {closestType}");

        return closestType;
    }


    // Method to convert an integer to IntervalType
    public IntervalType GetIntervalType(float interval)
    {
        switch (interval)
        {
            case 1:
                return IntervalType.OneStep;
            case 2:
                return IntervalType.TwoStep;
            case 3:
                return IntervalType.ThreeStep;
            case 4:
                return IntervalType.FourStep;
            case 5:
                return IntervalType.FiveStep;
            default:
                Debug.LogWarning("Invalid interval value. Defaulting to OneStep.");
                return IntervalType.OneStep;
        }
    }
}
