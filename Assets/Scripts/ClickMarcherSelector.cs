using System.Collections.Generic;
using UnityEngine;

public class ClickMarcherSelector : MonoBehaviour
{
    
    private Vector3 mousePos;
    public SelectedMarchers selectedMarchers;
    public MarcherMovement moveMarchers;

    void Start(){
        selectedMarchers = GetComponent<SelectedMarchers>();
        moveMarchers = GetComponent<MarcherMovement>();
    }
    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        mousePos = Input.mousePosition;

        // Handle Ctrl+Click deselection
        if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftControl) && !moveMarchers.transformGizmo)
        {
            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
            {
                if (hit.collider != null && hit.collider.gameObject != null && hit.collider.gameObject.GetComponent<Unit>())
                {
                    selectedMarchers.DeselectMarcher(hit.collider.gameObject);
                    selectedMarchers.UpdateCameraFocus();
                    return; // Exit after handling Ctrl+Click deselection
                }
            }
        }

        // Handle normal click selection (only if not dragging)
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && !moveMarchers.transformGizmo)
        {
            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer) && !Input.GetKey(KeyCode.LeftShift))
            {
                if (!Input.GetKey(KeyCode.LeftControl)) // Only clear if Ctrl is not held
                {
                    selectedMarchers.ClearSelection();
                }
                return;
            }

            if (hit.collider == null || hit.collider.gameObject == null)
            {
                return;
            }

            if (!hit.collider.gameObject.GetComponent<Renderer>() || !hit.collider.gameObject.GetComponent<Unit>())
            {
                return;
            }

            Debug.Log($"MarcherSelector: Raycast hit on {hit.collider.gameObject.name}.");

            if (hit.collider.gameObject.CompareTag("Gizmo"))
            {
                Debug.Log("MarcherSelector: Gizmo clicked - Skipping selection logic.");
                // Skip selection logic if gizmo is clicked
                return;
            }

            if (selectedMarchers.marcherLayer == (selectedMarchers.marcherLayer | (1 << hit.collider.gameObject.layer)))
            {
                // Handle only Shift selection on mouse down
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    Debug.Log($"MarcherSelector: Shift key pressed - Adding marcher {hit.collider.gameObject.name} to selection.");
                    selectedMarchers.SelectMarcher(hit.collider.gameObject);
                }
                else if (!Input.GetKey(KeyCode.LeftControl)) // Only select if Ctrl is not pressed
                {
                    Debug.Log($"MarcherSelector: Selecting marcher {hit.collider.gameObject.name}.");
                    selectedMarchers.ClearSelection();
                    selectedMarchers.SelectMarcher(hit.collider.gameObject);
                }
                selectedMarchers.UpdateCameraFocus();
            }
        }
    }
}
