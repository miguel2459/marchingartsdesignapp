using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles copying and pasting marcher positions across counts.
/// Use LeftCtrl+C to copy, LeftCtrl+H to paste as "hold", LeftCtrl+Space to paste as "march".
/// </summary>
public class MarcherClipboardManager : MonoBehaviour
{
    [Header("Dependencies")]
    public SelectedMarchers selectedMarchers;
    public CountsProgressBar countsProgressBar;
    public EnsembleDirector2 director;
    public MarcherPositionService marcherPositionService;
    private Dictionary<MarcherPositionsManager, Vector3> copiedPositions = new Dictionary<MarcherPositionsManager, Vector3>();

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.C)) CopySelected();
            if (Input.GetKeyDown(KeyCode.H)) PasteToCurrent("hold");
            if (Input.GetKeyDown(KeyCode.Space)) PasteToCurrent("march");
        }
    }

    private void CopySelected()
    {
        copiedPositions.Clear();

        int set, count;
        GetTargetSetAndCount(out set, out count);

        foreach (GameObject marcherObj in selectedMarchers.selectedMarchers)
        {
            if (marcherObj.TryGetComponent(out MarcherPositionsManager posManager))
            {
                if (posManager.HasPositionAtCount(set, count))
                {
                    copiedPositions[posManager] = posManager.GetPositionAtCount(set, count);
                }
            }
        }

        Debug.Log($"📋 Copied {copiedPositions.Count} marcher positions from Set {set}, Count {count}");
    }

    private void PasteToCurrent(string tag)
    {
        int set, count;
        GetTargetSetAndCount(out set, out count);

        foreach (GameObject marcherObj in selectedMarchers.selectedMarchers)
        {
            if (marcherObj.TryGetComponent(out MarcherPositionsManager posManager) &&
                copiedPositions.TryGetValue(posManager, out Vector3 copiedPos))
            {
                switch (tag)
                {
                    case "march":
                        marcherPositionService.ConfirmMarcherPosition(posManager, set, count, copiedPos);
                        break;
                    // case "hold":
                    //     posManager.ConfirmHoldAndFillBack(set, count, copiedPos);
                    //     break;
                    default:
                        Debug.LogWarning($"❓ Unsupported tag '{tag}' passed to PasteToCurrent().");
                        break;
                }
                marcherObj.transform.position = copiedPos;
            }
        }

        Debug.Log($"📌 Pasted {copiedPositions.Count} marchers to Set {set}, Count {count} with tag: {tag}");
        countsProgressBar?.UpdateCountSubtextsForSet(set);
        director?.UpdateInspectorSetProgress();
    }

    private void GetTargetSetAndCount(out int setIndex, out int countIndex)
    {
        int currentSet = int.Parse(SessionManager.instance.showStateSO.LastSet);
        int activeCountIndex = countsProgressBar?.GetActiveCountIndex() ?? -1;

        if (activeCountIndex >= 0)
        {
            setIndex = currentSet;
            countIndex = activeCountIndex + 1;
        }
        else
        {
            if (currentSet == 1)
            {
                setIndex = 0;
                countIndex = 0;
            }
            else
            {
                setIndex = currentSet - 1;
                countIndex = SessionManager.instance.runtimeCacheSO.SetTimingMap[setIndex].count;
            }
        }
    }
}
