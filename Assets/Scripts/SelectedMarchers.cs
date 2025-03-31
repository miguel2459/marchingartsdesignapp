using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Handles user selection of marchers and confirms set positions when spacebar is pressed.
/// </summary>
public class SelectedMarchers : MonoBehaviour
{
    [Header("Selection Settings")]
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;
    public LayerMask marcherLayer;

    [Header("References")]
    public CameraControl cameraControl;
    public Camera cam;
    public MarcherMovement moveMarcher;

    public List<GameObject> selectedMarchers = new List<GameObject>();
    public bool selectAllMarchers; // for inspector testing

    private void Update()
    {
        CheckForSpaceBarSetPosition();
    }

    /// <summary>
    /// If spacebar is pressed, confirms the transform.position as a SetPosition for each selected marcher.
    /// </summary>
    public void CheckForSpaceBarSetPosition()
    {
        if (Input.GetKeyDown(KeyCode.Space) && moveMarcher.transformGizmo != null && selectedMarchers.Count > 0)
        {
            int currentSet = int.Parse(SessionManager.instance.SessionState.LastSet);

            Debug.Log($"SelectedMarchers: ⏺️ Setting SetPosition for {selectedMarchers.Count} marchers on Set {currentSet}");

            foreach (GameObject marcher in selectedMarchers)
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    posManager.SetPosition(currentSet, marcher.transform.position);
                }
            }
        }
    }

    public void SelectMarcher(GameObject marcher)
    {
        if (!selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Add(marcher);
            marcher.GetComponent<Renderer>().material.color = highlightColor;
            marcher.GetComponent<Unit>()?.SetSelector(true);

            if (moveMarcher.transformGizmo != null)
            {
                marcher.transform.SetParent(moveMarcher.transformGizmo.transform);
            }

            Debug.Log($"SelectedMarchers: ✅ {marcher.name} selected.");
        }
    }

    public void DeselectMarcher(GameObject marcher)
    {
        if (selectedMarchers.Contains(marcher))
        {
            selectedMarchers.Remove(marcher);
            marcher.GetComponent<Renderer>().material.color = normalColor;
            marcher.GetComponent<Unit>()?.SetSelector(false);

            if (moveMarcher.transformGizmo != null)
            {
                marcher.transform.SetParent(transform);
            }

            Debug.Log($"SelectedMarchers: ❎ {marcher.name} deselected.");
        }
    }

    public void ClearSelection()
    {
        foreach (GameObject marcher in selectedMarchers.ToList())
        {
            if (marcher != null)
            {
                var renderer = marcher.GetComponent<Renderer>();
                var unit = marcher.GetComponent<Unit>();

                if (renderer != null) renderer.material.color = normalColor;
                if (unit != null) unit.SetSelector(false);
                if (moveMarcher?.transformGizmo != null) marcher.transform.SetParent(transform);
            }
        }

        selectedMarchers.Clear();
        moveMarcher?.HideTransformGizmo();
        moveMarcher.isMoving = false;

        Debug.Log("SelectedMarchers: 🧹 Selection cleared.");
    }

    public void UpdateCameraFocus()
    {
        cameraControl?.SetSelectedMarchers(selectedMarchers);
    }
}
