using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMarcherSelector : MonoBehaviour
{
    // Core References (Ensure assigned in Inspector)
    public SelectedMarchers selectedMarchers;
    public TransformGizmoManager transformGizmoManager;
    public EnsembleDirector2 director; // Needed for accessing the list of all marchers
    public TransformGizmoUIButtonManager gizmoButtonUI;

    [Header("Drag Selection")]
    public RectTransform selectionImage; // Assign the UI Image/Panel for the selection box
    public float dragThreshold = 10f; // Min pixels mouse must move to trigger drag

    // Internal State
    private static int lastInputFrame = -1;
    private bool isDragging = false;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private Rect selectionRect;
    private bool dragSelectorLocked = false;

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
        if (lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;
        
        // 👆 BLOCK: If two or more fingers are touching, disable drag selector
        int touchCount = Input.touchCount;

// 🔒 Lock drag when 2+ fingers are down
        if (touchCount >= 2)
        {
            if (!dragSelectorLocked)
            {
                Debug.Log("⛔ Locking drag selector due to 2-finger touch.");
                dragSelectorLocked = true;
                isDragging = false;
                ResetSelectionBox();
            }

            return; // Exit early to block drag logic
        }

// 🔓 Unlock drag only when all fingers are lifted
        if (dragSelectorLocked && touchCount == 0)
        {
            Debug.Log("✅ Drag selector unlocked (all fingers lifted).");
            dragSelectorLocked = false;
        }


        // 🔒 Global input blocks
        if (UIInteractionBlocker.BlockSceneInputThisFrame || UIInteractionBlocker.IsTouchOverUI())
        {
            Debug.Log("⛔ Input blocked: UI interaction or touch over UI.");
            return;
        }

        // 🤚 Block selection while camera gesture is active or recently used
        if (TouchInputContext.IsCameraGestureActive || TouchInputContext.IsRecentGesture)
        {
            return;
        }

        // 🖱️ Standard desktop UI check (for mouse-over UI elements)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (isDragging && Input.GetMouseButtonUp(0))
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
                if (!Input.GetKey(KeyCode.LeftShift) || !MobileModifierKeyProxy.IsShiftHeld && !Input.GetKey(KeyCode.LeftControl) || !MobileModifierKeyProxy.IsControlHeld)
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
                if (Input.GetKey(KeyCode.LeftControl) || MobileModifierKeyProxy.IsControlHeld)
                {
                    DeSelectMarchersInDrag();
                }
                else
                {
                    SelectMarchersInDrag(Input.GetKey(KeyCode.LeftShift) || MobileModifierKeyProxy.IsShiftHeld); // Pass additive flag
                }
                //selectedMarchers.UpdateCameraFocus(); // Update camera after potential selection change
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
                if (Input.GetKey(KeyCode.LeftControl) || MobileModifierKeyProxy.IsControlHeld)
                {
                    // Raycast ONLY for marchers
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
                    {
                        if (hit.collider?.gameObject?.GetComponent<Unit>() != null)
                        {
                            selectedMarchers.Deselect(hit.collider.gameObject);

                            //selectedMarchers.UpdateCameraFocus();
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
                            if (gizmoButtonUI != null)
                            {
                                gizmoButtonUI.SetVisualGizmoOff();
                            }
                            return; // Handled click-away
                        }
                    }
                    // C) Handle Re-anchoring Gizmo (Shift+Click on selected marcher)
                    // Check marcher layer specifically
                    else if (hit.collider != null && selectedMarchers.marcherLayer == (selectedMarchers.marcherLayer | (1 << hit.collider.gameObject.layer)))
                    {
                        GameObject clickedMarcher = hit.collider.gameObject;
                        if (Input.GetKey(KeyCode.LeftShift) || MobileModifierKeyProxy.IsShiftHeld && selectedMarchers.IsSelected(clickedMarcher))
                        {
                            Debug.Log("MarcherSelector: Reanchoring gizmo to selected marcher.");
                            transformGizmoManager.ReanchorGizmoToMarcher(clickedMarcher);
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
                            if (Input.GetKey(KeyCode.LeftShift) || MobileModifierKeyProxy.IsShiftHeld) // Additive selection
                            {
                                if (!selectedMarchers.IsSelected(clicked))
                                {
                                    selectedMarchers.Select(clicked);
                                }
                            }
                            else // Regular click - select only this one
                            {
                                Debug.Log($"MarcherSelector: Selecting marcher {clicked.name}.");
                                selectedMarchers.ClearSelection(); // Clear first
                                selectedMarchers.Select(clicked);
                            }
                            //selectedMarchers.UpdateCameraFocus();
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
        if (selectionImage == null || !isDragging) return;

        // Convert screen positions to canvas local positions
        RectTransform parentRect = selectionImage.parent as RectTransform;

        Vector2 localStart;
        Vector2 localEnd;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, startMousePos, null, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, endMousePos, null, out localEnd);

        // Calculate center and size
        Vector2 center = (localStart + localEnd) / 2f;
        Vector2 size = new Vector2(
            Mathf.Abs(localStart.x - localEnd.x),
            Mathf.Abs(localStart.y - localEnd.y)
        );

        selectionImage.anchoredPosition = center;
        selectionImage.sizeDelta = size;
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
                if (!selectedMarchers.IsSelected(marcherGO)) // Select if not already selected
                {
                    selectedMarchers.Select(marcherGO);
                     Debug.Log($"{marcherGO.name} selected via drag.");
                }
            }
            else // If not in the current drag box
            {
                 // If NOT additive selection, deselect marchers outside the box
                 if (!isAdditive && selectedMarchers.IsSelected(marcherGO))
                 {
                      selectedMarchers.Deselect(marcherGO);
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
             if (selectedMarchers.IsSelected(marcherGO))
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
             selectedMarchers.Deselect(marcherToDeselect);
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