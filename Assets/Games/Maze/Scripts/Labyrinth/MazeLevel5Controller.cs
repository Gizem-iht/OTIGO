using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MazeLevel5Controller : MonoBehaviour
{
    [Header("UI")]
    public GameObject levelCompletePanel;
    public Button replayButton;
    public Button nextButton;
    public Button exitButton;
    public Slider volumeSlider;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MazeMainMenu";
    public string nextLevelSceneName = "Maze_Level6";

    [Header("Player (Girl)")]
    public GirlDrag girl;
    public Transform girlStartPoint;

    [Header("Walls")]
    public LayerMask wallLayers;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioClip wallClip;
    public AudioClip tebrikClip;

    [Header("Win Pop Image")]
    public Transform winImage;
    public float popDuration = 0.45f;
    public float popScale = 1.2f;

    [Header("Wrong Reset")]
    public float wrongLockSeconds = 0.10f;

    [Header("OTIGO API")]
    public int activityId = 5;
    public int levelPlayed = 5;
    public int totalTargetCount = 1;
    public int parentHelpCount = 0;

    private const string VolumeKey = "Maze_GameVolume";

    private bool finished = false;
    private bool inPenalty = false;
    private bool resultSent = false;
    private bool gameplayStarted = true;

    private int mistakesMade = 0;
    private float activePlayTime = 0f;

    private string StatePrefix
    {
        get { return "Maze_State_" + SceneManager.GetActiveScene().name + "_"; }
    }

    private void Start()
    {
        if (!MazeOtigoSessionTracker.HasSession())
            MazeOtigoSessionTracker.BeginSession(activityId);

        finished = false;
        inPenalty = false;
        resultSent = false;
        gameplayStarted = true;

        mistakesMade = 0;
        parentHelpCount = 0;
        activePlayTime = 0f;

        if (girl != null)
        {
            girl.controller = this;
            girl.wallLayers = wallLayers;
            girl.Unlock();
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplayLevel);
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextLevel);
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
        return voiceSource != null && voiceSource.isPlaying;
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void RestorePlayingLevel()
    {
        gameplayStarted = true;

        if (girl != null)
        {
            girl.wallLayers = wallLayers;
            girl.Unlock();
        }
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (girl != null)
            girl.Lock();

        if (winImage != null)
        {
            winImage.gameObject.SetActive(true);
            winImage.localScale = Vector3.one * popScale;
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    public void OnHitWall()
    {
        if (finished || inPenalty) return;

        mistakesMade++;
        inPenalty = true;
        SaveState();

        if (voiceSource != null && wallClip != null)
            voiceSource.PlayOneShot(wallClip);

        StartCoroutine(ResetGirlAfterDelay());
    }

    private IEnumerator ResetGirlAfterDelay()
    {
        if (girl == null || girlStartPoint == null)
        {
            inPenalty = false;
            yield break;
        }

        girl.Lock();
        yield return new WaitForSeconds(wrongLockSeconds);

        Rigidbody2D rb = girl.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = girlStartPoint.position;
        }
        else
        {
            girl.transform.position = girlStartPoint.position;
        }

        girl.Unlock();
        inPenalty = false;
        SaveState();
    }

    public void OnReachedGoal()
    {
        if (finished) return;

        finished = true;
        inPenalty = false;
        gameplayStarted = false;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
            AddParentHelp();

        if (girl != null)
        {
            girl.Lock();

            Rigidbody2D rb = girl.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
            }
        }

        SaveLevelResult();
        SaveState();

        if (voiceSource != null && tebrikClip != null)
            voiceSource.PlayOneShot(tebrikClip);

        if (winImage != null)
            StartCoroutine(PopWinImage());

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
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

    public void AddParentHelp()
    {
        parentHelpCount++;

        if (ParentModeManager.Instance != null)
            ParentModeManager.Instance.RegisterParentHelp();

        MazeOtigoSessionTracker.AddParentHelpForLevel(levelPlayed);

        SaveState();

        Debug.Log("Maze Level 5 parentHelpCount arttı: " + parentHelpCount);
    }

    public void ReplayLevel()
    {
        ClearState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        ClearState();
        OtigoGameProgress.SaveNextOrClearForFinal("MazeLastLevel", nextLevelSceneName, "Maze_TebrikScene");
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

        if (girl != null)
        {
            Vector3 pos = girl.transform.position;

            PlayerPrefs.SetFloat(StatePrefix + "GirlX", pos.x);
            PlayerPrefs.SetFloat(StatePrefix + "GirlY", pos.y);
            PlayerPrefs.SetFloat(StatePrefix + "GirlZ", pos.z);
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

        if (girl != null)
        {
            float x = PlayerPrefs.GetFloat(StatePrefix + "GirlX", girl.transform.position.x);
            float y = PlayerPrefs.GetFloat(StatePrefix + "GirlY", girl.transform.position.y);
            float z = PlayerPrefs.GetFloat(StatePrefix + "GirlZ", girl.transform.position.z);

            girl.transform.position = new Vector3(x, y, z);

            Rigidbody2D rb = girl.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.isKinematic = finished;
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
        PlayerPrefs.DeleteKey(StatePrefix + "GirlX");
        PlayerPrefs.DeleteKey(StatePrefix + "GirlY");
        PlayerPrefs.DeleteKey(StatePrefix + "GirlZ");

        PlayerPrefs.Save();
    }
}
