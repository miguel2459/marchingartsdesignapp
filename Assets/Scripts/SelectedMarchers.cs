using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedMarchers : MonoBehaviour
{
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;

    public LayerMask marcherLayer;
    public List<GameObject> selectedMarchers = new List<GameObject>();
    public CameraControl cameraControl; // Reference to the CameraControl script
    public Camera cam;

    public MarcherMovement moveMarcher;
    public bool selectAllMarchers; // Toggle to select all marchers in the inspector
    
    public GameObject firstSelectedMarcher; // To store the first selected marcher

    void Start()
    {
        cam = Camera.main;
        
        moveMarcher = GetComponent<MarcherMovement>();

        // Ensure the cameraControl reference is assigned
        if (cameraControl == null)
        {
            cameraControl = cam.GetComponent<CameraControl>();
        }
        //Debug.Log("MarcherSelector: Start method called - Camera and selection box initialized.");
    }

    void Update()
    {
        CheckForSpaceBarSetPosition();
    }


    public void SelectMarcher(GameObject marcher)
    {
        if (!selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Add(marcher);
            marcher.GetComponent<Renderer>().material.color = highlightColor; // Highlight the selected marcher
            marcher.GetComponent<Unit>().SetSelector(true);
            if(moveMarcher.transformGizmo != null)
            {
                marcher.transform.SetParent(moveMarcher.transformGizmo.transform);

                // If the gizmo already exists and this is the first marcher selected, center the gizmo on this marcher
                if (firstSelectedMarcher == null)
                {
                    firstSelectedMarcher = marcher.gameObject;
                    moveMarcher.MoveGizmoToFirstSelectedMarcher(firstSelectedMarcher);
                }
            }
            Debug.Log($"MarcherSelector: Marcher {marcher.name} selected and highlighted.");
        }
    }


    public void DeselectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Remove(marcher);
            marcher.GetComponent<Renderer>().material.color = normalColor;
            marcher.GetComponent<Unit>().SetSelector(false);
            if (moveMarcher.transformGizmo != null)
            {
                marcher.transform.SetParent(transform);
            }
            Debug.Log($"MarcherSelector: Marcher {marcher.name} deselected and color reverted.");
        }
    }

    public void ClearSelection()
    {
        foreach (var marcher in selectedMarchers)
        {
            marcher.GetComponent<Renderer>().material.color = normalColor;
            marcher.GetComponent<Unit>().SetSelector(false);
            marcher.transform.SetParent(transform);
        }

        selectedMarchers.Clear();
        firstSelectedMarcher = null;
        moveMarcher.HideTransformGizmo();
        //Debug.Log("MarcherSelector: All marchers deselected and list cleared.");
    }

    public void CheckForSpaceBarSetPosition()
    {
        // Check if space bar is pressed, gizmo is active, and there are selected marchers
        if (Input.GetKeyDown(KeyCode.Space) && moveMarcher.transformGizmo != null && selectedMarchers.Count > 0)
        {
            Debug.Log("MarcherSelector: Space bar pressed - Setting position spheres for selected marchers.");

            // Loop through each selected marcher
            foreach (var marcher in selectedMarchers)
            {
                marcher.gameObject.GetComponent<MarcherPositionsManager>().SetPositionSphere();
            }
        }
    }

    public void UpdateCameraFocus()
    {
        if (cameraControl != null)
        {
            cameraControl.SetSelectedMarchers(selectedMarchers);
        }
    }
}
