using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager_SelectItems : MonoBehaviour
{
    [Header("Oyun Kuralı")]
    public ItemCategory targetCategory = ItemCategory.Fruit;
    public string questionTextTemulate = "Sence hangileri {0}?";
    public string categoryDisplayName = "meyve";

    [Header("Geri Bildirim Metinleri")]
    public string correctFeedback = "Aferin, doğru seçim!";
    public string wrongFeedback = "Bu değil, tekrar dene!";
    public string allCorrectFeedback = "Harika! Heusini buldun 🎉";

    [Header("Sesler")]
    public AudioSource sfxSource;
    public AudioClip startNarrationClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip allDoneClip;

    [Header("UI Yazılar")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;

    [Header("Buttons")]
    public Button restartButton;
    public Button closeButton;
    public Button nextLevelButton;

    [Header("🎨 Background Color")]
    public ObjectSelectBackgroundColorChanger backgroundColorChanger;
    public Button colorChangeButton;

    [Header("Settings")]
    public Slider volumeSlider;

    [Header("Items")]
    public List<SelectableFoodItemButton> allItems = new List<SelectableFoodItemButton>();
    public Transform itemsParent;

    [Header("⭐ TOP STAR BAR")]
    public List<Image> topStars;
    public Sprite starSprite;
    [Range(0f, 1f)] public float emptyAlpha = 0.2f;
    [Range(0f, 1f)] public float filledAlpha = 1f;

    [Header("STAR SETTINGS")]
    public int maxLevels = 6;
    public bool resetOnFirstLaunchThisSession = true;

    [Header("SCENES")]
    public string mainMenuSceneName = "ObjectSelect_MainMenu";
    public string congratsSceneName = "ObjectSelect_TebrikScene";

    [Header("OTIGO")]
    public int activityId = 3;
    public int levelPlayed = 1;
    public bool isFinalLevel = false;

    [Header("Parent Help")]
    public int parentHelpCount = 0;

    private const string LAST_LEVEL_KEY = "ObjectSelect_LastLevelSceneName";

    private int remainingTargets = 0;
    private int totalTargetsThisLevel = 0;
    private int wrongSelectionCount = 0;

    private bool levelFinished = false;
    private bool earnedStarThisLevel = false;
    private bool resultSaved = false;
    private bool gameplayStarted = false;

    private float activePlayTime = 0f;

    private string starsKey;
    private static bool sessionInitialized = false;

    private string StatePrefix
    {
        get { return "ObjectSelect_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private void Awake()
    {
        starsKey = "StarsCollected_SelectItems_" + categoryDisplayName;

        if (resetOnFirstLaunchThisSession && !sessionInitialized && !HasSavedState())
        {
            PlayerPrefs.SetInt(starsKey, 0);
            PlayerPrefs.Save();
            sessionInitialized = true;
        }
    }

    private void Start()
    {
        SafeSetActive(restartButton, false);
        SafeSetActive(nextLevelButton, false);

        if (!ObjectSelectOtigoSessionTracker.HasSession())
            ObjectSelectOtigoSessionTracker.BeginSession(activityId);

        resultSaved = false;
        wrongSelectionCount = 0;
        parentHelpCount = 0;
        activePlayTime = 0f;
        gameplayStarted = false;
        levelFinished = false;
        earnedStarThisLevel = false;

        SetupButtons();
        SetupVolume();
        SetupColorButton();
        FindItemsIfNeeded();
        SetupItems();

        if (questionText != null)
            questionText.text = string.Format(questionTextTemulate, categoryDisplayName);

        if (feedbackText != null)
            feedbackText.text = "";

        InitStarBar();
        UpdateStarBar();

        if (HasSavedState())
        {
            LoadState();
            RestoreCollectedItems();

            if (levelFinished)
                RestoreCompletedLevel();
            else
                gameplayStarted = true;
        }
        else
        {
            StartCoroutine(StartNarrationRoutine());
        }
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (gameplayStarted && !levelFinished && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;
    }

    private bool IsInstructionAudioPlaying()
    {
        return sfxSource != null && sfxSource.isPlaying;
    }

    private IEnumerator StartNarrationRoutine()
    {
        gameplayStarted = false;

        if (sfxSource != null && startNarrationClip != null)
        {
            sfxSource.PlayOneShot(startNarrationClip);
            yield return new WaitForSeconds(startNarrationClip.length);
        }

        gameplayStarted = true;
        SaveState();
    }

    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseGameOrGoToMenu);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(NextLevel);
        }
    }

    private void SetupVolume()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(v => AudioListener.volume = v);
        }
    }

    private void SetupColorButton()
    {
        if (colorChangeButton != null && backgroundColorChanger != null)
        {
            colorChangeButton.onClick.RemoveAllListeners();
            colorChangeButton.onClick.AddListener(backgroundColorChanger.ChangeColor);
        }
    }

    private void FindItemsIfNeeded()
    {
        if (allItems == null)
            allItems = new List<SelectableFoodItemButton>();

        if (allItems.Count == 0)
        {
            if (itemsParent != null)
                allItems.AddRange(itemsParent.GetComponentsInChildren<SelectableFoodItemButton>(true));
            else
                allItems.AddRange(FindObjectsOfType<SelectableFoodItemButton>(true));
        }
    }

    private void SetupItems()
    {
        remainingTargets = 0;
        totalTargetsThisLevel = 0;

        foreach (var item in allItems)
        {
            if (item == null) continue;

            item.Init(this);

            if (item.category == targetCategory)
            {
                remainingTargets++;
                totalTargetsThisLevel++;
            }
        }
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

    public void OnItemClicked(SelectableFoodItemButton item)
    {
        if (levelFinished || item == null) return;

        int itemIndex = allItems.IndexOf(item);

        if (item.category == targetCategory)
        {
            if (ParentModeManager.Instance != null &&
                ParentModeManager.Instance.IsParentModeActive)
            {
                AddParentHelp();
            }

            sfxSource?.PlayOneShot(correctClip);

            item.MarkCollected();
            StartCoroutine(item.PlayCorrectAnimation());
            StartCoroutine(ShowFeedbackMessage(correctFeedback));

            remainingTargets--;

            if (itemIndex >= 0)
                SaveCollectedItem(itemIndex);

            SaveState();

            if (remainingTargets <= 0)
            {
                earnedStarThisLevel = true;
                OnAllTargetsCollected();
            }
        }
        else
        {
            wrongSelectionCount++;

            sfxSource?.PlayOneShot(wrongClip);
            StartCoroutine(item.PlayWrongAnimation());
            StartCoroutine(ShowFeedbackMessage(wrongFeedback));

            SaveState();
        }
    }

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        ObjectSelectOtigoSessionTracker.AddParentHelpForLevel(levelPlayed);

        SaveState();
    }

    private void SaveLevelResult()
    {
        if (resultSaved) return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        ObjectSelectOtigoSessionTracker.AddOrUpdateLevelResult(
            levelNumber: levelPlayed,
            durationSeconds: durationSeconds,
            mistakesMade: wrongSelectionCount,
            targetCount: totalTargetsThisLevel
        );

        SaveState();

        if (isFinalLevel)
            ObjectSelectOtigoSessionTracker.SendFinalResultIfPossible();
    }

    private void OnAllTargetsCollected()
    {
        levelFinished = true;
        gameplayStarted = false;

        SaveLevelResult();

        if (feedbackText != null)
            feedbackText.text = allCorrectFeedback;

        sfxSource?.PlayOneShot(allDoneClip);

        SafeSetActive(restartButton, true);
        SafeSetActive(nextLevelButton, true);

        SaveState();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (feedbackText != null)
            feedbackText.text = allCorrectFeedback;

        SafeSetActive(restartButton, true);
        SafeSetActive(nextLevelButton, true);
    }

    private IEnumerator ShowFeedbackMessage(string msg)
    {
        if (feedbackText == null) yield break;

        feedbackText.text = msg;
        yield return new WaitForSeconds(1.2f);

        if (!levelFinished)
            feedbackText.text = "";
    }

    public void RestartLevel()
    {
        ClearState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CloseGameOrGoToMenu()
    {
        SaveState();

        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LAST_LEVEL_KEY, currentScene);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void NextLevel()
    {
        if (earnedStarThisLevel)
        {
            int stars = PlayerPrefs.GetInt(starsKey, 0);
            stars = Mathf.Clamp(stars + 1, 0, maxLevels);

            PlayerPrefs.SetInt(starsKey, stars);
            PlayerPrefs.Save();

            UpdateStarBar();
            earnedStarThisLevel = false;
        }

        ClearState();

        if (isFinalLevel)
        {
            OtigoGameProgress.ClearKey(LAST_LEVEL_KEY);
            SceneManager.LoadScene(congratsSceneName);
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            OtigoGameProgress.ClearKey(LAST_LEVEL_KEY);
            SceneManager.LoadScene(congratsSceneName);
        }
        else
        {
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);

            OtigoGameProgress.SaveNextOrClearForFinal(LAST_LEVEL_KEY, nextSceneName, congratsSceneName);

            SceneManager.LoadScene(nextIndex);
        }
    }

    private void SafeSetActive(Button btn, bool active)
    {
        if (btn != null && btn.gameObject != null)
            btn.gameObject.SetActive(active);
    }

    private void SaveCollectedItem(int index)
    {
        PlayerPrefs.SetInt(StatePrefix + "Collected_" + index, 1);
        PlayerPrefs.Save();
    }

    private void RestoreCollectedItems()
    {
        remainingTargets = totalTargetsThisLevel;

        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] == null) continue;

            bool collected =
                PlayerPrefs.GetInt(StatePrefix + "Collected_" + i, 0) == 1;

            if (collected)
            {
                allItems[i].MarkCollected();

                if (allItems[i].category == targetCategory)
                    remainingTargets--;
            }
        }

        remainingTargets = Mathf.Max(0, remainingTargets);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "LevelFinished", levelFinished ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "EarnedStar", earnedStarThisLevel ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "WrongSelectionCount", wrongSelectionCount);
        PlayerPrefs.SetInt(StatePrefix + "ParentHelpCount", parentHelpCount);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        levelFinished = PlayerPrefs.GetInt(StatePrefix + "LevelFinished", 0) == 1;
        earnedStarThisLevel = PlayerPrefs.GetInt(StatePrefix + "EarnedStar", 0) == 1;
        resultSaved = PlayerPrefs.GetInt(StatePrefix + "ResultSaved", 0) == 1;
        wrongSelectionCount = PlayerPrefs.GetInt(StatePrefix + "WrongSelectionCount", 0);
        parentHelpCount = PlayerPrefs.GetInt(StatePrefix + "ParentHelpCount", 0);
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelFinished");
        PlayerPrefs.DeleteKey(StatePrefix + "EarnedStar");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "WrongSelectionCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ParentHelpCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");

        for (int i = 0; i < allItems.Count; i++)
            PlayerPrefs.DeleteKey(StatePrefix + "Collected_" + i);

        PlayerPrefs.Save();
    }
}
