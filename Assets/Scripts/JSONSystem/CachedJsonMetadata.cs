using System;

/// <summary>
/// Holds both the raw JSON text and the cloud timestamp received from the backend.
/// </summary>
public class CachedJsonMetadata
{
    public string jsonContent;
    public DateTime serverTimestamp;

    public CachedJsonMetadata(string jsonContent, DateTime serverTimestamp)
    {
        this.jsonContent = jsonContent;
        this.serverTimestamp = serverTimestamp;
    }
}