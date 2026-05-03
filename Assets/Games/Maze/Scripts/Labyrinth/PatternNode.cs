using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternNode : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
{
    public int nodeId;
    public PatternLockController controller;
    public Image image;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private bool isSelected = false;
    private static bool isDragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        controller.AddNode(nodeId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging)
            controller.AddNode(nodeId);
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0) || Input.touchCount == 0)
            isDragging = false;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (image != null)
            image.color = isSelected ? selectedColor : normalColor;
    }

    public void SetColorDirect(Color color)
    {
        if (image != null)
            image.color = color;
    }
}