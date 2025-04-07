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

                    // Clamp gizmo itself
                    targetPos.x = Mathf.Clamp(targetPos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                    targetPos.z = Mathf.Clamp(targetPos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);

                    transform.position = targetPos;

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
                        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f); // prevent collapse or extreme explode

                        Vector3 gizmoPos = transform.position;

                        foreach (GameObject marcher in selectedMarchers.selectedMarchers)
                        {
                            Vector3 direction = marcher.transform.position - gizmoPos;
                            direction.y = 0; // keep movement on horizontal plane

                            Vector3 newPos = gizmoPos + direction * scaleFactor;

                            // Clamp to field bounds (X = min.x to max.x, Z = min.y to max.y)
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
        }
    }

    public void SetMode(string newMode)
    {
        mode = newMode;
        rotateVisualizer.SetActive(mode == "rotate");
        scaleVisualizer.SetActive(mode == "scale");
    }
}
