using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class MazeLevel4Controller : MonoBehaviour
{
    [Header("UI")]
    public GameObject levelCompletePanel;
    public Button replayButton;
    public Button nextButton;
    public Button exitButton;
    public Slider volumeSlider;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MazeMainMenu";
    public string nextLevelSceneName = "Maze_Level5";

    [Header("Intro Voice (Before play)")]
    public CarrotDrag carrot;
    public AudioSource introSource;
    public AudioClip introClip;
    public float introExtraDelay = 0.05f;

    [Header("Wrong Hit (Camel/Wall) - DAAT + Reset")]
    public AudioSource sfxSource;
    public AudioClip daatClip;
    public Transform carrotStartPoint;
    public float wrongLockSeconds = 0.20f;

    [Header("Camel Voice (Optional)")]
    public AudioClip camelVoiceClip;

    [Header("Win Image Pop (Optional)")]
    public Transform winImage;
    public float winPopDuration = 0.5f;
    public float winPopScale = 1.2f;

    [Header("Tebrik Voice")]
    public AudioSource voiceSource;
    public AudioClip tebrikClip;
    public float afterTebrikDelay = 1.0f;

    [Header("Instruction Voice (Optional)")]
    public AudioClip instructionClip;

    [Header("Video Win Overlay (Optional)")]
    public GameObject videoOverlay;
    public RectTransform videoContainer;
    public VideoPlayer videoPlayer;
    public Button videoContinueButton;

    [Header("Video Pop Animation")]
    public float videoPopDuration = 0.45f;
    public float videoStartScale = 0.2f;
    public float videoEndScale = 1f;

    [Header("OTIGO API")]
    public int activityId = 5;
    public int levelPlayed = 4;
    public int totalTargetCount = 1;
    public int parentHelpCount = 0;

    private const string VolumeKey = "Maze_GameVolume";

    private bool finished = false;
    private bool inPenalty = false;
    private bool resultSent = false;
    private bool gameplayStarted = false;

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
        gameplayStarted = false;

        mistakesMade = 0;
        parentHelpCount = 0;
        activePlayTime = 0f;

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

        if (videoOverlay != null)
            videoOverlay.SetActive(false);

        if (videoContainer != null)
            videoContainer.localScale = Vector3.one * videoStartScale;

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
        }

        if (videoContinueButton != null)
        {
            videoContinueButton.gameObject.SetActive(false);
            videoContinueButton.onClick.RemoveAllListeners();
            videoContinueButton.onClick.AddListener(OnVideoContinuePressed);
        }

        if (winImage != null)
        {
            winImage.gameObject.SetActive(false);
            winImage.localScale = Vector3.zero;
        }

        if (carrot != null)
        {
            carrot.controller = this;
            carrot.Lock();
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
            StartCoroutine(PlayIntroAndUnlock());
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
        return (introSource != null && introSource.isPlaying) ||
               (voiceSource != null && voiceSource.isPlaying) ||
               (sfxSource != null && sfxSource.isPlaying);
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private IEnumerator PlayIntroAndUnlock()
    {
        if (carrot != null)
            carrot.Lock();

        if (introSource != null && introClip != null)
        {
            introSource.PlayOneShot(introClip);
            yield return new WaitForSeconds(introClip.length + introExtraDelay);
        }

        gameplayStarted = true;

        if (carrot != null)
            carrot.Unlock();
    }

    private void RestorePlayingLevel()
    {
        gameplayStarted = true;

        if (carrot != null)
            carrot.Unlock();
    }

    private void RestoreCompletedLevel()
    {
        gameplayStarted = false;

        if (carrot != null)
            carrot.Lock();

        if (winImage != null)
        {
            winImage.gameObject.SetActive(true);
            winImage.localScale = Vector3.one * winPopScale;
        }

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    public void OnReachedGoal(Transform focusTarget = null)
    {
        if (finished) return;

        finished = true;
        inPenalty = false;
        gameplayStarted = false;

        if (ParentModeManager.Instance != null && ParentModeManager.Instance.IsParentModeActive)
            AddParentHelp();

        if (carrot != null)
        {
            carrot.Lock();

            Rigidbody2D rb = carrot.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
            }
        }

        SaveLevelResult();
        SaveState();

        if (replayButton != null)
            replayButton.gameObject.SetActive(true);

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        if (winImage != null)
        {
            winImage.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateWinImagePop());
        }

        if (voiceSource != null && tebrikClip != null)
            voiceSource.PlayOneShot(tebrikClip);

        yield return new WaitForSeconds(afterTebrikDelay);

        if (voiceSource != null && instructionClip != null)
            voiceSource.PlayOneShot(instructionClip);

        if (videoOverlay != null && videoPlayer != null)
            yield return StartCoroutine(PlayVideoSequence());
        else if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);
    }

    private IEnumerator AnimateWinImagePop()
    {
        if (Camera.main == null || winImage == null) yield break;

        Vector3 centerWorld = Camera.main.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, Mathf.Abs(Camera.main.transform.position.z))
        );
        centerWorld.z = 0f;
        winImage.position = centerWorld;

        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one * winPopScale;
        winImage.localScale = startScale;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, winPopDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            winImage.localScale = Vector3.Lerp(startScale, targetScale, smooth);
            yield return null;
        }

        winImage.localScale = targetScale;
    }

    public void OnHitCamel()
    {
        if (finished || inPenalty) return;

        mistakesMade++;
        inPenalty = true;
        SaveState();

        if (camelVoiceClip != null)
        {
            if (voiceSource != null)
                voiceSource.PlayOneShot(camelVoiceClip);
            else if (sfxSource != null)
                sfxSource.PlayOneShot(camelVoiceClip);
        }

        StartCoroutine(ResetCarrotAfterDelay());
    }

    public void OnHitWall()
    {
        if (finished || inPenalty) return;

        mistakesMade++;
        inPenalty = true;
        SaveState();

        if (daatClip != null)
        {
            if (sfxSource != null)
                sfxSource.PlayOneShot(daatClip);
            else if (voiceSource != null)
                voiceSource.PlayOneShot(daatClip);
        }

        StartCoroutine(ResetCarrotAfterDelay());
    }

    private IEnumerator ResetCarrotAfterDelay()
    {
        if (carrot == null || carrotStartPoint == null)
        {
            inPenalty = false;
            yield break;
        }

        Rigidbody2D rb = carrot.GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.isKinematic = false;

        carrot.Lock();

        yield return new WaitForSeconds(wrongLockSeconds);

        if (!finished)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = carrotStartPoint.position;
            }
            else
            {
                carrot.transform.position = carrotStartPoint.position;
            }
        }

        carrot.Unlock();
        inPenalty = false;
        SaveState();
    }

    private IEnumerator PlayVideoSequence()
    {
        if (videoOverlay != null)
            videoOverlay.SetActive(true);

        if (videoContinueButton != null)
            videoContinueButton.gameObject.SetActive(false);

        if (videoContainer != null)
        {
            videoContainer.localScale = Vector3.one * videoStartScale;

            float t = 0f;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, videoPopDuration);
                float scale = Mathf.SmoothStep(videoStartScale, videoEndScale, t);
                videoContainer.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        bool prepared = false;
        void OnPrepared(VideoPlayer vu) => prepared = true;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();

        while (!prepared)
            yield return null;

        videoPlayer.prepareCompleted -= OnPrepared;

        videoPlayer.Play();

        while (videoPlayer.isPlaying)
            yield return null;

        if (videoContinueButton != null)
            videoContinueButton.gameObject.SetActive(true);
    }

    public void OnVideoContinuePressed()
    {
        if (videoOverlay != null)
            videoOverlay.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
        }

        if (videoContinueButton != null)
            videoContinueButton.gameObject.SetActive(false);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        SaveState();
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

        Debug.Log("Maze Level 4 parentHelpCount arttı: " + parentHelpCount);
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

        if (carrot != null)
        {
            Vector3 pos = carrot.transform.position;

            PlayerPrefs.SetFloat(StatePrefix + "CarrotX", pos.x);
            PlayerPrefs.SetFloat(StatePrefix + "CarrotY", pos.y);
            PlayerPrefs.SetFloat(StatePrefix + "CarrotZ", pos.z);
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

        if (carrot != null)
        {
            float x = PlayerPrefs.GetFloat(StatePrefix + "CarrotX", carrot.transform.position.x);
            float y = PlayerPrefs.GetFloat(StatePrefix + "CarrotY", carrot.transform.position.y);
            float z = PlayerPrefs.GetFloat(StatePrefix + "CarrotZ", carrot.transform.position.z);

            carrot.transform.position = new Vector3(x, y, z);

            Rigidbody2D rb = carrot.GetComponent<Rigidbody2D>();

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
        PlayerPrefs.DeleteKey(StatePrefix + "CarrotX");
        PlayerPrefs.DeleteKey(StatePrefix + "CarrotY");
        PlayerPrefs.DeleteKey(StatePrefix + "CarrotZ");

        PlayerPrefs.Save();
    }
}
