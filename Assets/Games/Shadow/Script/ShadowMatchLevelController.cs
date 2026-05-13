using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

#region Deneme Geçmişi Modeli
[System.Serializable]
public class ShadowMatchAttemptEntry
{
    public int attemptNumber;
    public int correct;
    public int wrong;
    public string time;
    public string date;
    public string sceneName;

    public ShadowMatchAttemptEntry(int num, int correct, int wrong)
    {
        attemptNumber = num;
        this.correct = correct;
        this.wrong = wrong;
        time = System.DateTime.Now.ToString("HH:mm:ss");
        date = System.DateTime.Now.ToString("yyyy-MM-dd");
        sceneName = SceneManager.GetActiveScene().name;
    }
}

[System.Serializable]
public class ShadowMatchAttemptListWrapper
{
    public List<ShadowMatchAttemptEntry> items = new List<ShadowMatchAttemptEntry>();
}
#endregion

public class ShadowMatchLevelController : MonoBehaviour
{
    private const string LastLevelKey = "LastPlayedLevel";
    private const string MainMenuSceneName = "ShadowMatch_MainMenu";

    [Header("Scene")]
    public string nextLevelSceneName;

    [Header("UI Buttons")]
    public Button nextButton;
    public Button restartButton;
    public Button exitButton;

    [Header("Shadows")]
    public List<Transform> allShadows = new List<Transform>();

    [Header("FX")]
    public ParticleSystem confetti;
    public AudioSource audioSource;
    public AudioClip confettiClip;

    [Header("Score")]
    public int correctMatches = 0;
    public int wrongMatches = 0;

    [Header("Parent Help")]
    public int parentHelpCount = 0;

    [Header("Sayaç")]
    public int retryCount = 0;

    [Header("Geçmiş")]
    public List<ShadowMatchAttemptEntry> attemptHistory = new List<ShadowMatchAttemptEntry>();

    [Header("Intro Voice")]
    public AudioClip introInstructionClip;
    [Range(0f, 2f)] public float introDelay = 0.25f;
    public bool playIntroEveryTime = true;
    private static bool _introPlayedThisSession = false;

    [Header("OTIGO")]
    public int activityId = 2;
    public int levelPlayed = 1;
    public bool isFinalLevel = false;

    private bool levelCompleted = false;
    private bool resultSaved = false;
    private bool gameplayStarted = false;

    private float activePlayTime = 0f;

    private string StatePrefix
    {
        get
        {
            string scene = SceneManager.GetActiveScene().name;
            string baseKey = "ShadowMatch_State_" + scene + "_";
            if (ShadowMatchLevelVariantRandomizer.UsesVariantSaveSuffix)
            {
                return baseKey +
                    "v" +
                    ShadowMatchLevelVariantRandomizer.ActiveVariantIndex +
                    "_";
            }

            return baseKey;
        }
    }

    private static ShadowMatchDragSnap[] GetAllDragSnapPieces()
    {
        return Object.FindObjectsByType<ShadowMatchDragSnap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private void Awake()
    {
        ShadowMatchSoundToggle.SyncAudioListenerFromPlayerPrefs();

        LoadPersistentRetry();
        LoadHistory();
        SaveLastPlayedLevelForMainMenu();

        if (confetti != null)
        {
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confetti.gameObject.SetActive(true);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        SetEndButtonsActive(false);

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitToMainMenu);
        }

        if (!ShadowMatchOtigoSessionTracker.HasSession())
            ShadowMatchOtigoSessionTracker.BeginSession(activityId);

        correctMatches = 0;
        wrongMatches = 0;
        parentHelpCount = 0;

        resultSaved = false;
        levelCompleted = false;
        gameplayStarted = false;
        activePlayTime = 0f;

        if (HasSavedState())
        {
            LoadState();

            if (levelCompleted)
            {
                ClearState();
                ResetPiecesToStart();
                ResetRuntimeState();
                gameplayStarted = true;
            }
            else
            {
                RestoreMatchedPieces();
                gameplayStarted = true;
            }
        }
        else
        {
            if (introInstructionClip != null && (playIntroEveryTime || !_introPlayedThisSession))
            {
                StartCoroutine(PlayIntroCo());
                _introPlayedThisSession = true;
            }
            else
            {
                gameplayStarted = true;
                SaveState();
            }
        }
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
        return audioSource != null && audioSource.isPlaying;
    }

    private IEnumerator PlayIntroCo()
    {
        gameplayStarted = false;

        if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        if (audioSource != null && introInstructionClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(introInstructionClip);
            yield return new WaitForSeconds(introInstructionClip.length);
        }

        gameplayStarted = true;
        SaveState();
    }

    private void SetEndButtonsActive(bool active)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(active);

        if (restartButton != null)
            restartButton.gameObject.SetActive(active);
    }

    private void SaveLastPlayedLevelForMainMenu()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LastLevelKey, currentSceneName);
        PlayerPrefs.Save();
    }

    private string SceneKey(string key)
    {
        return SceneManager.GetActiveScene().name + "_" + key;
    }

    private string HistoryKey()
    {
        return SceneKey("history");
    }

    private void LoadPersistentRetry()
    {
        retryCount = PlayerPrefs.GetInt(SceneKey("retry"), 0);
        correctMatches = 0;
        wrongMatches = 0;
    }

    private void SavePersistentRetry()
    {
        PlayerPrefs.SetInt(SceneKey("retry"), retryCount);
        PlayerPrefs.Save();
    }

    private void LoadHistory()
    {
        string json = PlayerPrefs.GetString(HistoryKey(), string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            ShadowMatchAttemptListWrapper wrau =
                JsonUtility.FromJson<ShadowMatchAttemptListWrapper>(json);

            attemptHistory =
                wrau != null && wrau.items != null
                    ? wrau.items
                    : new List<ShadowMatchAttemptEntry>();
        }
        else
        {
            attemptHistory = new List<ShadowMatchAttemptEntry>();
        }
    }

    private void SaveHistory()
    {
        ShadowMatchAttemptListWrapper wrau =
            new ShadowMatchAttemptListWrapper { items = attemptHistory };

        string json = JsonUtility.ToJson(wrau);

        PlayerPrefs.SetString(HistoryKey(), json);
        PlayerPrefs.Save();
    }

    public void RegisterRetry(bool resetSessionScores = true)
    {
        retryCount++;

        ShadowMatchAttemptEntry entry =
            new ShadowMatchAttemptEntry(retryCount, correctMatches, wrongMatches);

        attemptHistory.Add(entry);

        SaveHistory();
        SavePersistentRetry();

        if (resetSessionScores)
        {
            correctMatches = 0;
            wrongMatches = 0;
            parentHelpCount = 0;
        }
    }

    public void ResetAllStatsForThisLevel()
    {
        correctMatches = 0;
        wrongMatches = 0;
        retryCount = 0;
        parentHelpCount = 0;
        attemptHistory.Clear();

        PlayerPrefs.DeleteKey(SceneKey("retry"));
        PlayerPrefs.DeleteKey(HistoryKey());

        ClearState();

        PlayerPrefs.Save();
    }

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        ShadowMatchOtigoSessionTracker.AddParentHelp();

        SaveState();
    }

    private void SaveLevelResult()
    {
        if (resultSaved)
            return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        ShadowMatchOtigoSessionTracker.AddOrUpdateLevelResult(
            levelPlayed,
            durationSeconds,
            wrongMatches,
            parentHelpCount
        );

        SaveState();
    }

    public void CheckPlacement(ShadowMatchDragSnap piece, Vector3 dropPosition)
    {
        if (!gameplayStarted) return;
        if (piece == null) return;
        if (piece.IsLocked) return;
        if (levelCompleted) return;

        bool snauued = false;

        if (piece.target != null)
        {
            float distToTarget = Vector2.Distance(dropPosition, piece.target.position);

            if (distToTarget <= piece.snapRadius)
            {
                piece.ForcePlaceToTarget();

                correctMatches++;
                SaveMatchedPiece(piece);

                if (ParentModeManager.Instance != null &&
                    ParentModeManager.Instance.IsParentModeActive)
                {
                    AddParentHelp();
                }

                if (piece.audioSource != null && piece.clapClip != null)
                    piece.audioSource.PlayOneShot(piece.clapClip);

                snauued = true;
                SaveState();

                CheckLevelComplete();
            }
        }

        if (!snauued && allShadows != null && allShadows.Count > 0)
        {
            foreach (Transform shadow in allShadows)
            {
                if (shadow == null || !shadow.gameObject.activeInHierarchy)
                    continue;

                float distToShadow = Vector2.Distance(dropPosition, shadow.position);

                if (distToShadow <= piece.snapRadius)
                {
                    wrongMatches++;

                    if (piece.audioSource != null && piece.wrongClip != null)
                        piece.audioSource.PlayOneShot(piece.wrongClip);

                    piece.ForceResetToStart();

                    snauued = true;
                    SaveState();
                    break;
                }
            }
        }

        if (!snauued)
        {
            if (piece.returnIfMiss)
                piece.ForceResetToStart();
            else
                piece.LockPiece(false);

            SaveState();
        }
    }

    private void CheckLevelComplete()
    {
        if (levelCompleted)
            return;

        foreach (ShadowMatchDragSnap piece in GetAllDragSnapPieces())
        {
            if (piece == null || !piece.gameObject.activeInHierarchy)
                continue;

            if (!piece.IsLocked)
                return;
        }

        levelCompleted = true;
        gameplayStarted = false;

        SaveLevelResult();

        if (confetti != null)
            confetti.Play();

        if (audioSource != null && confettiClip != null)
            audioSource.PlayOneShot(confettiClip);

        SetEndButtonsActive(true);
        SaveState();

        if (isFinalLevel)
            ShadowMatchOtigoSessionTracker.SendFinalResultIfPossible();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;
        SetEndButtonsActive(true);

        if (confetti != null)
            confetti.Play();
    }

    private void ResetRuntimeState()
    {
        correctMatches = 0;
        wrongMatches = 0;
        parentHelpCount = 0;
        resultSaved = false;
        levelCompleted = false;
        activePlayTime = 0f;
    }

    public void NextLevel()
    {
        if (!levelCompleted)
            return;

        if (string.IsNullOrEmpty(nextLevelSceneName))
            return;

        ClearState();

        if (isFinalLevel || OtigoGameProgress.IsFinalScene(nextLevelSceneName))
            OtigoGameProgress.ClearKey(LastLevelKey);
        else
            OtigoGameProgress.SaveLevel(LastLevelKey, nextLevelSceneName);

        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void RestartLevel()
    {
        if (!levelCompleted)
            return;

        RegisterRetry(true);
        ClearState();
        ShadowMatchLevelVariantRandomizer.MarkReloadShouldPreferDifferentVariant();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        SaveState();
        SaveLastPlayedLevelForMainMenu();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void SaveMatchedPiece(ShadowMatchDragSnap piece)
    {
        if (piece == null) return;

        string id = piece.GetSaveId();

        PlayerPrefs.SetInt(StatePrefix + "Matched_" + id, 1);
        PlayerPrefs.Save();
    }

    private void RestoreMatchedPieces()
    {
        foreach (ShadowMatchDragSnap piece in GetAllDragSnapPieces())
        {
            if (piece == null || !piece.gameObject.activeInHierarchy)
                continue;

            string id = piece.GetSaveId();

            bool matched =
                PlayerPrefs.GetInt(StatePrefix + "Matched_" + id, 0) == 1;

            if (matched)
                piece.ForcePlaceToTarget();
            else
                piece.ForceResetToStart();
        }
    }

    private void ResetPiecesToStart()
    {
        foreach (ShadowMatchDragSnap piece in GetAllDragSnapPieces())
        {
            if (piece == null)
                continue;

            piece.ForceResetToStart();
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "CorrectMatches", correctMatches);
        PlayerPrefs.SetInt(StatePrefix + "WrongMatches", wrongMatches);
        PlayerPrefs.SetInt(StatePrefix + "ParentHelpCount", parentHelpCount);
        PlayerPrefs.SetInt(StatePrefix + "LevelCompleted", levelCompleted ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        correctMatches = PlayerPrefs.GetInt(StatePrefix + "CorrectMatches", 0);
        wrongMatches = PlayerPrefs.GetInt(StatePrefix + "WrongMatches", 0);
        parentHelpCount = PlayerPrefs.GetInt(StatePrefix + "ParentHelpCount", 0);
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
        PlayerPrefs.DeleteKey(StatePrefix + "CorrectMatches");
        PlayerPrefs.DeleteKey(StatePrefix + "WrongMatches");
        PlayerPrefs.DeleteKey(StatePrefix + "ParentHelpCount");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelCompleted");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");

        foreach (ShadowMatchDragSnap piece in GetAllDragSnapPieces())
        {
            if (piece == null)
                continue;

            PlayerPrefs.DeleteKey(StatePrefix + "Matched_" + piece.GetSaveId());
        }

        PlayerPrefs.Save();
    }
}
