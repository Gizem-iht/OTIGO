using UnityEngine;
using UnityEngine.UI;

public class BrushCursor : MonoBehaviour
{
    [Header("UI")]
    public RectTransform brushRect;
    public Image brushImage;

    [Header("Settings")]
    public bool followMouse = true;

    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (brushRect == null)
            brushRect = GetComponent<RectTransform>();

        if (brushImage == null)
            brushImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (!followMouse || canvas == null || canvasRect == null)
            return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        brushRect.anchoredPosition = localPoint;
    }

    public void SetBrushColor(Color color)
    {
        if (brushImage != null)
        {
            Color visibleColor = color;
            visibleColor.a = 1f;
            brushImage.color = visibleColor;
        }
    }

    public void ShowBrush(bool show)
    {
        gameObject.SetActive(show);
    }
}