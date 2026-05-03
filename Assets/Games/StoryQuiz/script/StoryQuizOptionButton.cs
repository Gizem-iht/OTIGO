using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryQuizOptionButton : MonoBehaviour
{
    public Image optionImage;
    public TextMeshProUGUI optionText;

    private bool isCorrect;
    private StoryQuizBaseController controller;
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
    }

    public void Setup(Sprite sprite, string text, bool correct, StoryQuizBaseController baseController)
    {
        if (optionImage != null)
            optionImage.sprite = sprite;

        if (optionText != null)
            optionText.text = text;

        isCorrect = correct;
        controller = baseController;
    }

    public void OnClick()
    {
        if (controller != null && myButton != null)
            controller.OnOptionSelected(isCorrect, myButton);
    }
}