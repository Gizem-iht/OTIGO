using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CorrectImageSelectImageManager : MonoBehaviour
{
    [Header("Sesler")]
    public AudioSource audioSource;
    public AudioClip selectHamburgerClip;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;

    [Header("UI")]
    public TextMeshProUGUI feedbackText;

    [Header("Buttons")]
    public Button mainMenuButton;
    public Button restartButton;
    public Button nextLevelButton;

    [Header("Settings")]
    public Slider volumeSlider;

    [Header("Scene Names")]
    public string mainMenuSceneName = "CorrectImage_Main";
    public string congratsSceneName = "CorrectImage_TebrikScene";

    [SerializeField] private string targetItemId = "hamburger";

    private readonly List<CorrectImageSelectableItem> allItems = new List<CorrectImageSelectableItem>();
    private bool levelFinished = false;

    private void Start()
    {
        allItems.AddRange(FindObjectsOfType<CorrectImageSelectableItem>(true));
        StartRound();

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(NextLevel);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = 1f;
        }
    }

    public void SetVolume(float value)
    {
        if (audioSource != null)
            audioSource.volume = value;
    }

    public void StartRound()
    {
        levelFinished = false;

        if (feedbackText != null)
            feedbackText.text = "";

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (item.image != null)
                item.image.color = new Color(1f, 1f, 1f, 1f);

            item.transform.localScale = Vector3.one;
            item.SetInteractable(true);
        }

        if (audioSource != null && selectHamburgerClip != null)
        {
            audioSource.Stop();
            audioSource.clip = selectHamburgerClip;
            audioSource.Play();
        }

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);
    }

    public void OnItemSelected(string clickedItemId)
    {
        if (levelFinished) return;

        CorrectImageSelectableItem clickedObj = allItems.Find(i => i != null && i.itemId == clickedItemId);

        if (clickedObj == null) return;

        if (clickedItemId == targetItemId)
        {
            levelFinished = true;

            if (feedbackText != null)
                feedbackText.text = "Tebrikler! 🎉";

            if (audioSource != null && correctSfx != null)
                audioSource.PlayOneShot(correctSfx);

            StartCoroutine(AnimateCorrect(clickedObj));
            StartCoroutine(FadeOthers(clickedObj));

            foreach (var item in allItems)
            {
                if (item != null)
                    item.SetInteractable(false);
            }

            if (restartButton != null)
                restartButton.gameObject.SetActive(true);

            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(true);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Yanlış! 😅";

            if (audioSource != null && wrongSfx != null)
                audioSource.PlayOneShot(wrongSfx);
        }
    }

    private IEnumerator AnimateCorrect(CorrectImageSelectableItem item)
    {
        Vector3 startScale = item.transform.localScale;
        Vector3 targetScaleValue = startScale * 1.3f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            item.transform.localScale = Vector3.Lerp(startScale, targetScaleValue, t);
            yield return null;
        }
    }

    private IEnumerator FadeOthers(CorrectImageSelectableItem correctItem)
    {
        foreach (var item in allItems)
        {
            if (item == null || item == correctItem || item.image == null) continue;

            float t = 0f;
            Color start = item.image.color;
            Color target = new Color(start.r, start.g, start.b, 0.3f);

            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                item.image.color = Color.Lerp(start, target, t);
                yield return null;
            }
        }
    }

    public void GoToMainMenu()
    {
        PlayerPrefs.SetString("CorrectImage_LastLevel", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartLevel()
    {
        StartRound();
    }

    public void NextLevel()
    {
        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
            ParentModeManager.Instance.CloseParentMode();

        SceneManager.LoadScene(congratsSceneName);
    }
}
