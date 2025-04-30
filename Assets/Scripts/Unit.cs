using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public GameObject selector;

    // Start is called before the first frame update
    void Start()
    {
        SetSelector(false);
    }

    // Update is called once per frame
    void Update()
    {
        
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


}
