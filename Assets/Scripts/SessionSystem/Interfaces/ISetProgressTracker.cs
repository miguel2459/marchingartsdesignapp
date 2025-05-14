// Scripts/Interfaces/ISetProgressTracker.cs
public interface ISetProgressTracker
{
    /// <summary>Returns 0 – 1 for the given set.</summary>
    float GetSetProgress(int setIndex);

    /// <summary>Raised when any set’s progress changes (setIndex, percent).</summary>
    event System.Action<int, float> OnSetProgressChanged;
}
