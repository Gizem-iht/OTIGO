using UnityEngine;
using UnityEngine.UI;

public class CorrectImageSelectableItem : MonoBehaviour
{
    [Tooltip("Bu görselin kimliği")]
    public string itemId;

    [Tooltip("GameManager referansı")]
    public CorrectImageSelectImageManager gameManager;

    [HideInInspector] public Image image;

    private Button button;

    private void Awake()
    {
        image = GetComponent<Image>();

        if (image == null)
            image = GetComponentInChildren<Image>();

        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickItem);
        }
        else
        {
            Debug.LogWarning($"{name} üzerinde Button component yok!");
        }
    }

    private void OnClickItem()
    {
        if (gameManager != null)
        {
            gameManager.OnItemSelected(itemId);
        }
        else
        {
            Debug.LogWarning($"{name} için gameManager atanmadı!");
        }
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}