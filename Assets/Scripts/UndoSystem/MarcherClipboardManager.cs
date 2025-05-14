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
    [SerializeField] private MarcherLifecycleService marcherLifecycleService;
    public GameObject ghostPrefab;
    private List<GameObject> activeGhosts = new List<GameObject>();
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

        DestroyActiveGhosts(); // Clean up any existing ghosts

        foreach (GameObject marcherObj in selectedMarchers.selectedMarchers)
        {
            if (marcherObj.TryGetComponent(out MarcherPositionsManager posManager))
            {
                if (posManager.HasPositionAtCount(set, count))
                {
                    copiedPositions[posManager] = posManager.GetPositionAtCount(set, count);

                    // 🧠 Create ghost marcher at this position
                    GameObject ghost = Instantiate(ghostPrefab, marcherObj.transform.position, Quaternion.identity);
                    ghost.transform.localScale = marcherObj.transform.localScale * 1.2f;
                    ghost.transform.SetParent(director.transform); // Optional: keep hierarchy tidy
                    activeGhosts.Add(ghost);
                }
            }
        }

        Debug.Log($"📋 Copied {copiedPositions.Count} marcher positions from Set {set}, Count {count}");
    }

    private void DestroyActiveGhosts()
    {
        foreach (var ghost in activeGhosts)
        {
            if (ghost != null)
                Destroy(ghost);
        }
        activeGhosts.Clear();
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
                marcherLifecycleService.ConfirmDot(posManager, set, count, copiedPos);
                marcherObj.transform.position = copiedPos;
            }
        }

        Debug.Log($"📌 Pasted {copiedPositions.Count} marchers to Set {set}, Count {count} with tag: {tag}");
        countsProgressBar?.UpdateCountSubtextsForSet(set);
        director?.UpdateInspectorSetProgress();
        DestroyActiveGhosts(); // 🧼 Clean up the ghost marchers after paste
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
