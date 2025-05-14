using System.Collections.Generic;
using UnityEngine;

public interface ICameraFocusHandler
{
    void SetSelectedMarchers(List<GameObject> marchers);
    void FocusOnSelection(Vector3 focalPoint);
}
