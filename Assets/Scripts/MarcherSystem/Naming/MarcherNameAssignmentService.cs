using System.Collections.Generic;

/// <summary>
/// Tracks name usage per section and provides section abbreviations.
/// </summary>
public static class MarcherNameAssignmentService
{
    private static Dictionary<string, int> sectionCounters = new Dictionary<string, int>();

    // Full section names (used for dropdown UI)
    private static readonly List<string> standardSections = new List<string>
    {
        "Flute", "Piccolo", "Clarinet", "Bass Clarinet",
        "Alto Sax", "Tenor Sax", "Soprano Sax", "Bari Sax",
        "Trumpet", "Mellophone", "Baritone", "Euphonium",
        "Trombone", "Tuba", "Snare", "Tenors",
        "Bass Drum", "Cymbals", "Guard", "Drum Major", "Pit"
    };

    // Abbreviation mapping for marcher label naming
    private static readonly Dictionary<string, string> sectionAbbreviations = new Dictionary<string, string>
    {
        { "Flute",         "FL" },
        { "Piccolo",       "PI" },
        { "Clarinet",      "CL" },
        { "Bass Clarinet", "BC" },
        { "Alto Sax",      "AS" },
        { "Tenor Sax",     "TS" },
        { "Soprano Sax",   "SS" },
        { "Bari Sax",      "BS" },
        { "Trumpet",       "TR" },
        { "Mellophone",    "ME" },
        { "Baritone",      "BR" },
        { "Euphonium",     "EU" },
        { "Trombone",      "TB" },
        { "Tuba",          "TU" },
        { "Snare Drum",         "SN" },
        { "Tenor Drum",        "TN" },
        { "Bass Drum",     "BD" },
        { "Cymbals",       "CY" },
        { "Guard",         "GD" },
        { "Drum Major",    "DM" },
        { "Pit",           "PT" }
    };

    /// <summary>
    /// Gets full section names for dropdown UI.
    /// </summary>
    public static List<string> GetStandardSections()
    {
        return new List<string>(standardSections);
    }

    /// <summary>
    /// Returns the next available number for a given section.
    /// </summary>
    public static int GetNextNumberFor(string section)
    {
        if (!sectionCounters.ContainsKey(section))
            sectionCounters[section] = 1;

        return sectionCounters[section]++;
    }

    /// <summary>
    /// Gets the abbreviation (e.g., "TR" for Trumpet) for a full section name.
    /// </summary>
    public static string GetAbbreviation(string section)
    {
        return sectionAbbreviations.TryGetValue(section, out string abbr)
            ? abbr
            : section; // fallback to full name if not found
    }
    public static string GetSectionByAbbreviation(string abbr)
    {
        foreach (var kvp in sectionAbbreviations)
        {
            if (kvp.Value == abbr)
                return kvp.Key;
        }
        return abbr; // fallback
    }
}
