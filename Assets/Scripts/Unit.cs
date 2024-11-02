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

    public void SetSelector(bool toggle){
        selector.SetActive(toggle);
    }
}
