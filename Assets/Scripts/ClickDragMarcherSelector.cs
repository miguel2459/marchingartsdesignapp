using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class ClickDragMarcherSelector : MonoBehaviour
{
    public RectTransform selectionImage; // UI RectTransform to show the selection box
    Rect selectionRect;
    public EnsembleDirector2 director;
    public SelectedMarchers selectMarcher;
    public MarcherMovement moveMarcher;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    void Start(){
        director = GetComponent<EnsembleDirector2>();
        selectMarcher = GetComponent<SelectedMarchers>();
        moveMarcher = GetComponent<MarcherMovement>();
        UpdateSelectionBox();
    }
    void Update(){
        HandleMouseInput();
    }
    void HandleMouseInput()
    {
        // Early exit if components aren't initialized
        if (selectMarcher == null || moveMarcher == null) return;

        // Start drag
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            startMousePos = Input.mousePosition;
            selectionRect = new Rect();
            
            // Only clear selection if not holding shift or control
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) && !moveMarcher.transformGizmo)
            {
                selectMarcher.ClearSelection();
            }

            // Initialize selection box size to zero
            if (selectionImage != null)
            {
                selectionImage.sizeDelta = Vector2.zero;
            }
        }

        // Update drag
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt) && !moveMarcher.transformGizmo)
        {
            endMousePos = Input.mousePosition;
            
            // Only update selection rect if we have valid start and end positions
            if (startMousePos != Vector2.zero)
            {
                //X Axis Calculations
                if(Input.mousePosition.x < startMousePos.x){
                    selectionRect.xMin = Input.mousePosition.x;
                    selectionRect.xMax = startMousePos.x;
                }
                else{
                    selectionRect.xMin = startMousePos.x; 
                    selectionRect.xMax = Input.mousePosition.x;
                }
                //Y Axis Calculations
                if(Input.mousePosition.y < startMousePos.y){
                    selectionRect.yMin = Input.mousePosition.y;
                    selectionRect.yMax = startMousePos.y;
                }
                else{
                    selectionRect.yMin = startMousePos.y; 
                    selectionRect.yMax = Input.mousePosition.y;
                }
                UpdateSelectionBox();
            }
        }

        // End drag
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && !moveMarcher.transformGizmo)
        {
            // Only process selection if we have a valid drag area
            if (startMousePos != Vector2.zero && endMousePos != Vector2.zero)
            {
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    DeSelectMarchersInDrag();
                }
                else if (Input.GetKey(KeyCode.LeftShift))
                {
                    SelectMarchersInDrag(true);
                }
                else
                {
                    SelectMarchersInDrag(false);
                }
            }
            
            ResetSelectionBox();
        }
    }
        
    void UpdateSelectionBox()
    {
        Vector2 boxStart = startMousePos;
        Vector2 center = (boxStart + endMousePos)/2;
        selectionImage.position = center;

        float sizeX = Mathf.Abs(boxStart.x - endMousePos.x);
        float sizeY = Mathf.Abs(boxStart.y - endMousePos.y);

        selectionImage.sizeDelta = new Vector2(sizeX, sizeY);
    }

    void SelectMarchersInDrag(bool isAdditive)
    {
        // Check if there are marchers to select
        if (director.marchers == null || director.marchers.Count == 0) return;

        // Loop through all marchers in the director's list
        foreach (MarcherPositionsManager marcher in director.marchers)
        {
            // Check if the marcher is within the selection rectangle
            if (selectionRect.Contains(Camera.main.WorldToScreenPoint(marcher.gameObject.transform.position)))
            {
                // If the marcher is already selected and we're doing additive selection, skip it
                if (isAdditive && selectMarcher.selectedMarchers.Contains(marcher.gameObject))
                {
                    continue;
                }

                // Add marcher to the selection
                selectMarcher.SelectMarcher(marcher.gameObject);
            }
        }
    }


    void DeSelectMarchersInDrag()
    {
        // Check if there are marchers to deselect
        if (director.marchers == null || director.marchers.Count == 0) return;

        // If the marcher is within the selection rectangle, deselect it
        foreach (MarcherPositionsManager marcher in director.marchers)
        {
            Vector3 marcherScreenPos = Camera.main.WorldToScreenPoint(marcher.gameObject.transform.position);
            if (selectionRect.Contains(marcherScreenPos))
            {
                selectMarcher.DeselectMarcher(marcher.gameObject);
                Debug.Log($"Marcher {marcher.gameObject.name} deselected via drag.");
            }
        }
    }

    private void ResetSelectionBox()
    {
        startMousePos = endMousePos = Vector2.zero;
        if (selectionImage != null)
        {
            selectionImage.sizeDelta = Vector2.zero;
        }
    }
}