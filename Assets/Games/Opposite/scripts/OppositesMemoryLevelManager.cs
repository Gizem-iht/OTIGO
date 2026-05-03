using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OppositesMemoryLevelManager : MonoBehaviour
{
    public static OppositesMemoryLevelManager Instance;

    private const string VolumeKey = "OppositesVolume";
    private const string LAST_LEVEL_KEY = "Opposites_LastLevelSceneName";

    [Header("Level Info")]
    public int activityId = 10;
    public int levelNumber = 1;
    public string nextSceneName = "";
    public bool isLastLevel = false;

    [Header("Cards")]
    public List<OppositesCard> cards = new List<OppositesCard>();

    [Header("Preview")]
    public float previewSeconds = 5f;

    [Header("Buttons")]
    public Button replayButton;
    public Button nextButton;
    public Button menuButton;

    [Header("UI")]
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip introClip;
    public AudioClip startClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeClip;
    public AudioClip flipClip;

    private OppositesCard firstSelected;
    private OppositesCard secondSelected;

    private int mistakesMade = 0;
    private int matchedPairCount = 0;
    private int totalPairs = 0;

    private bool isBusy = false;
    private bool levelCompleted = false;
    private bool resultSaved = false;
    private bool gameplayStarted = false;

    private float activePlayTime = 0f;

    private string StatePrefix
    {
        get { return "Opposites_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SaveCurrentLevel();

        if (levelNumber == 1 && !HasSavedState())
            OppositesOtigoSessionTracker.BeginSession(activityId);
        else if (!OppositesOtigoSessionTracker.HasSession())
            OppositesOtigoSessionTracker.BeginSession(activityId);

        mistakesMade = 0;
        matchedPairCount = 0;
        totalPairs = cards.Count / 2;

        isBusy = false;
        levelCompleted = false;
        resultSaved = false;
        gameplayStarted = false;
        activePlayTime = 0f;

        firstSelected = null;
        secondSelected = null;

        SetupButtons();
        SetupSlider();

        StartCoroutine(DelayedStartRestore());
    }

    private IEnumerator DelayedStartRestore()
    {
        yield return null;

        if (HasSavedState())
        {
            LoadState();
            RestoreMatchedCards();

            if (levelCompleted)
                RestoreCompletedLevel();
            else
                gameplayStarted = true;
        }
        else
        {
            StartCoroutine(IntroThenPreviewRoutine());
        }
    }

    private IEnumerator IntroThenPreviewRoutine()
    {
        if (sfxSource != null && introClip != null)
        {
            gameplayStarted = false;
            isBusy = true;

            sfxSource.PlayOneShot(introClip);
            yield return new WaitForSeconds(introClip.length);
        }

        yield return StartCoroutine(PreviewThenStartRoutine());
    }

    private IEnumerator PreviewThenStartRoutine()
    {
        gameplayStarted = false;
        isBusy = true;

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (card.IsMatched) continue;

            card.OpenCard();
        }

        yield return new WaitForSeconds(previewSeconds);

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (card.IsMatched) continue;

            card.CloseCard();
        }

        isBusy = false;

        StartCoroutine(StartVoiceRoutine());
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (gameplayStarted && !levelCompleted && !parentModeActive && !IsInstructionAudioPlaying())
            activePlayTime += Time.deltaTime;
    }

    private bool IsInstructionAudioPlaying()
    {
        return sfxSource != null && sfxSource.isPlaying;
    }

    private IEnumerator StartVoiceRoutine()
    {
        gameplayStarted = false;

        if (sfxSource != null && startClip != null)
        {
            sfxSource.PlayOneShot(startClip);
            yield return new WaitForSeconds(startClip.length);
        }

        gameplayStarted = true;
        SaveState();
    }

    private void SetupButtons()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(RestartLevel);
            replayButton.gameObject.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(GoNext);
            nextButton.gameObject.SetActive(false);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoMenu);
        }
    }

    private void SetupSlider()
    {
        if (volumeSlider == null) return;

        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);

        volumeSlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;

        if (sfxSource != null)
            sfxSource.volume = value;
    }

    public void OnCardSelected(OppositesCard card)
    {
        if (!gameplayStarted) return;
        if (isBusy) return;
        if (levelCompleted) return;
        if (card == null) return;
        if (card == firstSelected) return;
        if (card.IsMatched) return;
        if (card.IsOpen) return;

        card.OpenCard();

        if (sfxSource != null && flipClip != null)
            sfxSource.PlayOneShot(flipClip);

        if (sfxSource != null && card.cardVoiceClip != null)
            sfxSource.PlayOneShot(card.cardVoiceClip);

        if (firstSelected == null)
        {
            firstSelected = card;
            return;
        }

        secondSelected = card;
        StartCoroutine(CheckMatchRoutine());
    }

    private IEnumerator CheckMatchRoutine()
    {
        isBusy = true;

        yield return new WaitForSeconds(0.8f);

        bool isCorrectMatch =
            firstSelected != null &&
            secondSelected != null &&
            firstSelected.pairId == secondSelected.pairId &&
            firstSelected.conceptId != secondSelected.conceptId;

        if (isCorrectMatch)
        {
            firstSelected.SetMatched();
            secondSelected.SetMatched();

            matchedPairCount++;

            SaveMatchedPair(firstSelected, secondSelected);

            if (sfxSource != null && correctClip != null)
                sfxSource.PlayOneShot(correctClip);

            if (ParentModeManager.Instance != null &&
                ParentModeManager.Instance.IsParentModeActive)
            {
                OppositesOtigoSessionTracker.AddParentHelpForLevel(levelNumber);
            }

            SaveState();

            if (matchedPairCount >= totalPairs)
            {
                levelCompleted = true;
                gameplayStarted = false;
                SaveState();
                yield return StartCoroutine(CompleteRoutine());
            }
        }
        else
        {
            mistakesMade++;

            if (sfxSource != null && wrongClip != null)
                sfxSource.PlayOneShot(wrongClip);

            yield return new WaitForSeconds(0.5f);

            if (firstSelected != null) firstSelected.CloseCard();
            if (secondSelected != null) secondSelected.CloseCard();

            SaveState();
        }

        firstSelected = null;
        secondSelected = null;
        isBusy = false;
    }

    private IEnumerator CompleteRoutine()
    {
        SaveLevelResult();

        yield return new WaitForSeconds(0.5f);

        if (sfxSource != null && completeClip != null)
            sfxSource.PlayOneShot(completeClip);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        SaveState();

        if (isLastLevel)
            OppositesOtigoSessionTracker.SendFinalResultIfPossible();
    }

    private void SaveLevelResult()
    {
        if (resultSaved) return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        OppositesOtigoSessionTracker.AddOrUpdateLevelResult(
            levelNumber,
            durationSeconds,
            mistakesMade
        );

        SaveState();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    public void RestartLevel()
    {
        ClearState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoNext()
    {
        ClearState();

        if (isLastLevel)
        {
            PlayerPrefs.DeleteKey(LAST_LEVEL_KEY);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Opposites_TebrikScene");
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            PlayerPrefs.SetString(LAST_LEVEL_KEY, nextSceneName);
            PlayerPrefs.Save();
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void GoMenu()
    {
        SaveState();
        SaveCurrentLevel();
        SceneManager.LoadScene("Opposites_MainMenu");
    }

    private void SaveCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LAST_LEVEL_KEY, currentSceneName);
        PlayerPrefs.Save();
    }

    private void SaveMatchedPair(OppositesCard a, OppositesCard b)
    {
        if (a != null && !string.IsNullOrEmpty(a.cardId))
            PlayerPrefs.SetInt(StatePrefix + "Matched_" + a.cardId, 1);

        if (b != null && !string.IsNullOrEmpty(b.cardId))
            PlayerPrefs.SetInt(StatePrefix + "Matched_" + b.cardId, 1);

        PlayerPrefs.Save();
    }

    private void RestoreMatchedCards()
    {
        HashSet<string> donePairs = new HashSet<string>();

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.cardId)) continue;

            bool matched = PlayerPrefs.GetInt(StatePrefix + "Matched_" + card.cardId, 0) == 1;

            if (matched)
            {
                card.SetMatched();

                if (!string.IsNullOrEmpty(card.pairId))
                    donePairs.Add(card.pairId);
            }
        }

        matchedPairCount = donePairs.Count;
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "MistakesMade", mistakesMade);
        PlayerPrefs.SetInt(StatePrefix + "MatchedPairCount", matchedPairCount);
        PlayerPrefs.SetInt(StatePrefix + "LevelCompleted", levelCompleted ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        mistakesMade = PlayerPrefs.GetInt(StatePrefix + "MistakesMade", 0);
        matchedPairCount = PlayerPrefs.GetInt(StatePrefix + "MatchedPairCount", 0);
        levelCompleted = PlayerPrefs.GetInt(StatePrefix + "LevelCompleted", 0) == 1;
        resultSaved = PlayerPrefs.GetInt(StatePrefix + "ResultSaved", 0) == 1;
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "MistakesMade");
        PlayerPrefs.DeleteKey(StatePrefix + "MatchedPairCount");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelCompleted");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");

        foreach (OppositesCard card in cards)
        {
            if (card == null) continue;
            if (string.IsNullOrEmpty(card.cardId)) continue;

            PlayerPrefs.DeleteKey(StatePrefix + "Matched_" + card.cardId);
        }

        PlayerPrefs.Save();
    }
}
