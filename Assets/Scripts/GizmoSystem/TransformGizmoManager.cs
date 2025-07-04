using UnityEngine;

public class TransformGizmoManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GameObject unifiedGizmoPrefab;
    public Camera cam;
    public SelectedMarchers selectedMarchers;
    public SnapToGridLines snapToGrid;
    public MarcherPositionHistory history;
    [SerializeField] private TransformGizmoUIButtonManager gizmoButtonUI;

    [Header("Layer Masks")]
    public LayerMask gizmoLayer;
    public LayerMask marcherLayer;

    // Internal State
    private GameObject activeGizmo;
    private GizmoMode? currentMode = GizmoMode.Position;
    private Plane movePlane;
    private Vector3 offset;
    private bool isMoving = false;
    private bool isFreeDraggingGizmo = false;

    // Public Properties
    public bool HasActiveGizmo => activeGizmo != null;
    public GameObject transformGizmo => activeGizmo;
    public bool IsFreeDraggingGizmo => isFreeDraggingGizmo;
    public bool IsGizmoMoving => isMoving;

    // ========================================================
    // 🔁 Unity Lifecycle
    // ========================================================
    void Update()
    {
        if (selectedMarchers.SelectedCount == 0)
        {
            if (activeGizmo)
            {
                Destroy(activeGizmo);
                activeGizmo = null;
            }
            return;
        }

        HandleHotkeyToggle();
        HandleMouseDown();
        HandleShiftRelease();
        HandleMouseUp();
    }

    // ========================================================
    // 🔁 Event Handlers
    // ========================================================
    private void HandleHotkeyToggle()
    {
        if (GizmoInputHandler.IsPositionKeyPressed()) SetMode(GizmoMode.Position);
        if (GizmoInputHandler.IsRotateKeyPressed()) SetMode(GizmoMode.Rotate);
        if (GizmoInputHandler.IsScaleKeyPressed()) SetMode(GizmoMode.Scale);
    }

    private void HandleMouseDown()
    {
        if (!Input.GetMouseButtonDown(0) || Input.GetKey(KeyCode.LeftAlt)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // ✅ Clicked gizmo: initiate drag
        if (HasActiveGizmo && Physics.Raycast(ray, out hit, 1000f, gizmoLayer))
        {
            if (ModifierInput.ShiftHeld)
            {
                Debug.Log("TransformGizmoManager: Shift+Clicked gizmo - initiating freeform drag.");
                isFreeDraggingGizmo = true;

                movePlane = new Plane(Vector3.up, hit.point);
                if (movePlane.Raycast(ray, out float distance))
                    offset = ray.GetPoint(distance) - activeGizmo.transform.position;

                selectedMarchers.ForEachSelected(m => m.transform.SetParent(null));
                return;
            }

            Debug.Log("TransformGizmoManager: Regular gizmo click - initiating standard drag.");
            isMoving = true;
            return;
        }

        // ✅ Clicked marcher: try to reanchor
        if (Physics.Raycast(ray, out hit, 1000f, marcherLayer))
        {
            if (TryReanchorGizmoIfApplicable(hit))
                return;
        }
    }

    private void HandleShiftRelease()
    {
        if (!isFreeDraggingGizmo) return;

        if (!ModifierInput.ShiftHeld)
        {
            isFreeDraggingGizmo = false;
            selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
            Debug.Log("TransformGizmoManager: Shift released — ending freeform reanchoring.");
            MobileModifierKeyProxy.SetShiftHeld(false);
        }
    }

    private void HandleMouseUp()
    {
        if (!Input.GetMouseButtonUp(0)) return;

        if (isMoving)
        {
            isMoving = false;

            if (isFreeDraggingGizmo)
            {
                isFreeDraggingGizmo = false;
                selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
            }
        }
    }

    // ========================================================
    // 🧠 Public API
    // ========================================================
    public void SetMode(GizmoMode mode)
    {
        if (HasActiveGizmo && currentMode == mode)
        {
            HideTransformGizmo();
            currentMode = null;
            return;
        }

        currentMode = mode;

        if (HasActiveGizmo)
        {
            var behavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
            behavior?.SetMode(currentMode.Value);
            gizmoButtonUI?.UpdateVisualFromExternalMode(mode);
            return;
        }

        // 🧠 Spawn new gizmo
        Vector3 center = Vector3.zero;
        int count = 0;
        selectedMarchers.ForEachSelected(m =>
        {
            center += m.transform.position;
            count++;
        });
        center /= Mathf.Max(1, count);

        activeGizmo = Instantiate(unifiedGizmoPrefab, center, Quaternion.identity, transform);
        var newBehavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
        if (newBehavior)
        {
            newBehavior.cam = cam;
            newBehavior.selectedMarchers = selectedMarchers;
            newBehavior.snapToGrid = snapToGrid;
            newBehavior.SetMode(currentMode.Value);
            newBehavior.gizmoManager = this;
            newBehavior.positionHistory = history;
        }

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
        selectedMarchers.ReCacheAnchorsForSelected();
        gizmoButtonUI?.UpdateVisualFromExternalMode(mode);
    }

    public void ReanchorGizmoToMarcher(GameObject marcher)
    {
        if (!HasActiveGizmo || marcher == null) return;

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(null));

        activeGizmo.transform.position = marcher.transform.position;
        Debug.Log($"TransformGizmoManager: Gizmo reanchored to {marcher.name} at {marcher.transform.position}");

        selectedMarchers.ForEachSelected(m => m.transform.SetParent(activeGizmo.transform));
        activeGizmo.GetComponent<UnifiedGizmoBehavior>()?.ForceHandleUpdate();
    }

    public void HideTransformGizmo()
    {
        if (!HasActiveGizmo) return;

        Transform ensembleParent = selectedMarchers.director.transform;

        selectedMarchers.ForEachSelected(m =>
        {
            if (m != null) m.transform.SetParent(ensembleParent);
        });

        Destroy(activeGizmo);
        activeGizmo = null;
        gizmoButtonUI?.SetVisualGizmoOff();
    }

    public bool TryReanchorGizmoIfApplicable(RaycastHit hit)
    {
        if (!HasActiveGizmo || hit.collider == null) return false;

        GameObject hitObject = hit.collider.gameObject;

        if (selectedMarchers.IsSelected(hitObject) && ModifierInput.ShiftHeld)
        {
            Debug.Log("TransformGizmoManager: Reanchoring gizmo to selected marcher via Shift+click.");
            ReanchorGizmoToMarcher(hitObject);
            MobileModifierKeyProxy.SetShiftHeld(false);
            return true;
        }

        return false;
    }

    public void SetActiveCamera(Camera activeCam)
    {
        cam = activeCam;

        if (HasActiveGizmo)
        {
            var behavior = activeGizmo.GetComponent<UnifiedGizmoBehavior>();
            if (behavior != null)
            {
                behavior.cam = activeCam;
            }
        }
    }
}
