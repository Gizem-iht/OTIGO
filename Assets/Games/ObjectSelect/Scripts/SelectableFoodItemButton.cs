using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum ItemCategory
{
    Fruit,
    Vegetable,
    Animal,
    Toy,
    Vehicle,
    Object
}

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class SelectableFoodItemButton : MonoBehaviour
{
    [Header("Kategori")]
    public ItemCategory category = ItemCategory.Fruit;

    [Header("Görseller")]
    public Sprite normalSprite;
    public Sprite starSprite;

    private Image image;
    private Button button;
    private bool isCollected = false;
    private GameManager_SelectItems gameManager;

    private void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();

        if (normalSprite != null)
            image.sprite = normalSprite;
    }

    private void Update()
    {
        if (button != null)
        {
            bool blockInput = ParentModeManager.Instance != null &&
                              ParentModeManager.Instance.IsPatternPanelOpen;

            button.interactable = !blockInput && !isCollected;
        }
    }

    public void Init(GameManager_SelectItems manager)
    {
        gameManager = manager;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (isCollected) return;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsPatternPanelOpen)
            return;

        if (gameManager != null)
            gameManager.OnItemClicked(this);
    }

    public void MarkCollected()
    {
        isCollected = true;

        if (button != null)
            button.interactable = false;
    }

    public IEnumerator PlayCorrectAnimation()
    {
        Vector3 startScale = transform.localScale;
        Vector3 bigScale = startScale * 1.3f;

        float t = 0f;
        float duration = 0.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            transform.localScale = Vector3.Lerp(startScale, bigScale, lerp);
            yield return null;
        }

        if (starSprite != null)
            image.sprite = starSprite;

        t = 0f;
        duration = 0.3f;

        Color startColor = image.color;
        Color endColor = startColor;
        endColor.a = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;
            image.color = Color.Lerp(startColor, endColor, lerp);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public IEnumerator PlayWrongAnimation()
    {
        float duration = 0.2f;
        float strength = 10f;
        float t = 0f;
        Vector3 originalPos = transform.localPosition;

        while (t < duration)
        {
            t += Time.deltaTime;
            float offset = Mathf.Sin(t * 50f) * strength;
            transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}