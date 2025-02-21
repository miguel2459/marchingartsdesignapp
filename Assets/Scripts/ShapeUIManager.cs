using UnityEngine;
using UnityEngine.UI;

public class ShapeUIManager : MonoBehaviour
{
    public ShapeMarchers shapeMarchers;

    // Assign your shape buttons in the Inspector
    public Button boxButton;
    public Button curveButton;
    public Button circleButton;
    public Button lineButton;

    private void Start()
    {
        // Set default shape to Box
        SetActiveShape(ShapeMarchers.ShapeType.Box);

        // Add listeners to buttons for click events
        boxButton.onClick.AddListener(() => OnShapeButtonClick(ShapeMarchers.ShapeType.Box));
        curveButton.onClick.AddListener(() => OnShapeButtonClick(ShapeMarchers.ShapeType.Curve));
        circleButton.onClick.AddListener(() => OnShapeButtonClick(ShapeMarchers.ShapeType.Circle));
        lineButton.onClick.AddListener(() => OnShapeButtonClick(ShapeMarchers.ShapeType.Line));
    }

    private void OnShapeButtonClick(ShapeMarchers.ShapeType shapeType)
    {
        // Set the active shape in ShapeMarchers and update button states
        SetActiveShape(shapeType);
    }

    private void SetActiveShape(ShapeMarchers.ShapeType shapeType)
    {
        // Enable all buttons
        EnableAllButtons();

        // Disable the selected shape button
        switch (shapeType)
        {
            case ShapeMarchers.ShapeType.Box:
                boxButton.interactable = false;
                break;
            case ShapeMarchers.ShapeType.Curve:
                curveButton.interactable = false;
                break;
            case ShapeMarchers.ShapeType.Circle:
                circleButton.interactable = false;
                break;
            case ShapeMarchers.ShapeType.Line:
                lineButton.interactable = false;
                break;
        }
    }

    private void EnableAllButtons()
    {
        // Re-enable all buttons to allow switching shapes
        boxButton.interactable = true;
        curveButton.interactable = true;
        circleButton.interactable = true;
        lineButton.interactable = true;
    }

    public ShapeMarchers.ShapeType GetCurrentShape()
    {
        // Determine which button is currently inactive (not interactable) and return the respective shape type
        if (!boxButton.interactable)
        {
            return ShapeMarchers.ShapeType.Box;
        }
        else if (!curveButton.interactable)
        {
            return ShapeMarchers.ShapeType.Curve;
        }
        else if (!circleButton.interactable)
        {
            return ShapeMarchers.ShapeType.Circle;
        }
        else if (!lineButton.interactable)
        {
            return ShapeMarchers.ShapeType.Line;
        }

        // Default to Box if no button is found inactive
        return ShapeMarchers.ShapeType.Box;
    }
}