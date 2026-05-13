using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public abstract class StoryQuizBaseController : MonoBehaviour
{
    [Header("Level Settings")]
    public int activityId = 11;
    public int levelNumber = 1;
    public string nextSceneName = "";
    public bool isLastLevel = false;
    public string mainMenuSceneName = "StoryQuiz_MainMenu";
    public string finalSceneName = "StoryQuiz_Tebrik";
    public string lastLevelKey = "StoryQuiz_LastLevel";

    [Header("Story Data")]
    [TextArea(2, 5)] public string storyText;
    public Sprite storySprite;
    public AudioClip storyVoiceClip;

    [Header("Question Data")]
    [TextArea(2, 4)] public string questionTextValue;
    public AudioClip questionVoiceClip;

    [Header("Option 1")]
    public Sprite option1Sprite;
    public string option1Text;
    public bool option1IsCorrect;

    [Header("Option 2")]
    public Sprite option2Sprite;
    public string option2Text;
    public bool option2IsCorrect;

    [Header("Option 3")]
    public Sprite option3Sprite;
    public string option3Text;
    public bool option3IsCorrect;

    [Header("Option 4")]
    public Sprite option4Sprite;
    public string option4Text;
    public bool option4IsCorrect;

    [Header("Option 5")]
    public Sprite option5Sprite;
    public string option5Text;
    public bool option5IsCorrect;

    [Header("UI References")]
    public GameObject storyPanel;
    public GameObject questionPanel;
    public Image storyImage;
    public TextMeshProUGUI storyTextUI;
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI feedbackText;

    [Header("Option Buttons")]
    public StoryQuizOptionButton optionButton1;
    public StoryQuizOptionButton optionButton2;
    public StoryQuizOptionButton optionButton3;
    public StoryQuizOptionButton optionButton4;
    public StoryQuizOptionButton optionButton5;

    [Header("Control Buttons")]
    public Button muteButton;
    public Button replayButton;
    public Button nextButton;
    public Button goMenuButton;
    public Button replayVoiceButton;
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource sfxSource;
    public AudioClip introClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeClip;

    [Header("Feedback Texts")]
    public string correctFeedback = "Aferin, doğru cevap!";
    public string wrongFeedback = "Tekrar deneyelim.";

    [Header("Parent Mode")]
    public ParentModeManager parentModeManager;

    [Header("Button Animations")]
    public float wrongShakeDuration = 0.25f;
    public float wrongShakeMagnitude = 12f;
    public float correctPopDuration = 0.20f;
    public float correctScaleMultiplier = 1.15f;

    private float activePlayTime = 0f;
    private int mistakesMade = 0;
    private bool answeredCorrectly = false;
    private bool questionPhaseStarted = false;
    private bool isMuted = false;
    private bool parentHelpUsedThisLevel = false;
    private bool isAnimatingOption = false;
    private float lastNonZeroVolume = 1f;
    private Coroutine parentHelpStoryCoroutine;
    private bool wasParentModeActive = false;
    private bool wasPatternPanelOpen = false;

    protected virtual void Start()
    {
        ResolveParentModeManager();
        SaveCurrentLevelAsProgress();

        if (!StoryQuizOtigoSessionTracker.HasSession())
            StoryQuizOtigoSessionTracker.BeginSession(activityId);

        SetupUI();
        SetupButtons();

        StartCoroutine(IntroThenStoryFlow());
    }

    private void SaveCurrentLevelAsProgress()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(lastLevelKey, currentScene);
        PlayerPrefs.Save();
        Debug.Log("StoryQuiz kayıtlı level: " + currentScene);
    }

    private void SetupUI()
    {
        DisableQuestionPanelBackgroundRaycast();

        if (storyPanel != null)
            storyPanel.SetActive(true);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (replayButton != null)
            replayButton.gameObject.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = "";

        if (storyImage != null)
            storyImage.sprite = storySprite;

        if (storyTextUI != null)
            storyTextUI.text = storyText;

        if (questionTextUI != null)
            questionTextUI.text = questionTextValue;

        if (optionButton1 != null)
            optionButton1.Setup(option1Sprite, option1Text, option1IsCorrect, this);

        if (optionButton2 != null)
            optionButton2.Setup(option2Sprite, option2Text, option2IsCorrect, this);

        if (optionButton3 != null)
            optionButton3.Setup(option3Sprite, option3Text, option3IsCorrect, this);

        if (optionButton4 != null)
            optionButton4.Setup(option4Sprite, option4Text, option4IsCorrect, this);

        if (optionButton5 != null)
            optionButton5.Setup(option5Sprite, option5Text, option5IsCorrect, this);

        SetReplayVoiceButtonVisible(true);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = AudioListener.volume;
        }
    }

    private void SetupButtons()
    {
        if (muteButton != null)
        {
            muteButton.onClick.RemoveAllListeners();
            muteButton.onClick.AddListener(ToggleMute);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayLevel);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(GoNextLevel);
        }

        if (goMenuButton != null)
        {
            goMenuButton.onClick.RemoveAllListeners();
            goMenuButton.onClick.AddListener(GoMenu);
        }

        if (replayVoiceButton != null)
        {
            replayVoiceButton.onClick.RemoveAllListeners();
            replayVoiceButton.onClick.AddListener(ReplayCurrentVoice);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }

        if (parentModeManager != null)
            wasParentModeActive = parentModeManager.IsParentModeActive;
    }

    private void ResolveParentModeManager()
    {
        if (parentModeManager != null)
            return;

        if (ParentModeManager.Instance != null)
            parentModeManager = ParentModeManager.Instance;
        else
            parentModeManager = FindObjectOfType<ParentModeManager>();
    }

    private void DisableQuestionPanelBackgroundRaycast()
    {
        if (questionPanel == null)
            return;

        Image questionPanelImage = questionPanel.GetComponent<Image>();

        if (questionPanelImage != null)
            questionPanelImage.raycastTarget = false;
    }

    private void SetReplayVoiceButtonVisible(bool visible)
    {
        if (replayVoiceButton == null)
            return;

        Transform replayTransform = replayVoiceButton.transform;
        Transform patternTransform = parentModeManager != null &&
                                     parentModeManager.patternPanel != null
            ? parentModeManager.patternPanel.transform
            : null;

        bool patternIsChild = patternTransform != null &&
                              patternTransform.IsChildOf(replayTransform);

        if (!patternIsChild)
        {
            replayVoiceButton.gameObject.SetActive(visible);
            return;
        }

        replayVoiceButton.gameObject.SetActive(true);
        replayVoiceButton.interactable = visible;

        Graphic[] graphics = replayVoiceButton.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic.transform.IsChildOf(patternTransform))
                continue;

            graphic.enabled = visible;
        }
    }

    private void Update()
    {
        if (parentModeManager == null)
        {
            if (!answeredCorrectly && !IsInstructionAudioPlaying())
                activePlayTime += Time.deltaTime;

            return;
        }

        bool parentModeActive = parentModeManager.IsParentModeActive;
        bool patternPanelOpen = parentModeManager.IsPatternPanelOpen;

        if (!answeredCorrectly && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;

        if (patternPanelOpen && !wasPatternPanelOpen)
            BringParentModeUIToFront();

        if (parentModeActive &&
            !wasParentModeActive &&
            questionPhaseStarted &&
            !answeredCorrectly)
        {
            ReplayStoryForParentHelp();
        }

        wasParentModeActive = parentModeActive;
        wasPatternPanelOpen = patternPanelOpen;
    }

    private bool IsInstructionAudioPlaying()
    {
        return voiceSource != null && voiceSource.isPlaying;
    }

    private IEnumerator IntroThenStoryFlow()
    {
        if (introClip != null && voiceSource != null)
        {
            questionPhaseStarted = false;

            if (storyPanel != null)
                storyPanel.SetActive(true);

            if (questionPanel != null)
                questionPanel.SetActive(false);

            SetReplayVoiceButtonVisible(false);

            voiceSource.Stop();
            voiceSource.clip = introClip;
            voiceSource.Play();

            yield return new WaitForSeconds(introClip.length + 0.2f);
        }

        yield return StartCoroutine(StoryFlow());
    }

    private IEnumerator StoryFlow()
    {
        questionPhaseStarted = false;

        if (storyPanel != null)
            storyPanel.SetActive(true);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        SetReplayVoiceButtonVisible(true);

        PlayStoryVoice();

        float waitTime = 2f;
        if (storyVoiceClip != null)
            waitTime = storyVoiceClip.length + 0.4f;

        yield return new WaitForSeconds(waitTime);

        ShowQuestionPhase();
    }

    protected void ShowQuestionPhase()
    {
        questionPhaseStarted = true;

        if (storyPanel != null)
            storyPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(true);

        SetReplayVoiceButtonVisible(false);

        BringParentModeUIToFront();
        PlayQuestionVoice();
    }

    public void OnOptionSelected(bool isCorrect, Button clickedButton)
    {
        if (!questionPhaseStarted)
            return;

        if (answeredCorrectly)
            return;

        if (isAnimatingOption)
            return;

        if (isCorrect)
            StartCoroutine(HandleCorrectAnswer(clickedButton));
        else
            StartCoroutine(HandleWrongAnswer(clickedButton));
    }

    private IEnumerator HandleCorrectAnswer(Button clickedButton)
    {
        isAnimatingOption = true;
        answeredCorrectly = true;

        if (feedbackText != null)
            feedbackText.text = correctFeedback;

        if (sfxSource != null && correctClip != null)
            sfxSource.PlayOneShot(correctClip);

        if (clickedButton != null)
            yield return StartCoroutine(PopButton(clickedButton.transform));

        CountParentHelpIfNeeded();
        SaveLevelResult();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.transform.SetAsLastSibling();
        }

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(true);
            replayButton.transform.SetAsLastSibling();
        }

        if (parentModeManager != null)
        {
            if (parentModeManager.lockButton != null)
                parentModeManager.lockButton.transform.SetAsLastSibling();

            if (parentModeManager.closeParentModeButton != null)
                parentModeManager.closeParentModeButton.transform.SetAsLastSibling();

            if (parentModeManager.patternPanel != null)
                parentModeManager.patternPanel.transform.SetAsLastSibling();
        }

        if (sfxSource != null && completeClip != null)
            sfxSource.PlayOneShot(completeClip);

        isAnimatingOption = false;
    }

    private IEnumerator HandleWrongAnswer(Button clickedButton)
    {
        isAnimatingOption = true;
        mistakesMade++;

        if (feedbackText != null)
            feedbackText.text = wrongFeedback;

        if (sfxSource != null && wrongClip != null)
            sfxSource.PlayOneShot(wrongClip);

        if (clickedButton != null)
            yield return StartCoroutine(ShakeButton(clickedButton.transform));

        isAnimatingOption = false;
    }

    private void ReplayStoryForParentHelp()
    {
        if (parentHelpStoryCoroutine != null)
            StopCoroutine(parentHelpStoryCoroutine);

        parentHelpStoryCoroutine = StartCoroutine(ReplayStoryForParentHelpRoutine());
    }

    private IEnumerator ReplayStoryForParentHelpRoutine()
    {
        yield return StartCoroutine(ReplayStorySequence());
        parentHelpStoryCoroutine = null;
    }

    private void BringParentModeUIToFront()
    {
        if (parentModeManager == null)
            return;

        if (parentModeManager.lockButton != null)
            parentModeManager.lockButton.transform.SetAsLastSibling();

        if (parentModeManager.closeParentModeButton != null)
            parentModeManager.closeParentModeButton.transform.SetAsLastSibling();

        if (parentModeManager.patternPanel != null)
            parentModeManager.patternPanel.transform.SetAsLastSibling();
    }

    private IEnumerator ShakeButton(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < wrongShakeDuration)
        {
            float x = Random.Range(-wrongShakeMagnitude, wrongShakeMagnitude);
            float y = Random.Range(-wrongShakeMagnitude * 0.35f, wrongShakeMagnitude * 0.35f);

            target.localPosition = originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
    }

    private IEnumerator PopButton(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 originalScale = target.localScale;
        Vector3 enlargedScale = originalScale * correctScaleMultiplier;

        float halfDuration = correctPopDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            target.localScale = Vector3.Lerp(originalScale, enlargedScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = enlargedScale;
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            target.localScale = Vector3.Lerp(enlargedScale, originalScale, elapsed / halfDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = originalScale;
    }

    private void CountParentHelpIfNeeded()
    {
        if (parentModeManager == null)
            return;

        if (parentHelpUsedThisLevel)
            return;

        if (parentModeManager.IsParentModeActive)
        {
            parentHelpUsedThisLevel = true;
            StoryQuizOtigoSessionTracker.AddParentHelp();
        }
    }

    private void SaveLevelResult()
    {
        int duration = Mathf.RoundToInt(activePlayTime);
        int helpCount = parentHelpUsedThisLevel ? 1 : 0;

        StoryQuizOtigoSessionTracker.AddOrUpdateLevelResult(
            levelNumber,
            duration,
            mistakesMade,
            helpCount
        );
    }

    public void ToggleMute()
    {
        if (!isMuted)
        {
            if (AudioListener.volume > 0f)
                lastNonZeroVolume = AudioListener.volume;

            AudioListener.volume = 0f;
            isMuted = true;
        }
        else
        {
            AudioListener.volume = Mathf.Clamp(lastNonZeroVolume, 0.1f, 1f);
            isMuted = false;
        }

        if (volumeSlider != null)
            volumeSlider.value = AudioListener.volume;
    }

    public void OnVolumeSliderChanged(float value)
    {
        AudioListener.volume = value;

        if (value > 0f)
        {
            isMuted = false;
            lastNonZeroVolume = value;
        }
        else
        {
            isMuted = true;
        }
    }

    public void ReplayLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RestartLevel()
    {
        ReplayLevel();
    }

    public void GoMenu()
    {
        SaveCurrentLevelAsProgress();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void GoToMainMenu()
    {
        GoMenu();
    }

    public void GoNextLevel()
    {
        if (!answeredCorrectly)
            return;

        if (isLastLevel)
        {
            OtigoGameProgress.ClearKey(lastLevelKey);

            StoryQuizOtigoSessionTracker.SendFinalResultIfPossible();

            SceneManager.LoadScene(finalSceneName);
        }
        else
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                OtigoGameProgress.SaveLevel(lastLevelKey, nextSceneName);

                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.LogWarning("nextSceneName boş bırakılmış.");
            }
        }
    }

    public void GoToNextLevel()
    {
        GoNextLevel();
    }

    public void ReplayCurrentVoice()
    {
        StopAllCoroutines();
        StartCoroutine(ReplayStorySequence());
    }

    private IEnumerator ReplayStorySequence()
    {
        questionPhaseStarted = false;

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (storyPanel != null)
            storyPanel.SetActive(true);

        SetReplayVoiceButtonVisible(true);

        if (feedbackText != null)
            feedbackText.text = "";

        PlayStoryVoice();

        float waitTime = 2f;
        if (storyVoiceClip != null)
            waitTime = storyVoiceClip.length + 0.4f;

        yield return new WaitForSeconds(waitTime);

        ShowQuestionPhase();
    }

    protected void PlayStoryVoice()
    {
        if (voiceSource == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = storyVoiceClip;

        if (storyVoiceClip != null)
            voiceSource.Play();
    }

    protected void PlayQuestionVoice()
    {
        if (voiceSource == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = questionVoiceClip;

        if (questionVoiceClip != null)
            voiceSource.Play();
    }
}
