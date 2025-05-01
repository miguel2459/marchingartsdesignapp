using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class TopDownCameraController : MonoBehaviour, ICameraFocusHandler
{
    [Header("Orthographic Settings")]
    public float defaultOrthoSize = 20f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 30f;
    public float zoomSpeed = 100f;
    public float panSpeed = 0.3f;

    [Header("Initial Fallback Height Logic")]
    public float yHeightMultiplier = 1.5f;

    private List<GameObject> selectedMarchers = new List<GameObject>();
    private bool isActive = false;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private float currentOrthoSize;
    private Camera cam;

    public void Enable()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographic = true;

        // Set initial transform (hardcoded startup position)
        transform.position = new Vector3(26.6f, 40f, 60f);
        transform.rotation = Quaternion.Euler(90f, 90f, 0f);

        SaveCurrentTransform();
        SnapToTopDown();
        isActive = true;
    }

    public void Disable()
    {
        RestorePreviousTransform();
        if (cam != null)
        {
            cam.orthographic = false;
        }
        isActive = false;
    }

    public void SetSelectedMarchers(List<GameObject> marchers)
    {
        selectedMarchers = marchers;
    }

    void Update()
    {
        if (!isActive) return;

        HandleZoom();
        HandlePan();
    }

    private void SaveCurrentTransform()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }

    private void RestorePreviousTransform()
    {
        transform.position = previousPosition;
        transform.rotation = previousRotation;
        if (cam != null)
        {
            cam.orthographicSize = defaultOrthoSize;
        }
    }

    private void SnapToTopDown()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null) return;

        float orthoSize;

        if (selectedMarchers == null || selectedMarchers.Count == 0)
        {
            orthoSize = defaultOrthoSize;
        }
        else
        {
            Bounds bounds = new Bounds(selectedMarchers[0].transform.position, Vector3.zero);
            foreach (var m in selectedMarchers)
                bounds.Encapsulate(m.transform.position);

            orthoSize = Mathf.Clamp(bounds.size.magnitude * yHeightMultiplier, minOrthoSize, maxOrthoSize);
            transform.position = new Vector3(bounds.center.x, transform.position.y, bounds.center.z);
        }

        cam.orthographicSize = orthoSize;
        currentOrthoSize = orthoSize;
    }

    private void HandleZoom()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        currentOrthoSize -= scrollDelta * zoomSpeed * Time.deltaTime;
        currentOrthoSize = Mathf.Clamp(currentOrthoSize, minOrthoSize, maxOrthoSize);

        if (cam != null)
        {
            cam.orthographicSize = currentOrthoSize;
        }
    }

    private void HandlePan()
    {
        if (Input.GetMouseButton(2)) // Middle mouse drag
        {
            float moveX = Input.GetAxis("Mouse X") * panSpeed;
            float moveZ = -Input.GetAxis("Mouse Y") * panSpeed;
            // Move relative to world space — top-down is fixed orientation
            transform.Translate(new Vector3(moveZ, 0f, moveX), Space.World);
        }
    }

    public void FocusOnSelection(Vector3 focalPoint)
    {
        if (selectedMarchers.Count == 0 || cam == null) return;

        // Lerp to center position (XZ only)
        Vector3 target = new Vector3(focalPoint.x, transform.position.y, focalPoint.z);
        StopAllCoroutines();
        StartCoroutine(LerpToPosition(target));
    }

    private IEnumerator LerpToPosition(Vector3 target)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 start = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }

    public bool IsActive() => isActive;
}
