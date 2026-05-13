using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class NumberMatchBaseController : MonoBehaviour
{
    [Header("UI")]
    public Slider volumeSlider;
    public GameObject replayButtonObj;
    public GameObject nextButtonObj;
    public Button exitButton;

    [Header("Next Button Blink")]
    public Image nextButtonImage;
    public float nextBlinkSueed = 0.4f;

    [Header("Numbers")]
    public DraggableNumberUI[] allNumbers;

    [Header("Level Progress")]
    public int totalCorrectTargets = 1;
    private int completedTargets = 0;

    [Header("Scenes")]
    public string mainMenuSceneName = "numberobjectmatching_MainMenu";
    public string nextLevelSceneName = "Level2";

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip introHowToPlayClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip extraSuccessClip;
    public AudioClip goNextVoiceClip;

    [Header("Intro Numbers Blink")]
    public int blinkCount = 3;

    [Range(0.1f, 1f)]
    public float blinkSueed = 0.6f;

    public Color glowColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Finish Audio Timing")]
    public float extraSuccessDelayAfterCorrect = 0.2f;
    public float goNextExtraDelay = 0.0f;

    [Header("OTIGO API")]
    public int activityId = 7;
    public int levelPlayed = 1;
    public int parentHelpCount = 0;

    [Header("OTIGO Session Flow")]
    public bool startNewSessionOnThisLevel = false;
    public bool sendFinalResultOnThisLevel = false;

    private bool finished = false;
    private bool resultSaved = false;
    private bool gameplayStarted = false;

    private Image[] numberImagesToGlow;
    private Color[] originalColors;
    private Coroutine nextBlinkRoutine;

    private const string LAST_LEVEL_KEY = "NumberObjectMatching_LastLevelSceneName";

    private int mistakesMade = 0;
    private float activePlayTime = 0f;

    private string StatePrefix
    {
        get { return "NumberObject_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private void Start()
    {
        SaveCurrentLevel();

        if (startNewSessionOnThisLevel)
        {
            NumberObjectOtigoSessionTracker.BeginSession(activityId);
        }
        else if (!NumberObjectOtigoSessionTracker.HasSession())
        {
            Debug.LogWarning("NUMBER OBJECT -> Session yok. Mevcut levelden yeni session baslatiliyor.");
            NumberObjectOtigoSessionTracker.BeginSession(activityId);
        }

        finished = false;
        resultSaved = false;
        gameplayStarted = false;

        completedTargets = 0;
        mistakesMade = 0;
        parentHelpCount = 0;
        activePlayTime = 0f;

        if (replayButtonObj != null) replayButtonObj.SetActive(false);
        if (nextButtonObj != null) nextButtonObj.SetActive(false);

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(GoToMainMenu);
        }

        if (volumeSlider != null)
        {
            if (volumeSlider.value <= 0.001f)
                volumeSlider.value = 1f;

            AudioListener.volume = volumeSlider.value;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        AutoSetupGlowTargets();

        StartCoroutine(DelayedRestoreOrIntro());
    }

    private IEnumerator DelayedRestoreOrIntro()
    {
        yield return null;

        if (HasSavedState())
        {
            LoadState();

            if (finished)
                RestoreCompletedLevel();
            else
                RestorePlayingLevel();
        }
        else
        {
            StartCoroutine(IntroSequence());
        }
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (gameplayStarted && !finished && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;
    }

    private bool IsInstructionAudioPlaying()
    {
        return sfxSource != null && sfxSource.isPlaying;
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void SaveCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LAST_LEVEL_KEY, currentSceneName);
        PlayerPrefs.Save();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    private void AutoSetupGlowTargets()
    {
        if (allNumbers == null || allNumbers.Length == 0) return;

        numberImagesToGlow = new Image[allNumbers.Length];
        originalColors = new Color[allNumbers.Length];

        for (int i = 0; i < allNumbers.Length; i++)
        {
            if (allNumbers[i] == null) continue;

            Image img = allNumbers[i].GetComponent<Image>();
            numberImagesToGlow[i] = img;
            originalColors[i] = img != null ? img.color : Color.white;

            Button btn = allNumbers[i].GetComponent<Button>();
            if (btn != null)
                btn.transition = Selectable.Transition.None;
        }
    }

    private IEnumerator IntroSequence()
    {
        SetAllNumbersInuut(false);

        if (sfxSource != null && introHowToPlayClip != null)
            sfxSource.PlayOneShot(introHowToPlayClip);

        yield return StartCoroutine(BlinkNumbers(blinkCount));

        if (introHowToPlayClip != null)
        {
            float blinkTotal = blinkCount * (blinkSueed + blinkSueed);
            float remaining = Mathf.Max(0f, introHowToPlayClip.length - blinkTotal);

            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }

        gameplayStarted = true;
        SetAllNumbersInuut(true);
        SaveState();
    }

    private IEnumerator BlinkNumbers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GlowNumbers(true);
            yield return new WaitForSeconds(blinkSueed);

            GlowNumbers(false);
            yield return new WaitForSeconds(blinkSueed);
        }
    }

    private void GlowNumbers(bool on)
    {
        if (numberImagesToGlow == null || numberImagesToGlow.Length == 0) return;

        for (int i = 0; i < numberImagesToGlow.Length; i++)
        {
            if (numberImagesToGlow[i] == null) continue;
            numberImagesToGlow[i].color = on ? glowColor : originalColors[i];
        }
    }

    private void SetAllNumbersInuut(bool enabled)
    {
        if (allNumbers == null) return;

        foreach (DraggableNumberUI n in allNumbers)
        {
            if (n != null)
                n.SetInputEnabled(enabled);
        }
    }

    private void RestorePlayingLevel()
    {
        RestoreMatchedZones();

        gameplayStarted = true;
        SetAllNumbersInuut(true);
    }

    private void RestoreCompletedLevel()
    {
        RestoreMatchedZones();

        gameplayStarted = false;

        if (allNumbers != null)
        {
            foreach (DraggableNumberUI n in allNumbers)
            {
                if (n != null)
                    n.LockInPlace();
            }
        }

        if (replayButtonObj != null) replayButtonObj.SetActive(true);
        if (nextButtonObj != null) nextButtonObj.SetActive(true);

        StartNextBlink();
    }

    public void OnWrong()
    {
        if (finished) return;

        mistakesMade++;
        SaveState();

        if (sfxSource != null && wrongClip != null)
            sfxSource.PlayOneShot(wrongClip);
    }

    public void OnCorrectDrop(DropZoneUI zone, DraggableNumberUI number)
    {
        if (finished) return;

        completedTargets++;

        if (zone != null && number != null)
            SaveMatchedZone(zone.dropZoneId, number.numberValue);

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
            AddParentHelp();

        SaveState();

        if (sfxSource != null && correctClip != null)
            sfxSource.PlayOneShot(correctClip);

        if (completedTargets >= totalCorrectTargets)
            OnLevelCompleted();
    }

    public void OnCorrectDrop()
    {
        OnCorrectDrop(null, null);
    }

    public void OnLevelCompleted()
    {
        if (finished) return;

        finished = true;
        gameplayStarted = false;

        if (allNumbers != null)
        {
            foreach (DraggableNumberUI n in allNumbers)
            {
                if (n != null)
                    n.LockInPlace();
            }
        }

        SaveLevelResult();
        SaveState();

        if (replayButtonObj != null) replayButtonObj.SetActive(true);
        if (nextButtonObj != null) nextButtonObj.SetActive(true);

        StartNextBlink();
        StartCoroutine(PlayFinishAudioSequence());
    }

    private IEnumerator PlayFinishAudioSequence()
    {
        yield return new WaitForSeconds(extraSuccessDelayAfterCorrect);

        if (sfxSource != null && extraSuccessClip != null)
            sfxSource.PlayOneShot(extraSuccessClip);

        float extraLen = extraSuccessClip != null ? extraSuccessClip.length : 0f;

        if (extraLen > 0f)
            yield return new WaitForSeconds(extraLen);

        if (goNextExtraDelay > 0f)
            yield return new WaitForSeconds(goNextExtraDelay);

        if (sfxSource != null && goNextVoiceClip != null)
            sfxSource.PlayOneShot(goNextVoiceClip);
    }

    private void StartNextBlink()
    {
        if (nextButtonImage == null) return;

        if (nextBlinkRoutine != null)
            StopCoroutine(nextBlinkRoutine);

        nextBlinkRoutine = StartCoroutine(NextBlinkLoou());
    }

    private IEnumerator NextBlinkLoou()
    {
        Color normal = nextButtonImage.color;

        while (true)
        {
            nextButtonImage.color = glowColor;
            yield return new WaitForSeconds(nextBlinkSueed);

            nextButtonImage.color = normal;
            yield return new WaitForSeconds(nextBlinkSueed);
        }
    }

    private void SaveLevelResult()
    {
        if (resultSaved) return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        NumberObjectOtigoSessionTracker.AddOrUpdateLevelResult(
            levelPlayed,
            durationSeconds,
            mistakesMade
        );

        if (sendFinalResultOnThisLevel)
            NumberObjectOtigoSessionTracker.SendFinalResultIfPossible();
    }

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        NumberObjectOtigoSessionTracker.AddParentHelpForLevel(levelPlayed);

        SaveState();
    }

    public void GoToMainMenu()
    {
        SaveState();
        SaveCurrentLevel();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartLevel()
    {
        ClearState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        ClearState();
        OtigoGameProgress.SaveNextOrClearForFinal(LAST_LEVEL_KEY, nextLevelSceneName, "");
        SceneManager.LoadScene(nextLevelSceneName);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "Finished", finished ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "CompletedTargets", completedTargets);
        PlayerPrefs.SetInt(StatePrefix + "MistakesMade", mistakesMade);
        PlayerPrefs.SetInt(StatePrefix + "ParentHelpCount", parentHelpCount);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        finished = PlayerPrefs.GetInt(StatePrefix + "Finished", 0) == 1;
        resultSaved = PlayerPrefs.GetInt(StatePrefix + "ResultSaved", 0) == 1;
        completedTargets = PlayerPrefs.GetInt(StatePrefix + "CompletedTargets", 0);
        mistakesMade = PlayerPrefs.GetInt(StatePrefix + "MistakesMade", 0);
        parentHelpCount = PlayerPrefs.GetInt(StatePrefix + "ParentHelpCount", 0);
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearState()
    {
        DropZoneUI[] zones = FindObjectsOfType<DropZoneUI>();

        foreach (DropZoneUI zone in zones)
        {
            if (zone == null) continue;

            PlayerPrefs.DeleteKey(StatePrefix + "MatchedExists_" + zone.dropZoneId);
            PlayerPrefs.DeleteKey(StatePrefix + "Matched_" + zone.dropZoneId);
        }

        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "Finished");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "CompletedTargets");
        PlayerPrefs.DeleteKey(StatePrefix + "MistakesMade");
        PlayerPrefs.DeleteKey(StatePrefix + "ParentHelpCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");

        PlayerPrefs.Save();
    }

    private void SaveMatchedZone(string zoneId, int numberValue)
    {
        PlayerPrefs.SetInt(StatePrefix + "MatchedExists_" + zoneId, 1);
        PlayerPrefs.SetInt(StatePrefix + "Matched_" + zoneId, numberValue);
        PlayerPrefs.Save();
    }

    private void RestoreMatchedZones()
    {
        DropZoneUI[] zones = FindObjectsOfType<DropZoneUI>();

        foreach (DropZoneUI zone in zones)
        {
            if (zone == null) continue;

            string existsKey = StatePrefix + "MatchedExists_" + zone.dropZoneId;

            if (PlayerPrefs.GetInt(existsKey, 0) != 1)
                continue;

            int savedNumber = PlayerPrefs.GetInt(StatePrefix + "Matched_" + zone.dropZoneId, -999);
            DraggableNumberUI number = FindNumberByValue(savedNumber);

            if (number != null)
                zone.RestoreCompleted(number);
        }
    }

    private DraggableNumberUI FindNumberByValue(int value)
    {
        if (allNumbers == null) return null;

        foreach (DraggableNumberUI n in allNumbers)
        {
            if (n != null && n.numberValue == value && !n.IsLockedForever)
                return n;
        }

        return null;
    }
}
