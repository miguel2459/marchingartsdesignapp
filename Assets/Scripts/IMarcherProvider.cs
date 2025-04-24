// Scripts/Interfaces/IMarcherProvider.cs
using System.Collections.Generic;

public interface IMarcherProvider
{
    IReadOnlyList<MarcherPositionsManager> Marchers { get; }
    int NumberOfSets { get; }

    void PreviewCountPosition(int setNumber, int countNumber);
    void RepositionMarchersToSet(int setNumber);
    void ColorMarchersForSet(int setIndex, IEnumerable<MarcherPositionsManager> subset = null);
}
