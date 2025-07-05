using System.Collections.Generic;
using UnityEngine;

public class UnifiedGizmoBehavior : MonoBehaviour
{
    public Camera cam;
    public SelectedMarchers selectedMarchers;
    public SnapToGridLines snapToGrid;
    public MarcherPositionHistory positionHistory;

    public GameObject rotateVisualizer;
    public GameObject scaleVisualizer;

    private GizmoMode mode = GizmoMode.Position;
    private Plane movePlane;
    private Vector3 offset;
    private bool isDragging = false;
    public TransformGizmoManager gizmoManager;
    private string activeAxis = "center"; // center, x, z
    private Dictionary<MarcherPositionsManager, Vector3> initialPositions = new Dictionary<MarcherPositionsManager, Vector3>();
    private Quaternion originalRotation;
    
    private Renderer activeHandleRenderer;
    private Color originalHandleColor;
    private readonly Color highlightRed = new Color(1f, 0.5f, 0.5f);
    private readonly Color highlightBlue = new Color(0.5f, 0.7f, 1f);
    private float cumulativeDeltaX = 0f;
    private float cumulativeDeltaZ = 0f;
    private float FreeDragMultiplier => Application.isMobilePlatform ? 0.25f : 0.4f;
    private float LockedDragMultiplier => Application.isMobilePlatform ? 0.35f : 0.5f;

    private const float SNAP_THRESHOLD = 0.25f;       // threshold to trigger a snap movement


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

            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
            foreach (var h in hits)
            {
                GameObject target = h.collider.gameObject;

                bool isGizmoPart = (target == gameObject || target.transform.IsChildOf(transform));
                bool isMarcher = target.layer == LayerMask.NameToLayer("Marcher");

                if (isGizmoPart && !isMarcher)
                {
                    hit = h; // capture valid gizmo hit
                    isDragging = true;
                    movePlane = new Plane(Vector3.up, transform.position);

                    if (movePlane.Raycast(ray, out float distance))
                    {
                        offset = ray.GetPoint(distance) - transform.position;
                    }

                    // Cache initial positions for undo
                    initialPositions.Clear();
                    selectedMarchers.ForEachSelected(marcher =>
                    {
                        if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                            initialPositions[posManager] = marcher.transform.position;
                    });

                    // Detect axis handle
                    string hitName = target.name.ToLower();
                    if (hitName.Contains("handle_x")) activeAxis = "x";
                    else if (hitName.Contains("handle_z")) activeAxis = "z";
                    else activeAxis = "center";
                    
                    // 🟡 Highlight the active handle if it has a renderer
                    activeHandleRenderer = target.GetComponent<Renderer>();
                    if (activeHandleRenderer != null)
                    {
                        originalHandleColor = activeHandleRenderer.material.GetColor("_Color");
                        if (activeAxis == "x")
                            activeHandleRenderer.material.SetColor("_Color", highlightRed);
                        else if (activeAxis == "z")
                            activeHandleRenderer.material.SetColor("_Color", highlightBlue);
                    }
                    
                    break; // stop once valid gizmo hit is found
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

                // Compute mouse drag projected onto screen-space axis
                Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                Vector3 axisWorld = activeAxis switch
                {
                    "x" => transform.right,
                    "z" => transform.forward,
                    _ => Vector3.zero
                };

                Vector3 screenStart = cam.WorldToScreenPoint(transform.position);
                Vector3 screenEnd = cam.WorldToScreenPoint(transform.position + axisWorld);
                Vector2 axisScreenDir = (screenEnd - screenStart).normalized;

                float projectedDelta = (activeAxis == "center") ? 0f : Vector2.Dot(mouseDelta, axisScreenDir);

                if (mode == GizmoMode.Position)
                {
                    Vector3 newPos = transform.position;

                    float multiplier = GridAlignProxy.IsGridLockActive ? LockedDragMultiplier : FreeDragMultiplier;
                    float delta = projectedDelta * multiplier;

                    if (activeAxis == "x")
                    {
                        if (GridAlignProxy.IsGridLockActive)
                        {
                            cumulativeDeltaX += delta;
                            if (Mathf.Abs(cumulativeDeltaX) >= SNAP_THRESHOLD)
                            {
                                float step = Mathf.Sign(cumulativeDeltaX) * (5f / 8f); // 8-to-5 step
                                Vector3 snapTarget = newPos;
                                snapTarget.x += step;
                                Vector3 snapped = snapToGrid.GetAxisSnappedPosition(snapTarget, "x");
                                newPos.x = snapped.x;
                                cumulativeDeltaX = 0f;
                            }
                        }
                        else
                        {
                            newPos.x += delta;
                        }
                    }
                    else if (activeAxis == "z")
                    {
                        if (GridAlignProxy.IsGridLockActive)
                        {
                            cumulativeDeltaZ += delta;
                            if (Mathf.Abs(cumulativeDeltaZ) >= SNAP_THRESHOLD)
                            {
                                float step = Mathf.Sign(cumulativeDeltaZ) * (5f / 8f);
                                Vector3 snapTarget = newPos;
                                snapTarget.z += step;
                                Vector3 snapped = snapToGrid.GetAxisSnappedPosition(snapTarget, "z");
                                newPos.z = snapped.z;
                                cumulativeDeltaZ = 0f;
                            }
                        }
                        else
                        {
                            newPos.z += delta;
                        }
                    }
                    else // center drag
                    {
                        newPos.x = targetPos.x;
                        newPos.z = targetPos.z;

                        if (GridAlignProxy.IsGridLockActive)
                        {
                            newPos = snapToGrid.GetAxisSnappedPosition(newPos, "center");
                        }
                    }

                    newPos.x = Mathf.Clamp(newPos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                    newPos.z = Mathf.Clamp(newPos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
                    transform.position = newPos;

                    selectedMarchers.ForEachSelected(marcher =>
                    {
                        Vector3 pos = marcher.transform.position;
                        pos.x = Mathf.Clamp(pos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                        pos.z = Mathf.Clamp(pos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
                        marcher.transform.position = pos;
                    });
                }


                else if ((mode == GizmoMode.Rotate || mode == GizmoMode.Scale) && gizmoManager != null && !gizmoManager.IsFreeDraggingGizmo)
                {
                    float effectiveDelta = (activeAxis == "center") ? mouseDelta.x : projectedDelta;
                    Debug.Log($"🔁 Manipulating mode {mode} with axis {activeAxis} | Δscreen: {effectiveDelta}");

                    if (mode == GizmoMode.Rotate)
                    {
                        transform.Rotate(Vector3.up, effectiveDelta * 5f);

                        selectedMarchers.ForEachSelected(marcher =>
                        {
                            Vector3 pos = marcher.transform.position;
                            pos.x = Mathf.Clamp(pos.x, snapToGrid.currentFieldMin.x, snapToGrid.currentFieldMax.x);
                            pos.z = Mathf.Clamp(pos.z, snapToGrid.currentFieldMin.y, snapToGrid.currentFieldMax.y);
                            marcher.transform.position = pos;
                        });
                    }
                    else if (mode == GizmoMode.Scale)
                    {
                        float scaleFactor = 1 + effectiveDelta * 0.05f;
                        scaleFactor = Mathf.Clamp(scaleFactor, 0.5f, 2f);

                        Vector3 gizmoPos = transform.position;
                        selectedMarchers.ForEachSelected(marcher =>
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
                        });
                    }
                }
            }
        }


        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            positionHistory.BeginBatch();
            
            selectedMarchers.ForEachSelected(marcher =>
            {
                if (marcher.TryGetComponent(out MarcherPositionsManager posManager))
                {
                    Vector3 newPos = marcher.transform.position;
                    
                    if (initialPositions.TryGetValue(posManager, out Vector3 initial))
                    {
                        positionHistory.RecordRawMovement(posManager, initial, newPos);
                    }
                }          
            });
            positionHistory.EndBatch();
            activeAxis = "center";
            
            if (mode == GizmoMode.Rotate)
            {
                // ✅ Temporarily unparent marchers
                selectedMarchers.ForEachSelected(m => m.transform.SetParent(null));

                // ✅ Reset gizmo rotation
                transform.rotation = originalRotation;
                Debug.Log("🔁 Gizmo rotation reset after drag.");

                // ✅ Reparent marchers back to gizmo
                selectedMarchers.ForEachSelected(m => m.transform.SetParent(transform));

                // 🔄 Optional: Update handles
                ForceHandleUpdate();
            }
            
            // 🔙 Restore original handle color
            if (activeHandleRenderer != null)
            {
                activeHandleRenderer.material.SetColor("_Color", originalHandleColor);
                activeHandleRenderer = null;
            }
            
            cumulativeDeltaX = 0f;
            cumulativeDeltaZ = 0f;
        }
    }
    
    public void ForceHandleUpdate()
    {
        // Force Unity to recalculate collider transforms
        foreach (BoxCollider col in GetComponentsInChildren<BoxCollider>())
        {
            col.enabled = false;
            col.enabled = true;
        }
    }

    public void SetMode(GizmoMode newMode)
    {
        mode = newMode;
        rotateVisualizer.SetActive(mode == GizmoMode.Rotate);
        scaleVisualizer.SetActive(mode == GizmoMode.Scale);
        if (mode == GizmoMode.Rotate)
        {
            originalRotation = transform.rotation;
        }
    }
    
    public GizmoMode GetCurrentMode()
    {
        return mode;
    }

    public string GetActiveAxis()
    {
        return activeAxis;
    }

}