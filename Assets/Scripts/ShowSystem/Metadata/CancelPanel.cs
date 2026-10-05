using UnityEngine;
using UnityEngine.EventSystems;

public class CancelPanel : MonoBehaviour
{
    [SerializeField] private RectTransform Panel; // inner confirm box
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Panel == null) return;

        // If click is outside the logout panel → cancel
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            Panel,
            eventData.position,
            eventData.pressEventCamera
        );

        if (!inside)
        {
            CancelLogout();
        }
    }

    public  void CancelLogout()
    {
        gameObject.SetActive(false);
    }
}
