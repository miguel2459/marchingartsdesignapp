using System.Collections.Generic;
using UnityEngine;

public class ClickMarcherSelector : MonoBehaviour
{
    
    private Vector3 mousePos;
    public SelectedMarchers selectedMarchers;

    void Start(){
        selectedMarchers = GetComponent<SelectedMarchers>();
    }
    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftAlt))
        {
            //Debug.Log("MarcherSelector: Left mouse button down - Starting mouse input handling.");

            Ray ray = selectedMarchers.cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectedMarchers.marcherLayer))
            {
                Debug.Log($"MarcherSelector: Raycast hit on {hit.collider.gameObject.name}.");

                if (hit.collider.gameObject != null && hit.collider.gameObject.CompareTag("Gizmo"))
                {
                    Debug.Log("MarcherSelector: Gizmo clicked - Skipping selection logic.");
                    // Skip selection logic if gizmo is clicked
                    return;
                }

                if (selectedMarchers.marcherLayer == (selectedMarchers.marcherLayer | (1 << hit.collider.gameObject.layer)))
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        selectedMarchers.DeselectMarcher(hit.collider.gameObject);
                        Debug.Log($"MarcherSelector: Ctrl key pressed - Deselecting marcher {hit.collider.gameObject.name}.");

                    }
                    else if (Input.GetKey(KeyCode.LeftShift))
                    {
                        Debug.Log($"MarcherSelector: Shift key pressed - Adding marcher {hit.collider.gameObject.name} to selection.");
                        selectedMarchers.SelectMarcher(hit.collider.gameObject);
                    }
                    else
                    {
                        Debug.Log($"MarcherSelector: Selecting marcher {hit.collider.gameObject.name}.");
                        selectedMarchers.ClearSelection();
                        selectedMarchers.SelectMarcher(hit.collider.gameObject);
                    }
                    selectedMarchers.UpdateCameraFocus();
                }
            }
            else if (!Input.GetKey(KeyCode.LeftAlt) && selectedMarchers.moveMarcher.transformGizmo == null) // Prevent clearing selection if Alt is pressed
            {
                selectedMarchers.ClearSelection();
                selectedMarchers.UpdateCameraFocus();
                //Debug.Log("MarcherSelector: Clicked on empty space - Clearing selection.");
            }
        }
    }
}
