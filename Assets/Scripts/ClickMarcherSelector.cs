using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMarcherSelector : MonoBehaviour
{
    // Core References (Ensure assigned in Inspector)
    public SelectedMarchers selectedMarchers;
    public TransformGizmoManager transformGizmoManager;
    public EnsembleDirector2 director; // Needed for accessing the list of all marchers

    [Header("Drag Selection")]
    public RectTransform selectionImage; // Assign the UI Image/Panel for the selection box
    public float dragThreshold = 10f; // Min pixels mouse must move to trigger drag

    // Internal State
    private static int lastInputFrame = -1;
    private bool isDragging = false;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private Rect selectionRect;
    // Removed redundant 'selectMarcher' and 'cam' references

    void Start() // Changed Awake to Start to ensure other components might be ready
    {
        // Ensure the selection box is initially hidden and reset
        if (selectionImage != null)
        {
             selectionImage.gameObject.SetActive(false); // Start inactive
        }
       ResetSelectionBox();
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        // Frame check and UI blocking (existing)
        if (lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;
        if (InputRouter.BlockSceneInputThisFrame)
        {
            // Debug.Log("⛔ World input blocked by UI. Skipping MarcherSelector input.");
            return;
        }
        // Prevent interaction if mouse starts or ends action over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
             // Debug.Log("🛡️ MarcherSelector: Input ignored, pointer is over UI element.");
             if (isDragging && Input.GetMouseButtonUp(0)) // Reset drag if released over UI
             {
                 ResetSelectionBox();
                 isDragging = false;
             }
             return;
        }


        // --- Mouse Button Down ---
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            bool clickOnGizmo = false;
            if (transformGizmoManager.HasActiveGizmo)
            {
                // Use selectedMarchers.cam consistently
                Ray gizmoCheckRay = selectedMarchers.cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit gizmoHit;
                // Check ONLY gizmo layer here
                if (Physics.Raycast(gizmoCheckRay, out gizmoHit, Mathf.Infinity, transformGizmoManager.gizmoLayer))
                {
                    clickOnGizmo = true;
                }
            }

            // Only process if gizmo isn't active OR the click was specifically on the gizmo
            if (!transformGizmoManager.HasActiveGizmo || clickOnGizmo)
            {
                startMousePos = Input.mousePosition;
                isDragging = false; // Reset dragging state
                selectionRect = new Rect(); // Initialize logical rect

                // Prepare visual selection box but keep it zero size initially
                if (selectionImage != null)
                {
                    selectionImage.gameObject.SetActive(true);
                    selectionImage.sizeDelta = Vector2.zero;
                }
            }
            // No clearing selection here
        }

        // --- Mouse Button Held Down (Drag Update) ---
        // Only update drag logic if the gizmo is NOT active
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt) && !transformGizmoManager.HasActiveGizmo)
        {
            endMousePos = Input.mousePosition; // Keep updating endPos

            // Check if drag threshold is met (only if not already dragging)
            if (!isDragging && Vector2.Distance(endMousePos, startMousePos) > dragThreshold)
            {
                isDragging = true;
                Debug.Log("🖱️ Drag detected.");

                // Clear selection ONCE when drag starts, unless holding Shift/Ctrl
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl))
                {
                    Debug.Log("🧹 Clearing selection for new drag.");
                    selectedMarchers.ClearSelection();
                }
            }

            // If dragging, update the logical rect AND the visual box
            if (isDragging)
            {
                // --- *** CRITICAL FIX: Update logical selectionRect *** ---
                selectionRect = new Rect(
                    Mathf.Min(startMousePos.x, endMousePos.x),
                    Mathf.Min(startMousePos.y, endMousePos.y),
                    Mathf.Abs(startMousePos.x - endMousePos.x),
                    Mathf.Abs(startMousePos.y - endMousePos.y)
                );
                // --- *** End Critical Fix *** ---

                UpdateSelectionBoxVisual(); // Update visual rect based on start/end pos
            }
        }

        // --- Mouse Button Up ---
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            // --- Case 1: We WERE dragging ---
            // Process drag end ONLY if gizmo wasn't active during the drag
            if (isDragging && !transformGizmoManager.HasActiveGizmo)
            {
                Debug.Log("🖱️ Drag finished.");
                // Perform selection/deselection based on the drag rect and modifier keys
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    DeSelectMarchersInDrag();
                }
                else
                {
                    SelectMarchersInDrag(Input.GetKey(KeyCode.LeftShift)); // Pass additive flag
                }
                selectedMarchers.UpdateCameraFocus(); // Update camera after potential selection change
            }
            // --- Case 2: We were NOT dragging (it was a click) ---
            // Process click logic regardless of gizmo state (gizmo-specific clicks handled within)
            else if (!isDragging) // Ensure it wasn't a drag that just ended
            {
                Debug.Log("🖱️ Click detected.");
                // --- Execute Existing Click Logic ---
                // Use selectedMarchers.cam consistently
                Ray ray = selectedMarchers.cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // A) Handle Ctrl+Click Deselection (no gizmo check needed, specific action)
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    // Raycast ONLY for marchers
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
                    {
                        if (hit.collider?.gameObject?.GetComponent<Unit>() != null)
                        {
                            selectedMarchers.DeselectMarcher(hit.collider.gameObject);
                            selectedMarchers.UpdateCameraFocus();
                            // Reset drag state just in case
                            isDragging = false;
                            ResetSelectionBox();
                            return; // Handled Ctrl+Deselect
                        }
                    }
                     // If Ctrl+Click misses, do nothing (don't clear selection)
                }
                // B) Handle Gizmo Interactions (Click-Away or Re-anchor)
                else if (transformGizmoManager.HasActiveGizmo)
                {
                    var gizmo = transformGizmoManager.transformGizmo;
                    var behavior = gizmo ? gizmo.GetComponent<UnifiedGizmoBehavior>() : null;
                    bool wasDraggingGizmo = behavior && behavior.IsDragging(); // Check if gizmo itself was just interacted with

                    // Check if the click hit *nothing relevant* (marcher or gizmo)
                    // Use combined layer mask here
                    if (!Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer | transformGizmoManager.gizmoLayer))
                    {
                        if (!wasDraggingGizmo) // Don't hide if user was just moving the gizmo
                        {
                            Debug.Log("MarcherSelector: Clicked away - hiding gizmo.");
                            transformGizmoManager.HideTransformGizmo();
                            // Optionally clear selection: selectedMarchers.ClearSelection();
                            // Reset drag state just in case
                            isDragging = false;
                            ResetSelectionBox();
                            return; // Handled click-away
                        }
                    }
                    // C) Handle Re-anchoring Gizmo (Shift+Click on selected marcher)
                    // Check marcher layer specifically
                    else if (hit.collider != null && selectedMarchers.marcherLayer == (selectedMarchers.marcherLayer | (1 << hit.collider.gameObject.layer)))
                    {
                        GameObject clickedMarcher = hit.collider.gameObject;
                        if (Input.GetKey(KeyCode.LeftShift) && selectedMarchers.selectedMarchers.Contains(clickedMarcher))
                        {
                            Debug.Log("MarcherSelector: Reanchoring gizmo to selected marcher.");
                            selectedMarchers.ReanchorToExisting(clickedMarcher);
                            // Reset drag state just in case
                            isDragging = false;
                            ResetSelectionBox();
                            return; // Handled gizmo re-anchor
                        }
                        // If not re-anchoring, do nothing further on click when gizmo is active
                    }
                     // If gizmo IS active and we didn't click-away or re-anchor, do nothing else on click
                }
                // D) Handle Standard Click Selection/Deselection (Only if Gizmo NOT active AND not Ctrl+Click)
                else // Gizmo not active, not Ctrl+Click
                {
                    // Raycast ONLY for marchers
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
                    {
                        GameObject clicked = hit.collider.gameObject;
                        if (clicked?.GetComponent<Unit>() != null) // Ensure it's a valid marcher unit
                        {
                            if (Input.GetKey(KeyCode.LeftShift)) // Additive selection
                            {
                                if (!selectedMarchers.selectedMarchers.Contains(clicked))
                                {
                                    selectedMarchers.SelectMarcher(clicked);
                                }
                            }
                            else // Regular click - select only this one
                            {
                                Debug.Log($"MarcherSelector: Selecting marcher {clicked.name}.");
                                selectedMarchers.ClearSelection(); // Clear first
                                selectedMarchers.SelectMarcher(clicked);
                            }
                            selectedMarchers.UpdateCameraFocus();
                        }
                        // else: Hit something on marcher layer, but not a Unit - do nothing
                    }
                    else // Clicked empty space (and gizmo not active, not Ctrl+Click)
                    {
                        Debug.Log("MarcherSelector: Clicked empty space - clearing selection.");
                        selectedMarchers.ClearSelection();
                    }
                }
            }

            // Reset drag state and box after any MouseUp processing completes
            isDragging = false;
            ResetSelectionBox();
            // --- End of Click/Drag Handling ---
        }
        // --- End of Combined Logic ---
    }

    // --- Helper Methods (Copied/Adapted from ClickDragMarcherSelector) ---

    // Updates the VISUAL selection box UI element
    void UpdateSelectionBoxVisual()
    {
         if (selectionImage == null || !isDragging) return; // Don't update if not dragging or no image

        // Position the UI element; anchor is assumed to be center
        Vector2 center = (startMousePos + endMousePos) / 2f;
        selectionImage.position = center;

        // Set the size of the UI element
        float sizeX = Mathf.Abs(startMousePos.x - endMousePos.x);
        float sizeY = Mathf.Abs(startMousePos.y - endMousePos.y);
        selectionImage.sizeDelta = new Vector2(sizeX, sizeY);
    }

    // Selects marchers within the logical selectionRect
    void SelectMarchersInDrag(bool isAdditive)
    {
        if (director?.Marchers == null || director.Marchers.Count == 0) return;

        // Use selectedMarchers.cam consistently
        Camera currentCam = selectedMarchers.cam;
        if (currentCam == null)
        {
            Debug.LogError("Camera reference is missing in SelectedMarchers!");
            return;
        }


        foreach (MarcherPositionsManager marcherManager in director.Marchers) // Use director.Marchers
        {
            GameObject marcherGO = marcherManager.gameObject; // Get the GameObject
            Vector3 screenPos = currentCam.WorldToScreenPoint(marcherGO.transform.position);

            // Check if marcher is in front of camera and within the logical rect
            if (screenPos.z > 0 && selectionRect.Contains(screenPos)) // Use screenPos directly
            {
                // Use the main selectedMarchers reference
                if (!selectedMarchers.selectedMarchers.Contains(marcherGO)) // Select if not already selected
                {
                    selectedMarchers.SelectMarcher(marcherGO);
                     Debug.Log($"{marcherGO.name} selected via drag.");
                }
            }
            else // If not in the current drag box
            {
                 // If NOT additive selection, deselect marchers outside the box
                 if (!isAdditive && selectedMarchers.selectedMarchers.Contains(marcherGO))
                 {
                      selectedMarchers.DeselectMarcher(marcherGO);
                      Debug.Log($"{marcherGO.name} deselected (outside drag area).");
                 }
            }
        }
    }

    // Deselects marchers within the logical selectionRect
    void DeSelectMarchersInDrag()
    {
        if (director?.Marchers == null || director.Marchers.Count == 0) return;

        // Use selectedMarchers.cam consistently
        Camera currentCam = selectedMarchers.cam;
         if (currentCam == null)
        {
            Debug.LogError("Camera reference is missing in SelectedMarchers!");
            return;
        }

        // Create a temporary list to avoid modification issues while iterating
        List<GameObject> marchersToDeselect = new List<GameObject>();

        foreach (MarcherPositionsManager marcherManager in director.Marchers) // Use director.Marchers
        {
             GameObject marcherGO = marcherManager.gameObject; // Get the GameObject
             // Use the main selectedMarchers reference to check if it's currently selected
             if (selectedMarchers.selectedMarchers.Contains(marcherGO))
             {
                 Vector3 screenPos = currentCam.WorldToScreenPoint(marcherGO.transform.position);
                 // Check if marcher is in front of camera and within the logical rect
                 if (screenPos.z > 0 && selectionRect.Contains(screenPos)) // Use screenPos directly
                 {
                     marchersToDeselect.Add(marcherGO);
                 }
             }
        }

        // Deselect the identified marchers
        foreach(GameObject marcherToDeselect in marchersToDeselect)
        {
             selectedMarchers.DeselectMarcher(marcherToDeselect);
             Debug.Log($"Marcher {marcherToDeselect.name} deselected via Ctrl+drag.");
        }
    }

    // Resets the visual selection box
    private void ResetSelectionBox()
    {
        // Don't reset start/end mouse pos here, they might be needed if mouse button is still down
        if (selectionImage != null)
        {
            selectionImage.sizeDelta = Vector2.zero; // Hide by setting size to zero
            selectionImage.gameObject.SetActive(false); // Also deactivate
        }
        // Logical rect is reset on MouseButtonDown
    }
}