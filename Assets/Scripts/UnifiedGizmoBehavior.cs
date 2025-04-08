using UnityEngine;

public class UnifiedGizmoBehavior : MonoBehaviour
{
    public Camera cam;
    public SelectedMarchers selectedMarchers;
    public SnapToGridLines snapToGrid;

    public GameObject rotateVisualizer;
    public GameObject scaleVisualizer;

    private string mode = "position"; // Modes: position, rotate, scale
    private Plane movePlane;
    private Vector3 offset;
    private bool isDragging = false;
    public TransformGizmoManager gizmoManager;
    private string activeAxis = "center"; // center, x, z
    public bool IsDragging()
    {
        return isDragging;
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                {
                    isDragging = true;
                    movePlane = new Plane(Vector3.up, transform.position);
                    float distance;
                    if (movePlane.Raycast(ray, out distance))
                    {
                        offset = ray.GetPoint(distance) - transform.position;
                    }

                    // Determine which axis handle is clicked
                    string hitName = hit.collider.gameObject.name.ToLower();
                    if (hitName.Contains("handle_x")) activeAxis = "x";
                    else if (hitName.Contains("handle_z")) activeAxis = "z";
                    else activeAxis = "center";
                }
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            float distance;

            if (movePlane.Raycast(ray, out distance))
            {
                Vector3 targetPos = ray.GetPoint(distance) - offset;
                targetPos.y = transform.position.y; // lock Y-axis

                if (mode == "position")
                {
                    if (Input.GetKey(KeyCode.Q))
                    {
                        targetPos = snapToGrid.GetSnappedGizmoPosition(targetPos);
                    }

                    Vector3 newPos = transform.position;
                    if (activeAxis == "x" || activeAxis == "center")
                        newPos.x = Mathf.Clamp(targetPos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                    if (activeAxis == "z" || activeAxis == "center")
                        newPos.z = Mathf.Clamp(targetPos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

                    transform.position = newPos;

                    // Clamp each marcher to field bounds (post-move)
                    foreach (GameObject marcher in selectedMarchers.selectedMarchers)
                    {
                        Vector3 pos = marcher.transform.position;
                        pos.x = Mathf.Clamp(pos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                        pos.z = Mathf.Clamp(pos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
                        marcher.transform.position = pos;
                    }
                }

                else if ((mode == "rotate" || mode == "scale") && gizmoManager != null && !gizmoManager.IsFreeDraggingGizmo)
                {
                    float mouseDelta = Input.GetAxis("Mouse X");

                    if (mode == "rotate")
                    {
                        transform.Rotate(Vector3.up, mouseDelta * 5f);

                        foreach (GameObject marcher in selectedMarchers.selectedMarchers)
                        {
                            Vector3 pos = marcher.transform.position;
                            pos.x = Mathf.Clamp(pos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                            pos.z = Mathf.Clamp(pos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
                            marcher.transform.position = pos;
                        }
                    }
                    else if (mode == "scale")
                    {
                        float scaleFactor = 1 + mouseDelta * 0.05f;
                        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f);

                        Vector3 gizmoPos = transform.position;

                        foreach (GameObject marcher in selectedMarchers.selectedMarchers)
                        {
                            Vector3 direction = marcher.transform.position - gizmoPos;
                            direction.y = 0;

                            Vector3 newPos = direction;
                            if (activeAxis == "x")
                                newPos = new Vector3(direction.x * scaleFactor, 0, direction.z);
                            else if (activeAxis == "z")
                                newPos = new Vector3(direction.x, 0, direction.z * scaleFactor);
                            else
                                newPos *= scaleFactor;

                            newPos = gizmoPos + newPos;
                            newPos.x = Mathf.Clamp(newPos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                            newPos.z = Mathf.Clamp(newPos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

                            marcher.transform.position = newPos;
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            activeAxis = "center"; // reset

            if (mode == "rotate")
            {
                // 🔓 Temporarily unparent marchers
                foreach (var marcher in selectedMarchers.selectedMarchers)
                {
                    marcher.transform.SetParent(null);
                }

                // 🔄 Reset gizmo rotation
                Vector3 currentRotation = transform.eulerAngles;
                transform.eulerAngles = new Vector3(currentRotation.x, 0f, currentRotation.z);
                Debug.Log("UnifiedGizmoBehavior: Rotation reset to Y = 0 after rotate interaction.");

                // 🔗 Re-parent marchers back to the gizmo
                foreach (var marcher in selectedMarchers.selectedMarchers)
                {
                    marcher.transform.SetParent(transform);
                }
            }
        }

    }

    public void SetMode(string newMode)
    {
        mode = newMode;
        rotateVisualizer.SetActive(mode == "rotate");
        scaleVisualizer.SetActive(mode == "scale");
    }
}
