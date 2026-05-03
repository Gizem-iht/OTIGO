using UnityEngine;
using UnityEngine.UI;

public class ObjectSelectBackgroundColorChanger : MonoBehaviour
{
    public Camera mainCamera;
    public Image backgroundImage;

    public Color[] colors;
    private int currentIndex = 0;

    private void Start()
    {
        ApplyColor();
    }

    public void ChangeColor()
    {
        if (colors == null || colors.Length == 0) return;

        currentIndex++;
        if (currentIndex >= colors.Length)
            currentIndex = 0;

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (colors == null || colors.Length == 0) return;

        Color selectedColor = colors[currentIndex];

        if (mainCamera != null)
            mainCamera.backgroundColor = selectedColor;

        if (backgroundImage != null)
            backgroundImage.color = selectedColor;
    }
}