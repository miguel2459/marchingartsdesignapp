using UnityEngine;

[RequireComponent(typeof(MarcherPositionsManager))]
public class MarcherVisualStateController : MonoBehaviour
{
    private MarcherPositionsManager positionManager;
    private Unit unit;
    public bool isSelected = false;
    public Color selectedColor = new Color(0f, 0.81f, 1f, 1f); // Blue
    public Color holdColor = new Color(0.8f, 0.6f, 1f, 1f);     // Purple
    public Color defaultColor = Color.white;


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
    public void ApplyHoldColorIfEligible(int setIndex, int countIndex)
    {
        if (!positionManager.HasFullProgressForSet(setIndex))
            return;

        bool isHolding = positionManager.IsHoldingAtCount(setIndex, countIndex);

        // 🟦 PRIORITY 1: Selected → Always blue, regardless of hold
        if (isSelected)
        {
            SetMaterialColor(selectedColor); // blue marcher
            return;
        }

        // 🟪 PRIORITY 2: Holding (and not selected)
        if (isHolding)
        {
            SetMaterialColor(holdColor); // purple marcher
        }
        else
        {
            SetMaterialColor(defaultColor);                  // normal unselected marcher
        }
    }

    /// <summary>
    /// Optional future: set marcher color externally.
    /// </summary>
    public void SetMaterialColor(Color c)
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = c;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selected)
        {
            SetMaterialColor(selectedColor);
        }
        else
        {
            SetMaterialColor(Color.white); // reset to progress-state, can be overridden
        }
    }

    public void UpdateSelectorAnchor(int setIndex, int countIndex)
    {
        if (!positionManager.TryGetConfirmedPosition(setIndex, countIndex, out Vector3 confirmedPos))
        {
            unit?.AttachSelectorToMarcher();  // fallback to default parenting
            unit?.SetSelector(false);         // hide if no confirmed dot
            return;
        }

        float dist = Vector3.Distance(transform.position, confirmedPos);
        if (dist < 0.01f)
        {
            unit?.AttachSelectorToMarcher();     // close enough to stay attached
            unit?.SetSelector(true, positionManager.IsHoldingAtCount(setIndex, countIndex));
        }
        else
        {
            unit?.DetachSelectorAt(confirmedPos);  // detach and leave at dot
            unit?.SetSelector(true, positionManager.IsHoldingAtCount(setIndex, countIndex));
        }
    }
    
    public void UpdateSelectorAttachment(Vector3 currentPosition, Vector3? confirmedAnchor)
    {
        if (!TryGetComponent(out Unit unit)) return;
        if (!confirmedAnchor.HasValue) return;

        float dist = Vector3.Distance(currentPosition, confirmedAnchor.Value);
        if (dist > 0.05f)
        {
            unit.DetachSelectorAt(confirmedAnchor.Value);
        }
        else
        {
            unit.AttachSelectorToMarcher();
        }
    }
}
