using UnityEngine;

[RequireComponent(typeof(MarcherPositionsManager))]
public class MarcherVisualStateController : MonoBehaviour
{
    private MarcherPositionsManager positionManager;
    private Unit unit;

    private void Awake()
    {
        positionManager = GetComponent<MarcherPositionsManager>();
        unit = GetComponent<Unit>();
    }

    /// <summary>
    /// Toggles the unit selector visual (e.g., ring highlight).
    /// </summary>
    public void SetSelectorVisible(bool visible)
    {
        if (unit != null)
        {
            unit.SetSelector(visible);
        }
    }

    /// <summary>
    /// Repositions marcher to a known confirmed dot.
    /// </summary>
    public void RepositionToDot(int set, int count)
    {
        if (positionManager.HasPositionAtCount(set, count))
        {
            Vector3 pos = positionManager.GetPositionAtCount(set, count);
            transform.position = pos;
        }
    }

    /// <summary>
    /// Reposition to fallback (last known confirmed or inferred dot).
    /// </summary>
    public void RepositionToLastKnown()
    {
        int fallbackSet = 0;
        int fallbackCount = 0;

        if (SessionManager.instance.showStateSO.LastSet != "1")
        {
            fallbackSet = int.TryParse(SessionManager.instance.showStateSO.LastSet, out var last) ? last - 1 : 0;

            if (SessionManager.instance.runtimeCacheSO.SetTimingMap.TryGetValue(fallbackSet, out var timing))
                fallbackCount = timing.count;
        }

        RepositionToDot(fallbackSet, fallbackCount);
    }

    /// <summary>
    /// Optional future: set marcher color externally.
    /// </summary>
    public void SetMaterialColor(Color c)
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = c;
    }
}
