using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RuntimeCacheSO", menuName = "MADA/Runtime Cache")]
public class RuntimeCacheSO : ScriptableObject
{
    public string CachedMarcherJSON;
    public string CachedTimingJSON;

    //public Dictionary<string, Vector3> MarchersCoordinates = new Dictionary<string, Vector3>();
    public Dictionary<int, SetTimingData> SetTimingMap = new Dictionary<int, SetTimingData>();
    [System.NonSerialized] // don’t write this big dictionary into the .asset on disk
    public Dictionary<string, Dictionary<int, Dictionary<int, PositionEntry>>> ParsedCountPositions;
    public Dictionary<string, MarcherIdentity> ParsedIdentities { get; set; }
    [System.Serializable]
    public class SetTimingData
    {
        public int setIndex;
        public int count;
        public float startBPM;
        public float endBPM;

        public SetTimingData(int setIndex, int count, float startBPM, float endBPM)
        {
            this.setIndex = setIndex;
            this.count = count;
            this.startBPM = startBPM;
            this.endBPM = endBPM;
        }
    }

    public void Clear()
    {
        CachedMarcherJSON = "";
        CachedTimingJSON = "";
        //MarchersCoordinates.Clear();
        SetTimingMap.Clear();
    }
}
