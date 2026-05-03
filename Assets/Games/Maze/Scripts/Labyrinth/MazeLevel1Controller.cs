using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MazeLevel1Controller : MonoBehaviour
{
    [Header("UI")]
    public GameObject levelCompletePanel;
    public Button replayButton;
    public Button nextButton;
    public Button exitButton;
    public Slider volumeSlider;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MazeMainMenu";
    public string nextLevelSceneName = "Maze_Level2";

    [Header("Chick")]
    public ChickDrag chick;
    public Transform chickStartPoint;

    [Header("Intro Voice")]
    public AudioSource introSource;
    public AudioClip introClip;
    public float introExtraDelay = 0.2f;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioClip wallClip;
    public AudioClip tebrikClip;

    [Header("Instruction Voice")]
    public AudioClip instructionClip;

    [Header("Wall Reset")]
    public float wrongLockSeconds = 0.15f;

    [Header("Win Pop Image")]
    public Transform winImage;
    public float popDuration = 0.45f;
    public float popScale = 1.2f;

    [Header("Button Glow")]
    public float buttonGlowDuration = 1.0f;
    public float buttonGlowScale = 1.12f;

    [Header("OTIGO API")]
    public int activityId = 5;
    public int levelPlayed = 1;
    public int totalTargetCount = 1;
    public int parentHelpCount = 0;

    private const string VolumeKey = "Maze_GameVolume";

    private bool finished = false;
    private bool inPenalty = false;
    private bool resultSent = false;
    private bool gameplayStarted = false;

    private int mistakesMade = 0;
    private float activePlayTime = 0f;

    private Coroutine replayGlowRoutine;
    private Coroutine nextGlowRoutine;

    private Vector3 replayOriginalScale;
    private Vector3 nextOriginalScale;

    private string StatePrefix
    {
        get
        {
            return "Maze_State_" + SceneManager.GetActiveScene().name + "_";
        }
    }

    private void Start()
    {
        if (!MazeOtigoSessionTracker.HasSession())
            MazeOtigoSessionTracker.BeginSession(activityId);

        finished = false;
        inPenalty = false;
        resultSent = false;
        gameplayStarted = false;

        mistakesMade = 0;
        parentHelpCount = 0;
        activePlayTime = 0f;

        if (chick != null)
        {
            chick.controller = this;
            chick.Lock();
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayLevel);
            replayOriginalScale = replayButton.transform.localScale;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextLevel);
            nextOriginalScale = nextButton.transform.localScale;
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(GoToMenu);
        }

        if (volumeSlider != null)
        {
            float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = saved;
            volumeSlider.value = saved;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (winImage != null)
        {
            winImage.gameObject.SetActive(false);
            winImage.localScale = Vector3.zero;
        }

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
            StartCoroutine(PlayIntroVoice());
        }
    }

    private void Update()
    {
        bool parentModeActive =
            ParentModeManager.Instance != null &&
            ParentModeManager.Instance.IsParentModeActive;

        if (gameplayStarted && !finished && !parentModeActive && !IsInstructionAudioPlaying())
        {
            activePlayTime += Time.deltaTime;
        }
    }

    private bool IsInstructionAudioPlaying()
    {
        return (introSource != null && introSource.isPlaying) ||
               (voiceSource != null && voiceSource.isPlaying);
    }

    private IEnumerator PlayIntroVoice()
    {
        if (introSource != null && introClip != null)
        {
            introSource.PlayOneShot(introClip);
            yield return new WaitForSeconds(introClip.length + introExtraDelay);
        }

        gameplayStarted = true;

        if (chick != null)
            chick.Unlock();
    }

    private void RestorePlayingLevel()
    {
        gameplayStarted = true;

        if (chick != null)
            chick.Unlock();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (chick != null)
            chick.Lock();

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        StartButtonGlow();
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    public void OnReachedChicken()
    {
        if (finished) return;

        finished = true;
        inPenalty = false;
        gameplayStarted = false;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
        {
            AddParentHelp();
        }

        if (chick != null)
        {
            chick.Lock();

            Rigidbody2D rb = chick.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
            }
        }

        SaveLevelResult();
        SaveState();

        StartCoroutine(WinSequence());
    }

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        MazeOtigoSessionTracker.AddParentHelpForLevel(levelPlayed);

        Debug.Log("Maze Level 1 parentHelpCount arttı: " + parentHelpCount);
    }

    private void SaveLevelResult()
    {
        if (resultSent) return;
        resultSent = true;

        int durationSeconds = Mathf.RoundToInt(activePlayTime);

        MazeOtigoSessionTracker.AddOrUpdateLevelResult(
            levelPlayed,
            durationSeconds,
            mistakesMade
        );
    }

    private IEnumerator WinSequence()
    {
        yield return StartCoroutine(PopWinImage());

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (voiceSource != null && tebrikClip != null)
        {
            voiceSource.PlayOneShot(tebrikClip);
            yield return new WaitForSeconds(tebrikClip.length);
        }

        if (voiceSource != null && instructionClip != null)
        {
            voiceSource.PlayOneShot(instructionClip);
            yield return new WaitForSeconds(instructionClip.length * 0.3f);
        }

        StartButtonGlow();
    }

    private IEnumerator PopWinImage()
    {
        if (winImage == null) yield break;

        winImage.gameObject.SetActive(true);

        if (Camera.main != null)
        {
            Vector3 center = Camera.main.ViewportToWorldPoint(
                new Vector3(0.5f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))
            );
            center.z = 0f;
            winImage.position = center;
        }

        Vector3 start = Vector3.zero;
        Vector3 target = Vector3.one * popScale;
        winImage.localScale = start;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, popDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            winImage.localScale = Vector3.Lerp(start, target, smooth);
            yield return null;
        }

        winImage.localScale = target;
    }

    private void StartButtonGlow()
    {
        if (replayButton != null)
        {
            if (replayGlowRoutine != null)
                StopCoroutine(replayGlowRoutine);

            replayButton.transform.localScale = replayOriginalScale;
            replayGlowRoutine = StartCoroutine(GlowButton(replayButton.transform, replayOriginalScale));
        }

        if (nextButton != null)
        {
            if (nextGlowRoutine != null)
                StopCoroutine(nextGlowRoutine);

            nextButton.transform.localScale = nextOriginalScale;
            nextGlowRoutine = StartCoroutine(GlowButton(nextButton.transform, nextOriginalScale));
        }
    }

    private IEnumerator GlowButton(Transform buttonTransform, Vector3 baseScale)
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, buttonGlowDuration * 0.5f);
                float curve = Mathf.SmoothStep(0f, 1f, t);
                buttonTransform.localScale = Vector3.Lerp(baseScale, baseScale * buttonGlowScale, curve);
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, buttonGlowDuration * 0.5f);
                float curve = Mathf.SmoothStep(0f, 1f, t);
                buttonTransform.localScale = Vector3.Lerp(baseScale * buttonGlowScale, baseScale, curve);
                yield return null;
            }
        }
    }

    public void OnHitWall()
    {
        if (finished || inPenalty) return;

        mistakesMade++;
        inPenalty = true;

        SaveState();

        if (voiceSource != null && wallClip != null)
            voiceSource.PlayOneShot(wallClip);

        StartCoroutine(ResetChickAfterDelay());
    }

    private IEnumerator ResetChickAfterDelay()
    {
        if (chick == null || chickStartPoint == null)
        {
            inPenalty = false;
            yield break;
        }

        chick.Lock();
        yield return new WaitForSeconds(wrongLockSeconds);

        Rigidbody2D rb = chick.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = chickStartPoint.position;
        }
        else
        {
            chick.transform.position = chickStartPoint.position;
        }

        chick.Unlock();
        inPenalty = false;

        SaveState();
    }

    public void ReplayLevel()
    {
        ClearState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        ClearState();
        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void GoToMenu()
    {
        SaveState();

        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("MazeLastLevel", currentScene);
        PlayerPrefs.Save();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(StatePrefix + "HasState", 1);
        PlayerPrefs.SetInt(StatePrefix + "Finished", finished ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "ResultSent", resultSent ? 1 : 0);
        PlayerPrefs.SetInt(StatePrefix + "MistakesMade", mistakesMade);
        PlayerPrefs.SetInt(StatePrefix + "ParentHelpCount", parentHelpCount);
        PlayerPrefs.SetFloat(StatePrefix + "ActivePlayTime", activePlayTime);

        if (chick != null)
        {
            Vector3 pos = chick.transform.position;
            PlayerPrefs.SetFloat(StatePrefix + "ChickX", pos.x);
            PlayerPrefs.SetFloat(StatePrefix + "ChickY", pos.y);
            PlayerPrefs.SetFloat(StatePrefix + "ChickZ", pos.z);
        }

        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        finished = PlayerPrefs.GetInt(StatePrefix + "Finished", 0) == 1;
        resultSent = PlayerPrefs.GetInt(StatePrefix + "ResultSent", 0) == 1;
        mistakesMade = PlayerPrefs.GetInt(StatePrefix + "MistakesMade", 0);
        parentHelpCount = PlayerPrefs.GetInt(StatePrefix + "ParentHelpCount", 0);
        activePlayTime = PlayerPrefs.GetFloat(StatePrefix + "ActivePlayTime", 0f);

        if (chick != null)
        {
            float x = PlayerPrefs.GetFloat(StatePrefix + "ChickX", chick.transform.position.x);
            float y = PlayerPrefs.GetFloat(StatePrefix + "ChickY", chick.transform.position.y);
            float z = PlayerPrefs.GetFloat(StatePrefix + "ChickZ", chick.transform.position.z);

            chick.transform.position = new Vector3(x, y, z);

            Rigidbody2D rb = chick.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = new Vector2(x, y);
            }
        }
    }

    private bool HasSavedState()
    {
        return PlayerPrefs.GetInt(StatePrefix + "HasState", 0) == 1;
    }

    private void ClearState()
    {
        PlayerPrefs.DeleteKey(StatePrefix + "HasState");
        PlayerPrefs.DeleteKey(StatePrefix + "Finished");
        PlayerPrefs.DeleteKey(StatePrefix + "ResultSent");
        PlayerPrefs.DeleteKey(StatePrefix + "MistakesMade");
        PlayerPrefs.DeleteKey(StatePrefix + "ParentHelpCount");
        PlayerPrefs.DeleteKey(StatePrefix + "ActivePlayTime");
        PlayerPrefs.DeleteKey(StatePrefix + "ChickX");
        PlayerPrefs.DeleteKey(StatePrefix + "ChickY");
        PlayerPrefs.DeleteKey(StatePrefix + "ChickZ");
        PlayerPrefs.Save();
    }
}
