using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class ClickDragMarcherSelector : MonoBehaviour
{
    public RectTransform selectionImage; // UI RectTransform to show the selection box
    Rect selectionRect;
    public EnsembleDirector2 director;
    public SelectedMarchers selectMarcher;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private bool isDragging = false;

    void Start(){
        director = GetComponent<EnsembleDirector2>();
        selectMarcher = GetComponent<SelectedMarchers>();
        UpdateSelectionBox();
    }
    void Update(){
        HandleMouseInput();
    }
    void HandleMouseInput()
    {
        
        isDragging = false; // Reset dragging state

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            startMousePos = Input.mousePosition;
            selectionRect = new Rect();
        }

        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            endMousePos = Input.mousePosition;
            UpdateSelectionBox();
            //X Axix Calculations
            if(Input.mousePosition.x < startMousePos.x){
                selectionRect.xMin = Input.mousePosition.x;
                selectionRect.xMax = startMousePos.x;
            }
            else{
                selectionRect.xMin = startMousePos.x; 
                selectionRect.xMax = Input.mousePosition.x;
            }
            //y Axix Calculations
            if(Input.mousePosition.y < startMousePos.y){
                selectionRect.yMin = Input.mousePosition.y;
                selectionRect.yMax = startMousePos.y;
            }
            else{
                selectionRect.yMin = startMousePos.y; 
                selectionRect.yMax = Input.mousePosition.y;
            }
        }

        if(Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt)){
            SelectMarchersInDrag();
            startMousePos = endMousePos = Vector2.zero;
            UpdateSelectionBox();
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

    void SelectMarchersInDrag()
    {
        foreach(MarcherPositionsManager marcher in director.marchers)
        {
            if(selectionRect.Contains(Camera.main.WorldToScreenPoint(marcher.gameObject.transform.position)))
            {
                selectMarcher.SelectMarcher(marcher.gameObject);
            }
        }

    }
}