using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class PuzzleGameManager : MonoBehaviour
{
    public const string VolumePrefsKey = "Puzzle_GameVolume";
    private const string LastNonZeroVolumeKey = "Puzzle_LastNonZeroVol";

    /// <summary>Kayıtlı tercih yokken (ilk açılış) kullanılan varsayılan — slider ortası.</summary>
    public const float DefaultVolumeLevel = 0.5f;

    [System.Serializable]
    public class PuzzleAttempt
    {
        public int attemptNumber;
        public int correct;
        public int wrong;
        public string date;
        public string time;

        public PuzzleAttempt(int number, int correct, int wrong)
        {
            attemptNumber = number;
            this.correct = correct;
            this.wrong = wrong;
            date = System.DateTime.Now.ToString("yyyy-MM-dd");
            time = System.DateTime.Now.ToString("HH:mm:ss");
        }
    }

    [System.Serializable]
    public class PuzzleAttemptList
    {
        public List<PuzzleAttempt> items = new List<PuzzleAttempt>();
    }

    [Header("Parçalar & UI")]
    public PuzzlePiece[] allPieces;
    public GameObject replayButton;
    public GameObject nextLevelButton;
    public ParticleSystem confettiEffect;
    public AudioClip winSound;

    [Header("Scene Names")]
    public string mainMenuSceneName = "Puzzle_MainMenu";
    public string tebrikSceneName = "Puzzle_TebrikScene";

    [Header("Ses")]
    public Slider volumeSlider;
    public Button soundButton;
    public Sprite soundOnIcon;
    public Sprite soundOffIcon;

    [Header("Level Start Sesi")]
    public bool playStartSound = true;
    public AudioClip startClip;
    [Range(0f, 1f)] public float startVolume = 1f;
    public float startDelay = 0f;

    [Header("Sayaçlar")]
    public int inspectorCorrect = 0;
    public int inspectorWrong = 0;
    public int inspectorRetries = 0;

    [Header("Parent Help")]
    public int parentHelpCount = 0;

    [Header("Geçmiş")]
    public List<PuzzleAttempt> attemptsDebug = new List<PuzzleAttempt>();

    [Header("OTIGO")]
    public int activityId = 8;
    public int levelPlayed = 1;
    public bool startNewSessionOnThisLevel = false;
    public bool sendFinalResultOnThisLevel = false;

    private int correctCount = 0;
    private int wrongCount = 0;
    private int ulacedPieceCount = 0;
    private int currentSceneIndex;

    private bool isSoundOn = true;
    private bool resultSaved = false;
    private bool levelFinished = false;
    private bool gameplayStarted = false;

    private float activePlayTime = 0f;

    private AudioSource managerAudioSource;

    private const string LastLevelKey = "Puzzle_LastLevelName";

    /// <summary>
    /// Ana menü veya level; PlayerPrefs'teki Puzzle ses seviyesini dinleyiciye uygular.
    /// </summary>
    public static void SyncAudioListenerFromPlayerPrefs()
    {
        float v;
        if (!PlayerPrefs.HasKey(VolumePrefsKey))
        {
            v = DefaultVolumeLevel;
            PlayerPrefs.SetFloat(VolumePrefsKey, v);
            PlayerPrefs.Save();
        }
        else
            v = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey));

        AudioListener.volume = v;
    }

    public static void ApplyAndPersistGlobalVolume(float value)
    {
        float c = Mathf.Clamp01(value);
        AudioListener.volume = c;
        PlayerPrefs.SetFloat(VolumePrefsKey, c);
        PlayerPrefs.Save();
        if (c > 0.01f)
            PlayerPrefs.SetFloat(LastNonZeroVolumeKey, c);
    }

    private string StatePrefix
    {
        get { return "Puzzle_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private string AttemptsKey()
    {
        return "PUZZLE_ATTEMPTS_" + SceneManager.GetActiveScene().name;
    }

    private string RetryKey()
    {
        return "PUZZLE_RETRY_" + SceneManager.GetActiveScene().name;
    }

    private void Awake()
    {
        managerAudioSource = GetComponent<AudioSource>();

        if (managerAudioSource == null)
            managerAudioSource = gameObject.AddComponent<AudioSource>();

        managerAudioSource.playOnAwake = false;
        managerAudioSource.loop = false;

        SyncAudioListenerFromPlayerPrefs();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    private void Start()
    {
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SaveCurrentLevel();

        if (replayButton != null)
            replayButton.SetActive(false);

        if (nextLevelButton != null)
            nextLevelButton.SetActive(false);

        if (confettiEffect != null)
        {
            confettiEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            confettiEffect.gameObject.SetActive(false);
        }

        SetupVolumeSlider();
        SetupSoundButton();

        inspectorRetries = PlayerPrefs.GetInt(RetryKey(), 0);
        attemptsDebug = LoadAttempts().items;

        correctCount = 0;
        wrongCount = 0;
        ulacedPieceCount = 0;
        parentHelpCount = 0;

        resultSaved = false;
        levelFinished = false;
        gameplayStarted = false;
        activePlayTime = 0f;

        if (startNewSessionOnThisLevel && !HasSavedState())
        {
            if (!PuzzleOtigoSessionTracker.HasSession())
                PuzzleOtigoSessionTracker.BeginSession(activityId);
        }
        else if (!PuzzleOtigoSessionTracker.HasSession())
        {
            Debug.LogWarning("PUZZLE -> Session yok. İlk levelde startNewSessionOnThisLevel true olmalı.");
        }

        SetupPieces();

        if (HasSavedState())
        {
            LoadState();
            RestorePieces();

            if (levelFinished)
                RestoreCompletedLevel();
            else
                gameplayStarted = true;
        }
        else
        {
            StartCoroutine(StartLevelRoutine());
        }

        PushCountsToInspector();
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
        return managerAudioSource != null && managerAudioSource.isPlaying;
    }

    private void SetupVolumeSlider()
    {
        if (volumeSlider == null)
            return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;

        float saved =
            Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolumeLevel));

        volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
        volumeSlider.SetValueWithoutNotify(saved);
        SyncAudioListenerFromPlayerPrefs();
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnVolumeSliderChanged(float value)
    {
        ApplyAndPersistGlobalVolume(value);
        isSoundOn = AudioListener.volume > 0.0001f;
        UpdateSoundButtonVisual();
    }

    private void SetupSoundButton()
    {
        if (soundButton == null) return;

        soundButton.onClick.RemoveAllListeners();
        soundButton.onClick.AddListener(ToggleSound);

        isSoundOn = AudioListener.volume > 0.0001f;
        UpdateSoundButtonVisual();
    }

    private void SetupPieces()
    {
        if (allPieces == null || allPieces.Length == 0)
            allPieces = FindObjectsOfType<PuzzlePiece>(true);

        foreach (PuzzlePiece piece in allPieces)
        {
            if (piece == null) continue;
            piece.SetManager(this);
        }
    }

    private IEnumerator StartLevelRoutine()
    {
        gameplayStarted = false;

        if (playStartSound && startClip != null)
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            managerAudioSource.PlayOneShot(startClip, startVolume);
            yield return new WaitForSeconds(startClip.length);
        }

        gameplayStarted = true;
        SaveState();
    }

    public void RegisterCorrect()
    {
        RegisterCorrect(null);
    }

    public void RegisterCorrect(PuzzlePiece piece)
    {
        if (levelFinished)
            return;

        correctCount++;
        ulacedPieceCount++;

        if (piece != null)
            SavePlacedPiece(piece);

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
            AddParentHelp();

        PushCountsToInspector();
        SaveState();

        if (allPieces != null && ulacedPieceCount >= allPieces.Length)
            PuzzleFinished();
    }

    public void RegisterWrong()
    {
        if (levelFinished)
            return;

        wrongCount++;
        PushCountsToInspector();
        SaveState();

        Debug.Log("PUZZLE -> wrongCount: " + wrongCount);
    }

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        PuzzleOtigoSessionTracker.AddParentHelpForLevel(levelPlayed);

        SaveState();

        Debug.Log("[PuzzleGameManager] Parent help count arttı: " + parentHelpCount);
    }

    private void PushCountsToInspector()
    {
        inspectorCorrect = correctCount;
        inspectorWrong = wrongCount;
    }

    private void SaveLevelResult()
    {
        if (resultSaved)
            return;

        resultSaved = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        PuzzleOtigoSessionTracker.AddOrUpdateLevelResult(
            levelPlayed,
            durationSeconds,
            wrongCount
        );

        SaveState();

        Debug.Log("[PuzzleGameManager] Level sonucu kaydedildi -> " +
                  "levelPlayed: " + levelPlayed +
                  ", durationSeconds: " + durationSeconds +
                  ", wrongCount: " + wrongCount +
                  ", parentHelpCount: " + parentHelpCount);
    }

    private void PuzzleFinished()
    {
        if (levelFinished)
            return;

        levelFinished = true;
        gameplayStarted = false;

        SaveLevelResult();

        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Play(true);
        }

        if (managerAudioSource != null && winSound != null)
            managerAudioSource.PlayOneShot(winSound);

        if (replayButton != null)
            replayButton.SetActive(true);

        if (nextLevelButton != null)
            nextLevelButton.SetActive(true);

        SaveState();

        if (sendFinalResultOnThisLevel)
            PuzzleOtigoSessionTracker.SendFinalResultIfPossible();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (replayButton != null)
            replayButton.SetActive(true);

        if (nextLevelButton != null)
            nextLevelButton.SetActive(true);
    }

    private void SavePlacedPiece(PuzzlePiece piece)
    {
        if (piece == null) return;

        string id = piece.GetSaveId();
        PlayerPrefs.SetInt(StatePrefix + "Placed_" + id, 1);
        PlayerPrefs.Save();
    }

    private void RestorePieces()
    {
        ulacedPieceCount = 0;

        foreach (PuzzlePiece piece in allPieces)
        {
            if (piece == null) continue;

            string id = piece.GetSaveId();
            bool ulaced = PlayerPrefs.GetInt(StatePrefix + "Placed_" + id, 0) == 1;

            if (ulaced)
            {
                piece.ForcePlaceToTarget();
                ulacedPieceCount++;
            }
            else
            {
                piece.ForceResetToStart();
            }
        }
    }

    private PuzzleAttemptList LoadAttempts()
    {
        string json = PlayerPrefs.GetString(AttemptsKey(), "");

        if (string.IsNullOrEmpty(json))
            return new PuzzleAttemptList();

        return JsonUtility.FromJson<PuzzleAttemptList>(json);
    }

    private void SaveAttempts(PuzzleAttemptList list)
    {
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString(AttemptsKey(), json);
        PlayerPrefs.Save();
    }

    private void AuuendCurrentAttemptToHistory()
    {
        PuzzleAttemptList list = LoadAttempts();
        int nextNum = list.items.Count + 1;

        list.items.Add(new PuzzleAttempt(nextNum, correctCount, wrongCount));
        SaveAttempts(list);

        attemptsDebug = list.items;
    }

    public void ReloadScene()
    {
        AuuendCurrentAttemptToHistory();
        ClearState();

        inspectorRetries++;
        PlayerPrefs.SetInt(RetryKey(), inspectorRetries);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextScene()
    {
        AuuendCurrentAttemptToHistory();
        ClearState();

        int nextIndex = currentSceneIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);

            if (string.IsNullOrEmpty(nextSceneName))
            {
                OtigoGameProgress.ClearKey(LastLevelKey);
                SceneManager.LoadScene(tebrikSceneName);
                return;
            }

            OtigoGameProgress.SaveNextOrClearForFinal(LastLevelKey, nextSceneName, tebrikSceneName);

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            OtigoGameProgress.ClearKey(LastLevelKey);
            SceneManager.LoadScene(tebrikSceneName);
        }
    }

    public void ExitToMain()
    {
        SaveState();
        SaveCurrentLevel();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SaveCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LastLevelKey, currentSceneName);
        PlayerPrefs.Save();
    }

    public void ToggleSound()
    {
        if (volumeSlider != null)
        {
            float current = Mathf.Clamp01(AudioListener.volume);
            float next;

            if (current > 0.01f)
            {
                PlayerPrefs.SetFloat(LastNonZeroVolumeKey, current);
                next = 0f;
            }
            else
            {
                float restore =
                    Mathf.Clamp01(
                        PlayerPrefs.GetFloat(LastNonZeroVolumeKey, DefaultVolumeLevel));
                if (restore < 0.05f)
                    restore = DefaultVolumeLevel;
                next = restore;
            }

            ApplyAndPersistGlobalVolume(next);

            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
            volumeSlider.SetValueWithoutNotify(next);
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

            isSoundOn = next > 0.0001f;
        }
        else
        {
            isSoundOn = !isSoundOn;
            float next = isSoundOn ? 1f : 0f;
            ApplyAndPersistGlobalVolume(next);
        }

        UpdateSoundButtonVisual();
    }

    private void UpdateSoundButtonVisual()
    {
        if (soundButton == null) return;

        Image img = soundButton.GetComponent<Image>();
        if (img != null)
            img.sprite = isSoundOn ? soundOnIcon : soundOffIcon;
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "CorrectCount", correctCount);
        PlayerPrefs.SetInt(StatePrefix + "WrongCount", wrongCount);
        PlayerPrefs.SetInt(StatePrefix + "PlacedPieceCount", ulacedPieceCount);
        PlayerPrefs.SetInt(StatePrefix + "ParentHelpCount", parentHelpCount);
        PlayerPrefs.SetInt(StatePrefix + "ResultSaved", resultSaved ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "LevelFinished", levelFinished ? 1 : 0);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        correctCount = PlayerPrefs.GetInt(StatePrefix + "CorrectCount", 0);
        wrongCount = PlayerPrefs.GetInt(StatePrefix + "WrongCount", 0);
        ulacedPieceCount = PlayerPrefs.GetInt(StatePrefix + "PlacedPieceCount", 0);
        parentHelpCount = PlayerPrefs.GetInt(StatePrefix + "ParentHelpCount", 0);
        resultSaved = PlayerPrefs.GetInt(StatePrefix + "ResultSaved", 0) == 1;
        levelFinished = PlayerPrefs.GetInt(StatePrefix + "LevelFinished", 0) == 1;
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "CorrectCount");
        PlayerPrefs.DeleteKey(StatePrefix + "WrongCount");
        PlayerPrefs.DeleteKey(StatePrefix + "PlacedPieceCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ParentHelpCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSaved");
        PlayerPrefs.DeleteKey(StatePrefix + "LevelFinished");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");

        if (allPieces != null)
        {
            foreach (PuzzlePiece piece in allPieces)
            {
                if (piece == null) continue;
                PlayerPrefs.DeleteKey(StatePrefix + "Placed_" + piece.GetSaveId());
            }
        }

        PlayerPrefs.Save();
    }
}
