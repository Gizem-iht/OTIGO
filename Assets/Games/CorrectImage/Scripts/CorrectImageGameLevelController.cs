using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CorrectImageGameLevelController : MonoBehaviour
{
    [Header("DATABASE")]
    public CorrectImageGameDatabase database;

    [Header("AUDIO")]
    public AudioSource audioSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip firstLevelWinSfx;

    [Header("VOICE & HIGHLIGHT SEQUENCE")]
    public AudioClip successVoice;
    public float highlightDuration = 1.4f;
    [Range(0f, 1f)] public float highlightAlpha = 0.25f;

    [Header("ONE TIME FLASH")]
    public bool flashOnlyOnce = true;
    public bool flashPerCategory = true;

    [Header("UI BUTTONS")]
    public Button backButton;
    public Button restartButton;
    public Button nextButton;
    public Slider volumeSlider;

    [Header("OPTION BUTTONS")]
    public List<Button> optionButtons = new List<Button>();

    [Header("VISUAL")]
    public Sprite starSprite;

    [Header("SCENES")]
    public string categorySceneName = "CorrectImage_CategorySelect";
    public string congratsSceneName = "CorrectImage_TebrikScene";

    [Header("LEVEL SETTINGS")]
    public int maxLevels = 6;

    [Header("SHAKE")]
    public float shakeDuration = 0.2f;
    public float shakeAmount = 15f;

    [Header("TOP STAR BAR")]
    public List<Image> topStars = new List<Image>();
    [Range(0f, 1f)] public float emptyAlpha = 0.2f;
    [Range(0f, 1f)] public float filledAlpha = 1f;

    [Header("STAR RESET")]
    public bool resetStarsOnCategoryEnter = false;
    public bool resetStarsWhenLevelZero = true;

    [Header("OTIGO API")]
    [SerializeField] private int activityId = 4;
    [SerializeField] private int parentHelpCount = 0;

    private CorrectImageCategoryData currentCategory;
    private ItemData targetItem;
    private List<ItemData> options = new List<ItemData>();

    private int levelIndex;
    private bool levelFinished = false;

    private string starsKey = "CorrectImage_StarsCollected";
    private bool earnedStarThisLevel = false;
    private Coroutine successSequenceCo;
    private string flashShownKey = "CorrectImage_FlashShown_GLOBAL";

    private const string SELECTED_CATEGORY_KEY = "CorrectImage_SelectedCategory";
    private const string CURRENT_LEVEL_KEY = "CorrectImage_CurrentLevel";

    private int totalMistakesMade = 0;
    private int completedLevelCount = 0;
    private bool resultSent = false;

    private int currentLevelMistakes = 0;
    private int currentLevelHelpCount = 0;
    private float currentLevelActiveTime = 0f;

    private int savedCorrectButtonIndex = -1;

    private List<OtigoActivityResultSender.LevelResult> levelResults =
        new List<OtigoActivityResultSender.LevelResult>();

    private string StatePrefix
    {
        get
        {
            string categoryId = PlayerPrefs.GetString(SELECTED_CATEGORY_KEY, "none");
            return "CorrectImage_State_" + categoryId + "_Level_" + levelIndex + "_";
        }
    }

    private void Start()
    {
        totalMistakesMade = PlayerPrefs.GetInt("CorrectImage_TotalMistakes", 0);
        completedLevelCount = PlayerPrefs.GetInt("CorrectImage_CompletedLevelCount", 0);
        parentHelpCount = PlayerPrefs.GetInt("CorrectImage_ParentHelpCount", 0);

        resultSent = false;
        levelResults.Clear();

        SetupButtons();

        if (database == null)
        {
            Debug.LogError("CorrectImageGameDatabase bağlı değil!");
            return;
        }

        string categoryId = PlayerPrefs.GetString(SELECTED_CATEGORY_KEY, "");
        levelIndex = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 0);

        currentCategory = database.categories.FirstOrDefault(c => c != null && c.categoryId == categoryId);

        if (currentCategory == null)
        {
            Debug.LogError("Kategori bulunamadı: " + categoryId);
            return;
        }

        if (currentCategory.items == null || currentCategory.items.Count < 2)
        {
            Debug.LogError("Kategori için en az 2 item gerekli!");
            return;
        }

        starsKey = "CorrectImage_StarsCollected_" + categoryId;

        flashShownKey = flashPerCategory
            ? "CorrectImage_FlashShown_" + categoryId
            : "CorrectImage_FlashShown_GLOBAL";

        if (resetStarsOnCategoryEnter)
        {
            PlayerPrefs.SetInt(starsKey, 0);
            PlayerPrefs.Save();
        }

        if (resetStarsWhenLevelZero && levelIndex == 0 && !HasSavedState())
        {
            PlayerPrefs.SetInt(starsKey, 0);
            PlayerPrefs.Save();
        }

        InitStarBar();
        UpdateStarBar();
        BuildLevel();
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (!levelFinished && !parentModeActive && !resultSent && !IsInstructionAudioPlaying())
        {
            currentLevelActiveTime += Time.deltaTime;
        }
    }

    private bool IsInstructionAudioPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButton);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
            restartButton.gameObject.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextLevel);
            nextButton.gameObject.SetActive(false);
        }

        if (volumeSlider != null && audioSource != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = audioSource.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    private void SetVolume(float value)
    {
        if (audioSource != null)
            audioSource.volume = value;
    }

    private void BuildLevel()
    {
        levelFinished = false;
        earnedStarThisLevel = false;
        savedCorrectButtonIndex = -1;

        if (successSequenceCo != null)
        {
            StopCoroutine(successSequenceCo);
            successSequenceCo = null;
        }

        if (restartButton != null) restartButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        if (HasSavedState())
            LoadSavedLevelState();
        else
            CreateNewLevelState();

        ApplyOptionsToButtons();

        if (levelFinished)
            RestoreCompletedVisual();
        else
            PlayTargetVoice();
    }

    private void CreateNewLevelState()
    {
        currentLevelActiveTime = 0f;
        currentLevelMistakes = 0;
        currentLevelHelpCount = 0;

        int optionCount = 2;

        if (database.optionsPerLevel != null && database.optionsPerLevel.Count > 0)
        {
            int safeIndex = Mathf.Clamp(levelIndex, 0, database.optionsPerLevel.Count - 1);
            optionCount = database.optionsPerLevel[safeIndex];
        }

        optionCount = Mathf.Clamp(optionCount, 2, optionButtons.Count);

        targetItem = currentCategory.items[Random.Range(0, currentCategory.items.Count)];

        options = currentCategory.items
            .Where(i => i != null && i != targetItem)
            .OrderBy(x => Random.value)
            .Take(optionCount - 1)
            .ToList();

        options.Add(targetItem);
        options = options.OrderBy(x => Random.value).ToList();

        SaveLevelState();
    }

    private void ApplyOptionsToButtons()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (optionButtons[i] == null) continue;

            if (i < options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = !levelFinished;

                Image img = optionButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = options[i].sprite;
                    img.color = Color.white;
                    img.preserveAspect = true;
                }

                int idx = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void RestoreCompletedVisual()
    {
        foreach (Button b in optionButtons)
        {
            if (b != null)
                b.interactable = false;
        }

        if (savedCorrectButtonIndex >= 0 && savedCorrectButtonIndex < optionButtons.Count)
        {
            Image img = optionButtons[savedCorrectButtonIndex].GetComponent<Image>();
            if (img != null && starSprite != null)
                img.sprite = starSprite;
        }

        earnedStarThisLevel = true;

        if (restartButton != null)
            restartButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    private void PlayTargetVoice()
    {
        if (audioSource != null && targetItem != null && targetItem.voice != null)
        {
            audioSource.Stop();
            audioSource.clip = targetItem.voice;
            audioSource.Play();
        }
    }

    private void OnOptionClicked(int index)
    {
        if (levelFinished) return;
        if (index < 0 || index >= options.Count) return;

        if (options[index] == targetItem)
        {
            levelFinished = true;
            earnedStarThisLevel = true;
            savedCorrectButtonIndex = index;

            if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
                AddParentHelp();

            Image img = optionButtons[index].GetComponent<Image>();
            if (img != null && starSprite != null)
                img.sprite = starSprite;

            foreach (Button b in optionButtons)
            {
                if (b != null)
                    b.interactable = false;
            }

            if (audioSource != null && correctSfx != null)
                audioSource.PlayOneShot(correctSfx);

            if (audioSource != null && levelIndex == 0 && firstLevelWinSfx != null)
                audioSource.PlayOneShot(firstLevelWinSfx);

            if (restartButton != null) restartButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            SaveLevelState();

            if (!flashOnlyOnce || PlayerPrefs.GetInt(flashShownKey, 0) == 0)
            {
                if (flashOnlyOnce)
                {
                    PlayerPrefs.SetInt(flashShownKey, 1);
                    PlayerPrefs.Save();
                }

                successSequenceCo = StartCoroutine(SuccessSequence());
            }
        }
        else
        {
            totalMistakesMade++;
            currentLevelMistakes++;

            PlayerPrefs.SetInt("CorrectImage_TotalMistakes", totalMistakesMade);
            SaveLevelState();

            if (audioSource != null && wrongSfx != null)
                audioSource.PlayOneShot(wrongSfx);

            RectTransform rt = optionButtons[index].GetComponent<RectTransform>();
            StartCoroutine(Shake(rt));
        }
    }

    private IEnumerator SuccessSequence()
    {
        if (audioSource != null && successVoice != null)
            audioSource.PlayOneShot(successVoice);

        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(FlashButtonOnce(nextButton));
        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(FlashButtonOnce(restartButton));
    }

    private IEnumerator FlashButtonOnce(Button btn)
    {
        if (btn == null) yield break;

        Image img = btn.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;

        img.color = new Color(original.r, original.g, original.b, highlightAlpha);
        yield return new WaitForSeconds(highlightDuration * 0.5f);

        img.color = original;
        yield return new WaitForSeconds(highlightDuration * 0.5f);
    }

    private IEnumerator Shake(RectTransform rect)
    {
        if (rect == null) yield break;

        Vector2 startPos = rect.anchoredPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            rect.anchoredPosition = startPos + Random.insideUnitCircle * shakeAmount;
            yield return null;
        }

        rect.anchoredPosition = startPos;
    }

    public void OnBackButton()
    {
        SaveLevelState();
        SceneManager.LoadScene(categorySceneName);
    }

    public void RestartLevel()
    {
        ClearLevelState();

        currentLevelActiveTime = 0f;
        currentLevelMistakes = 0;
        currentLevelHelpCount = 0;
        levelFinished = false;
        earnedStarThisLevel = false;
        savedCorrectButtonIndex = -1;

        BuildLevel();
    }

    public void NextLevel()
    {
        if (!levelFinished) return;

        int playedLevelNumber = levelIndex + 1;

        if (earnedStarThisLevel)
        {
            int stars = PlayerPrefs.GetInt(starsKey, 0);
            stars = Mathf.Clamp(stars + 1, 0, maxLevels);
            PlayerPrefs.SetInt(starsKey, stars);
            PlayerPrefs.Save();
            UpdateStarBar();

            completedLevelCount++;
            PlayerPrefs.SetInt("CorrectImage_CompletedLevelCount", completedLevelCount);
        }

        int levelDurationSeconds = Mathf.RoundToInt(currentLevelActiveTime);

        levelResults.Add(new OtigoActivityResultSender.LevelResult
        {
            levelNumber = playedLevelNumber,
            durationSeconds = levelDurationSeconds,
            mistakesMade = currentLevelMistakes,
            helpCount = currentLevelHelpCount
        });

        ClearLevelState();

        levelIndex++;

        if (levelIndex >= maxLevels)
        {
            SendOtigoResult();

            ClearAllProgress();

            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, 0);
            PlayerPrefs.Save();

            SceneManager.LoadScene(congratsSceneName);
        }
        else
        {
            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, levelIndex);
            PlayerPrefs.Save();

            BuildLevel();
        }
    }

    private void SendOtigoResult()
    {
        if (resultSent) return;
        resultSent = true;

        if (OtigoActivityResultSender.Instance == null)
        {
            Debug.LogError("OtigoActivityResultSender.Instance bulunamadı!");
            return;
        }

        int durationSeconds = levelResults.Sum(l => l.durationSeconds);
        int totalTargetCount = completedLevelCount;
        int levelPlayed = levelResults.Count;

        OtigoActivityResultSender.Instance.SendActivityResult(
            activityId,
            durationSeconds,
            totalMistakesMade,
            parentHelpCount,
            totalTargetCount,
            levelPlayed,
            levelResults
        );
    }

    public void AddParentHelp()
    {
        parentHelpCount++;
        currentLevelHelpCount++;

        PlayerPrefs.SetInt("CorrectImage_ParentHelpCount", parentHelpCount);

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        SaveLevelState();
    }

    private void SaveLevelState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "LevelFinished", levelFinished ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "EarnedStar", earnedStarThisLevel ? 1 : 0);
        PlayerPrefs.SetFloat(StatePrefix + "ActiveTime", currentLevelActiveTime);
        PlayerPrefs.SetInt(StatePrefix + "Mistakes", currentLevelMistakes);
        PlayerPrefs.SetInt(StatePrefix + "Help", currentLevelHelpCount);
        PlayerPrefs.SetInt(StatePrefix + "CorrectButtonIndex", savedCorrectButtonIndex);

        int targetIndex = currentCategory.items.IndexOf(targetItem);
        PlayerPrefs.SetInt(StatePrefix + "TargetIndex", targetIndex);

        PlayerPrefs.SetInt(StatePrefix + "OptionCount", options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            int optionItemIndex = currentCategory.items.IndexOf(options[i]);
            PlayerPrefs.SetInt(StatePrefix + "Option_" + i, optionItemIndex);
        }

        PlayerPrefs.Save();
    }

    private void LoadSavedLevelState()
    {
        levelFinished = PlayerPrefs.GetInt(StatePrefix + "LevelFinished", 0) == 1;
        earnedStarThisLevel = PlayerPrefs.GetInt(StatePrefix + "EarnedStar", 0) == 1;
        currentLevelActiveTime = PlayerPrefs.GetFloat(StatePrefix + "ActiveTime", 0f);
        currentLevelMistakes = PlayerPrefs.GetInt(StatePrefix + "Mistakes", 0);
        currentLevelHelpCount = PlayerPrefs.GetInt(StatePrefix + "Help", 0);
        savedCorrectButtonIndex = PlayerPrefs.GetInt(StatePrefix + "CorrectButtonIndex", -1);

        int targetIndex = PlayerPrefs.GetInt(StatePrefix + "TargetIndex", 0);
        targetIndex = Mathf.Clamp(targetIndex, 0, currentCategory.items.Count - 1);
        targetItem = currentCategory.items[targetIndex];

        options.Clear();

        int optionCount = PlayerPrefs.GetInt(StatePrefix + "OptionCount", 0);

        for (int i = 0; i < optionCount; i++)
        {
            int itemIndex = PlayerPrefs.GetInt(StatePrefix + "Option_" + i, -1);

            if (itemIndex >= 0 && itemIndex < currentCategory.items.Count)
                options.Add(currentCategory.items[itemIndex]);
        }

        if (options.Count == 0)
            CreateNewLevelState();
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearLevelState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelFinished");
        PlayerPrefs.DeleteKey(StatePrefix + "EarnedStar");
        PlayerPrefs.DeleteKey(StatePrefix + "ActiveTime");
        PlayerPrefs.DeleteKey(StatePrefix + "Mistakes");
        PlayerPrefs.DeleteKey(StatePrefix + "Help");
        PlayerPrefs.DeleteKey(StatePrefix + "CorrectButtonIndex");
        PlayerPrefs.DeleteKey(StatePrefix + "TargetIndex");
        PlayerPrefs.DeleteKey(StatePrefix + "OptionCount");

        for (int i = 0; i < optionButtons.Count; i++)
            PlayerPrefs.DeleteKey(StatePrefix + "Option_" + i);

        PlayerPrefs.Save();
    }

    private void ClearAllProgress()
    {
        PlayerPrefs.DeleteKey("CorrectImage_TotalMistakes");
        PlayerPrefs.DeleteKey("CorrectImage_CompletedLevelCount");
        PlayerPrefs.DeleteKey("CorrectImage_ParentHelpCount");
        PlayerPrefs.Save();
    }

    private void InitStarBar()
    {
        if (topStars == null || topStars.Count == 0) return;

        for (int i = 0; i < topStars.Count; i++)
        {
            if (topStars[i] == null) continue;

            if (starSprite != null)
                topStars[i].sprite = starSprite;

            topStars[i].preserveAspect = true;
        }
    }

    private void UpdateStarBar()
    {
        if (topStars == null || topStars.Count == 0) return;

        int stars = PlayerPrefs.GetInt(starsKey, 0);
        stars = Mathf.Clamp(stars, 0, maxLevels);

        for (int i = 0; i < topStars.Count; i++)
        {
            if (topStars[i] == null) continue;

            bool filled = i < stars;
            Color c = topStars[i].color;
            c.a = filled ? filledAlpha : emptyAlpha;
            topStars[i].color = c;
            topStars[i].gameObject.SetActive(i < maxLevels);
        }
    }
}
