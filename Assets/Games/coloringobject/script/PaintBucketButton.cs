using UnityEngine;
using UnityEngine.UI;

public class PaintBucketButton : MonoBehaviour
{
    public ColoringBaseController controller;
    public Color bucketColor = Color.red;
    public Button button;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SelectBucketColor);
        }
    }

    private void SelectBucketColor()
    {
        Debug.Log("BOYA SEÇİLDİ: " + bucketColor);

        if (controller == null)
        {
            Debug.LogError("PaintBucketButton controller boş!");
            return;
        }

        controller.SelectColor(bucketColor);
    }
}