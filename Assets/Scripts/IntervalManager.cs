using UnityEngine;

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
