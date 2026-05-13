using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OppositesCard : MonoBehaviour
{
    [Header("Unique ID")]
    public string cardId;

    [Header("Card Info")]
    public string conceptId;
    public string pairId;

    [Header("Images")]
    public Image cardImage;
    public Sprite frontSprite;
    public Sprite backSprite;

    [Header("Card Audio")]
    public AudioClip cardVoiceClip;

    public bool IsOpen { get; private set; } = false;
    public bool IsMatched { get; private set; } = false;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        ResetCard();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }
    }

    private void OnCardClicked()
    {
        if (IsMatched || IsOpen) return;
        if (OppositesMemoryLevelManager.Instance == null) return;

        OppositesMemoryLevelManager.Instance.OnCardSelected(this);
    }

    public void OpenCard()
    {
        IsOpen = true;

        if (cardImage != null && frontSprite != null)
            cardImage.sprite = frontSprite;
    }

    public void CloseCard()
    {
        if (IsMatched) return;

        IsOpen = false;

        if (cardImage != null && backSprite != null)
            cardImage.sprite = backSprite;
    }

    public void SetMatched()
    {
        IsMatched = true;
        IsOpen = true;

        if (cardImage != null && frontSprite != null)
            cardImage.sprite = frontSprite;

        if (button != null)
            button.interactable = false;
    }

    public void ResetCard()
    {
        IsMatched = false;
        IsOpen = false;

        if (cardImage != null && backSprite != null)
            cardImage.sprite = backSprite;

        if (button != null)
            button.interactable = true;
    }

    /// <summary>
    /// Runtime assignment for random opposite deals. Keeps <see cref="cardId"/> and back art;
    /// updates match identity and front face.
    /// </summary>
    public void AssignRuntimeOpposite(string newPairId, string newConceptId, Sprite newFront, AudioClip newVoice)
    {
        pairId = newPairId;
        conceptId = newConceptId;
        if (newFront != null)
            frontSprite = newFront;
        cardVoiceClip = newVoice;

        if (IsMatched)
        {
            if (cardImage != null && frontSprite != null)
                cardImage.sprite = frontSprite;
            return;
        }

        if (IsOpen)
            OpenCard();
        else
            CloseCard();
    }
}