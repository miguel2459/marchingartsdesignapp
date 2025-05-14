using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickDragMarcherSelector : MonoBehaviour
{
    public RectTransform selectionImage; // UI RectTransform to show the selection box
    Rect selectionRect;
    public EnsembleDirector2 director;
    public SelectedMarchers selectMarcher;
    public TransformGizmoManager transformGizmoManager;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    public Camera cam;
    private bool isDragging = false;
    private float dragThreshold = 10f; // pixels

    void Start(){
        UpdateSelectionBox();
    }
    void Update(){
        if (Input.GetMouseButton(0))
        {
            isDragging = (Vector2.Distance(Input.mousePosition, startMousePos) > dragThreshold);
        }
        else
        {
            isDragging = false;
        }

        HandleMouseInput();
    }
    void HandleMouseInput()
    {
         if (InputRouter.BlockSceneInputThisFrame) return;
         
        // Early exit if components aren't initialized
        if (selectMarcher == null || transformGizmoManager == null) return;

        // Start drag
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return; // 👈 Prevent selection logic if clicking on UI

            startMousePos = Input.mousePosition;
            selectionRect = new Rect();
            
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool clickedSelectedMarcher = false;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectMarcher.marcherLayer))
            {
                if (selectMarcher.selectedMarchers.Contains(hit.collider.gameObject))
                {
                    clickedSelectedMarcher = true;
                }
            }

            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) &&
                !transformGizmoManager.HasActiveGizmo && !clickedSelectedMarcher)
            {
                // Only clear if the user will actually perform a drag selection
                // This prevents premature clearing before a click is resolved
                Invoke(nameof(DeferredClearSelection), 0.02f); // allow mouse-up to cancel this if it's just a click
            }


            // Initialize selection box size to zero
            if (selectionImage != null)
            {
                selectionImage.sizeDelta = Vector2.zero;
            }
        }

        // Update drag
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt) && !transformGizmoManager.HasActiveGizmo)
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
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && !transformGizmoManager.HasActiveGizmo)
        {
            // Only process selection if we have a valid drag area
            if (isDragging && startMousePos != Vector2.zero && endMousePos != Vector2.zero)
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

    private void DeferredClearSelection()
    {
        if (isDragging)
        {
            selectMarcher.ClearSelection();
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
            Vector3 screenPos = cam.WorldToScreenPoint(marcher.gameObject.transform.position);
            Debug.Log($"{marcher.name} screenPos = {screenPos}, Z = {screenPos.z}");

            if (screenPos.z > 0 && selectionRect.Contains(new Vector2(screenPos.x, screenPos.y)))
            {
                if (isAdditive && selectMarcher.selectedMarchers.Contains(marcher.gameObject))
                    continue;

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
            Vector3 marcherScreenPos = cam.WorldToScreenPoint(marcher.gameObject.transform.position);
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