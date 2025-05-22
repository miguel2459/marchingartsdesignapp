using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private GameObject selector;
    private Transform originalParent;
    private Transform ensembleDetachedParent;


    void Awake()
    {
        SetSelector(false);
        if (selector != null)
            originalParent = selector.transform.parent;
    }
    public void SetDetachedParent(Transform newParent)
    {
        ensembleDetachedParent = newParent;
    }

    public void SetSelector(bool toggle, bool isHolding = false)
    {
        selector.SetActive(toggle);

        if (!toggle)
            return;

        Color selectorColor = isHolding
            ? new Color(0.8f, 0.6f, 1f, 1f) // light purple
            : new Color(0f, 0.81f, 1f, 1f); // blue

        SetSelectorColor(selectorColor);
    }

    public void SetSelectorColor(Color color)
    {
        if (selector.TryGetComponent<Renderer>(out var rend))
            rend.material.color = color;
    }

    public void AttachSelectorToMarcher()
    {
        if (selector == null) return;
        selector.transform.SetParent(originalParent);
        Vector3 currentLocalPos = selector.transform.localPosition;
        selector.transform.localPosition = new Vector3(0f, currentLocalPos.y, 0f);
    }

    public void DetachSelectorAt(Vector3 worldPos)
    {
        if (selector == null) return;
        selector.transform.SetParent(ensembleDetachedParent);
        selector.transform.position = new Vector3(worldPos.x, selector.transform.position.y, worldPos.z);
    }

    public void AttachSelectorToConfirmedPosition(Vector3 confirmedWorldPosition, bool isHolding)
    {
        if (selector == null) return;

        selector.transform.SetParent(originalParent); // back to marcher
        selector.transform.localPosition = new Vector3(0f, selector.transform.localPosition.y, 0f); // center xz

        selector.SetActive(true);
        SetSelectorColor(isHolding
            ? new Color(0.8f, 0.6f, 1f, 1f) // purple
            : new Color(0f, 0.81f, 1f, 1f) // blue
        );
    }
}
