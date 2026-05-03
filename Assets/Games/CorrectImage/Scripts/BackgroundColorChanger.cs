using UnityEngine;
using UnityEngine.UI;

public class BackgroundColorChanger : MonoBehaviour
{
    [Header("Background Image")]
    public Image backgroundImage;   // Canvas > Background (Image)

    [Header("Color Options")]
    public Color[] colors;          // Inspector’dan pastel renkler

    private int currentIndex = 0;

    private void Start()
    {
        if (backgroundImage == null)
        {
            Debug.LogError("Background Image atanmadı!");
            return;
        }

        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("Color listesi boş!");
            return;
        }

        // Başlangıç rengi
        backgroundImage.color = colors[currentIndex];
    }

    // GameLevelController bunu çağıracak
    public void ChangeColor()
{
    currentIndex++;
    if (currentIndex >= colors.Length)
        currentIndex = 0;

    backgroundImage.color = colors[currentIndex];
    Debug.Log("COLOR INDEX: " + currentIndex);
}

}
