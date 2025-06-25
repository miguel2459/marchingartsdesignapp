using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MarcherDragSelector
{
    private RectTransform selectionImage;
    private MarcherSelectionManager selectionManager;
    private TransformGizmoManager gizmoManager;
    private Camera mainCam;

    private Vector2 startMousePos;
    private Rect selectionRect;
    private bool isDragging = false;
    private bool dragLocked = false;

    public bool IsDragging => isDragging;
    public bool IsLocked => dragLocked;

    public MarcherDragSelector(
        RectTransform selectionImage,
        MarcherSelectionManager selectionManager,
        TransformGizmoManager gizmoManager,
        Camera camera)
    {
        this.selectionImage = selectionImage;
        this.selectionManager = selectionManager;
        this.gizmoManager = gizmoManager;
        this.mainCam = camera;
    }

    // Called externally when Unity's touch count changes
    public void OnTouchCountChanged(int count)
    {
        if (count >= 2)
        {
            CancelDrag();
            dragLocked = true;
        }
        else if (count == 0)
        {
            dragLocked = false;
        }
    }

    public void StartDrag(Vector2 mousePos)
    {
        // if (dragLocked || !selectionManager.IsDragAllowed()) return;

        startMousePos = mousePos;
        isDragging = true;
        UpdateSelectionBoxVisual(mousePos);
    }

    public void UpdateDrag(Vector2 currentMousePos)
    {
        if (!isDragging || dragLocked) return;
        UpdateSelectionBoxVisual(currentMousePos);
    }

    public void EndDrag(Vector2 endMousePos, bool isShiftHeld, bool isCtrlHeld)
    {
        if (!isDragging || dragLocked) return;

        UpdateSelectionBoxVisual(endMousePos);
        ApplySelection(isShiftHeld, isCtrlHeld);

        isDragging = false;
        ResetSelectionBox();
    }

    public void CancelDrag()
    {
        isDragging = false;
        ResetSelectionBox();
    }

    private void ApplySelection(bool shift, bool ctrl)
    {
        Vector2 min = selectionRect.min;
        Vector2 max = selectionRect.max;

        // foreach (var marcher in selectionManager.AllMarchers)
        // {
        //     Vector3 screenPos = mainCam.WorldToScreenPoint(marcher.transform.position);
        //
        //     if (screenPos.x > min.x && screenPos.x < max.x &&
        //         screenPos.y > min.y && screenPos.y < max.y)
        //     {
        //         if (ctrl)
        //             selectionManager.DeselectMarcher(marcher);
        //         else
        //             selectionManager.SelectMarcher(marcher, addToSelection: shift || ctrl);
        //     }
        // }
    }

    private void UpdateSelectionBoxVisual(Vector2 currentMousePos)
    {
        if (selectionImage == null) return;

        float width = currentMousePos.x - startMousePos.x;
        float height = currentMousePos.y - startMousePos.y;

        selectionRect = new Rect(startMousePos, new Vector2(width, height));

        selectionImage.gameObject.SetActive(true);
        selectionImage.anchoredPosition = startMousePos;
        selectionImage.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionImage.pivot = new Vector2(width >= 0 ? 0 : 1, height >= 0 ? 0 : 1);
    }

    private void ResetSelectionBox()
    {
        if (selectionImage != null)
        {
            selectionImage.sizeDelta = Vector2.zero;
            selectionImage.gameObject.SetActive(false);
        }

        selectionRect = Rect.zero;
    }
}
