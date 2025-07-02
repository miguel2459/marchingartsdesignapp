using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMarcherSelector : MonoBehaviour
{
    // ===============================
    // 🔗 References (Inspector)
    // ===============================
    public SelectedMarchers selectedMarchers;
    public TransformGizmoManager transformGizmoManager;
    public EnsembleDirector2 director;
    public TransformGizmoUIButtonManager gizmoButtonUI;

    [Header("Drag Selection UI")]
    public RectTransform selectionImage;
    public float dragThreshold = 10f;

    // ===============================
    // 🧠 Internal State
    // ===============================
    private static int lastInputFrame = -1;
    private bool isDragging = false;
    private bool dragSelectorLocked = false;
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private Rect selectionRect;

    // ===============================
    // 🔄 Unity Lifecycle
    // ===============================
    void Start()
    {
        if (selectionImage != null)
        {
            selectionImage.gameObject.SetActive(false);
        }
        ResetSelectionBox();
    }

    void Update()
    {
        HandleMouseInput();
    }

    // ===============================
    // 🖱️ Main Input Handling
    // ===============================
    void HandleMouseInput()
    {
        if (lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;

        int touchCount = Input.touchCount;

        // 🔒 Lock drag if two+ fingers
        if (touchCount >= 2)
        {
            if (!dragSelectorLocked)
            {
                Debug.Log("⛔ Locking drag selector due to 2-finger touch.");
                dragSelectorLocked = true;
                EndDrag();
            }
            return;
        }

        // 🔓 Unlock drag when all fingers are lifted
        if (dragSelectorLocked && touchCount == 0)
        {
            Debug.Log("✅ Drag selector unlocked (all fingers lifted).");
            dragSelectorLocked = false;
        }

        // 🛑 Global UI or gesture blocking
        if (UIInteractionBlocker.BlockSceneInputThisFrame || UIInteractionBlocker.IsTouchOverUI()) return;
        if (TouchInputContext.IsCameraGestureActive || TouchInputContext.IsRecentGesture) return;

        // ❌ Lock out while gizmo is active
        if (transformGizmoManager.HasActiveGizmo)
        {
            if (Input.GetMouseButtonUp(0)) EndDrag();
            return;
        }

        // 🛑 Ignore mouse input over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (isDragging && Input.GetMouseButtonUp(0)) EndDrag();
            return;
        }

        // ===============================
        // 👇 Mouse Button Down
        // ===============================
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            startMousePos = Input.mousePosition;
            isDragging = false;
            selectionRect = new Rect();

            if (selectionImage != null)
            {
                selectionImage.gameObject.SetActive(true);
                selectionImage.sizeDelta = Vector2.zero;
            }
        }

        // ===============================
        // ✏️ Mouse Held (Drag)
        // ===============================
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            endMousePos = Input.mousePosition;

            if (!isDragging && Vector2.Distance(endMousePos, startMousePos) > dragThreshold)
            {
                isDragging = true;
                Debug.Log("🖱️ Drag detected.");

                if (ModifierInput.NoModifier)
                {
                    Debug.Log("🧹 Clearing selection for new drag (no modifier held).");
                    selectedMarchers.ClearSelection();
                }
            }

            if (isDragging)
            {
                selectionRect = new Rect(
                    Mathf.Min(startMousePos.x, endMousePos.x),
                    Mathf.Min(startMousePos.y, endMousePos.y),
                    Mathf.Abs(startMousePos.x - endMousePos.x),
                    Mathf.Abs(startMousePos.y - endMousePos.y)
                );
                UpdateSelectionBoxVisual();
            }
        }

        // ===============================
        // 🖱️ Mouse Button Up (Click or Drag End)
        // ===============================
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            if (dragSelectorLocked)
            {
                Debug.Log("🛑 Drag selector was locked — skipping MouseUp selection logic.");
                EndDrag();
                return;
            }

            if (isDragging)
            {
                Debug.Log("🖱️ Drag finished.");
                if (ModifierInput.ControlHeld)
                    DeSelectMarchersInDrag();
                else
                    SelectMarchersInDrag(ModifierInput.ShiftHeld);
            }
            else
            {
                Debug.Log("🖱️ Click detected.");
                Ray ray = selectedMarchers.cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (ModifierInput.ControlHeld || ModifierInput.ShiftHeld)
                {
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
                    {
                        if (hit.collider?.gameObject?.GetComponent<Unit>() != null)
                        {
                            if (ModifierInput.ControlHeld)
                                selectedMarchers.Deselect(hit.collider.gameObject);
                            else if (ModifierInput.ShiftHeld && !selectedMarchers.IsSelected(hit.collider.gameObject))
                                selectedMarchers.Select(hit.collider.gameObject);

                            EndDrag();
                            return;
                        }
                    }

                    // No ClearSelection if Shift or Ctrl held and click-away
                    EndDrag();
                    return;
                }
                else
                {
                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
                    {
                        GameObject clicked = hit.collider.gameObject;
                        if (clicked?.GetComponent<Unit>() != null)
                        {
                            if (ModifierInput.ShiftHeld && !selectedMarchers.IsSelected(clicked))
                            {
                                selectedMarchers.Select(clicked);
                            }
                            else if (!ModifierInput.ShiftHeld)
                            {
                                Debug.Log($"MarcherSelector: Selecting marcher {clicked.name}.");
                                selectedMarchers.ClearSelection();
                                selectedMarchers.Select(clicked);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("MarcherSelector: Clicked empty space - clearing selection.");
                        selectedMarchers.ClearSelection();
                    }
                }
            }

            EndDrag();
        }
    }

    // ===============================
    // 🔲 Selection Box Visuals
    // ===============================
    void UpdateSelectionBoxVisual()
    {
        if (selectionImage == null || !isDragging) return;

        RectTransform parentRect = selectionImage.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, startMousePos, null, out Vector2 localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, endMousePos, null, out Vector2 localEnd);

        Vector2 center = (localStart + localEnd) / 2f;
        Vector2 size = new Vector2(Mathf.Abs(localStart.x - localEnd.x), Mathf.Abs(localStart.y - localEnd.y));

        selectionImage.anchoredPosition = center;
        selectionImage.sizeDelta = size;
    }

    // ===============================
    // ✅ Drag Select / Deselect Logic
    // ===============================
    void SelectMarchersInDrag(bool isAdditive)
    {
        if (director?.Marchers == null || director.Marchers.Count == 0) return;

        Camera cam = selectedMarchers.cam;
        if (cam == null) { Debug.LogError("Camera reference is missing!"); return; }

        foreach (MarcherPositionsManager marcher in director.Marchers)
        {
            GameObject go = marcher.gameObject;
            Vector3 screenPos = cam.WorldToScreenPoint(go.transform.position);

            if (screenPos.z > 0 && selectionRect.Contains(screenPos))
            {
                if (!selectedMarchers.IsSelected(go))
                {
                    selectedMarchers.Select(go);
                    Debug.Log($"{go.name} selected via drag.");
                }
            }
            else if (!isAdditive && selectedMarchers.IsSelected(go))
            {
                selectedMarchers.Deselect(go);
                Debug.Log($"{go.name} deselected (outside drag area).");
            }
        }
    }

    void DeSelectMarchersInDrag()
    {
        if (director?.Marchers == null || director.Marchers.Count == 0) return;

        Camera cam = selectedMarchers.cam;
        if (cam == null) { Debug.LogError("Camera reference is missing!"); return; }

        List<GameObject> toDeselect = new List<GameObject>();

        foreach (MarcherPositionsManager marcher in director.Marchers)
        {
            GameObject go = marcher.gameObject;
            if (selectedMarchers.IsSelected(go))
            {
                Vector3 screenPos = cam.WorldToScreenPoint(go.transform.position);
                if (screenPos.z > 0 && selectionRect.Contains(screenPos))
                {
                    toDeselect.Add(go);
                }
            }
        }

        foreach (GameObject go in toDeselect)
        {
            selectedMarchers.Deselect(go);
            Debug.Log($"{go.name} deselected via Ctrl+drag.");
        }
    }

    // ===============================
    // 🧹 Utility
    // ===============================
    private void ResetSelectionBox()
    {
        if (selectionImage != null)
        {
            selectionImage.sizeDelta = Vector2.zero;
            selectionImage.gameObject.SetActive(false);
        }
    }

    private void EndDrag()
    {
        isDragging = false;
        ResetSelectionBox();
    }
}
