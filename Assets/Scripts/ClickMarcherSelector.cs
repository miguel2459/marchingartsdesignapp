using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickMarcherSelector : MonoBehaviour
{
    
    private Vector3 mousePos;
    public SelectedMarchers selectedMarchers;
    public TransformGizmoManager transformGizmoManager;


    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        mousePos = Input.mousePosition;

        // Handle Ctrl+Click deselection
        if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftControl) && !transformGizmoManager.HasActiveGizmo)
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

        // 🛠️ Handle click-away to hide gizmo even when gizmo is active
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && transformGizmoManager.HasActiveGizmo)
        {
            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            // 🧠 Prevent hiding the gizmo if user was just manipulating it
            var gizmo = transformGizmoManager.transformGizmo;
            var behavior = gizmo ? gizmo.GetComponent<UnifiedGizmoBehavior>() : null;
            bool wasDraggingGizmo = behavior && behavior.IsDragging();

            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer | transformGizmoManager.gizmoLayer))
            {
                if (!wasDraggingGizmo)
                {
                    Debug.Log("MarcherSelector: Clicked away from both marcher and gizmo — hiding gizmo.");
                    transformGizmoManager.HideTransformGizmo();
                    return;
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && transformGizmoManager.HasActiveGizmo)
        {
            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
            {
                GameObject clicked = hit.collider.gameObject;

                if (Input.GetKey(KeyCode.LeftShift) && selectedMarchers.selectedMarchers.Contains(clicked))
                {
                    Debug.Log("MarcherSelector: Reanchoring gizmo to selected marcher.");
                    selectedMarchers.ReanchorToExisting(clicked);
                }
            }
        }

        // Handle normal click selection (only if not dragging)
        if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt) && !transformGizmoManager.HasActiveGizmo)
        {
            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer) && !Input.GetKey(KeyCode.LeftShift))
            {
                if (!Input.GetKey(KeyCode.LeftControl)) // Only clear if Ctrl is not held
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return; // 👈 Prevent selection logic if clicking on UI
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
                    GameObject clicked = hit.collider.gameObject;

                    if (!selectedMarchers.selectedMarchers.Contains(clicked))
                    {
                        selectedMarchers.SelectMarcher(clicked);
                    }
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
