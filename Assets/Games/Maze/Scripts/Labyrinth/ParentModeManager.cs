using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ParentModeManager : MonoBehaviour
{
    public static ParentModeManager Instance { get; private set; }

    [Header("UI")]
    public Button lockButton;
    public GameObject patternPanel;
    public Button closeParentModeButton;
    public GameObject parentModeActiveIndicator;

    [Header("Pattern Lock")]
    public PatternLockController patternLockController;

    [Header("State")]
    [SerializeField] private bool isParentModeActive = false;
    [SerializeField] private int parentHelpCount = 0;

    [Header("Auto Close")]
    public float patternPanelAutoCloseSeconds = 10f;

    [Header("Controlled Audio")]
    public AudioSource controlledVoiceSource;

    public bool IsParentModeActive => isParentModeActive;
    public int ParentHelpCount => parentHelpCount;
    public bool IsPatternPanelOpen => patternPanel != null && patternPanel.activeSelf;

    private Coroutine autoCloseCoroutine;

    private bool wasVoicePlayingBeforePattern = false;
    private float pausedVoiceTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (lockButton != null)
            lockButton.onClick.AddListener(OpenPatternPanel);

        if (closeParentModeButton != null)
            closeParentModeButton.onClick.AddListener(CloseParentMode);

        if (patternPanel != null)
            patternPanel.SetActive(false);

        UpdateUI();

        if (patternLockController != null)
        {
            patternLockController.OnPatternSuccess = ActivateParentMode;
            patternLockController.OnPatternFail = HandleWrongPattern;
        }
    }

    public void OpenPatternPanel()
    {
        PauseControlledVoiceIfNeeded();

        if (patternPanel != null)
            patternPanel.SetActive(true);

        if (patternLockController != null)
            patternLockController.ResetPattern();

        StartAutoCloseTimer();
    }

    public void ClosePatternPanelOnly()
    {
        StopAutoCloseTimer();

        if (patternPanel != null)
            patternPanel.SetActive(false);

        if (patternLockController != null)
            patternLockController.ResetPattern();

        ResumeControlledVoiceIfNeeded();
    }

    public void ActivateParentMode()
    {
        isParentModeActive = true;

        StopAutoCloseTimer();

        if (patternPanel != null)
            patternPanel.SetActive(false);

        if (patternLockController != null)
            patternLockController.ResetPattern();

        ResumeControlledVoiceIfNeeded();
        UpdateUI();

        Debug.Log("Veli modu AÇILDI.");
    }

    public void CloseParentMode()
    {
        isParentModeActive = false;
        UpdateUI();
        Debug.Log("Veli modu KAPANDI.");
    }

    private void HandleWrongPattern()
    {
        Debug.Log("Yanlış desen girildi.");
        // Panel açık kalır, timer işlemeye devam eder.
        // Ses durmuş halde kalır; panel kapanınca veya doğru desen girilince devam eder.
    }

    private void UpdateUI()
    {
        if (parentModeActiveIndicator != null)
            parentModeActiveIndicator.SetActive(isParentModeActive);

        if (closeParentModeButton != null)
            closeParentModeButton.gameObject.SetActive(isParentModeActive);

        if (lockButton != null)
            lockButton.gameObject.SetActive(!isParentModeActive);
    }

    public void RegisterParentHelp()
    {
        if (!isParentModeActive) return;

        parentHelpCount++;
        Debug.Log("Parent help count arttı: " + parentHelpCount);
    }

    public void ResetParentHelpCount()
    {
        parentHelpCount = 0;
    }

    private void StartAutoCloseTimer()
    {
        StopAutoCloseTimer();
        autoCloseCoroutine = StartCoroutine(AutoClosePatternPanel());
    }

    private void StopAutoCloseTimer()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }

    private IEnumerator AutoClosePatternPanel()
    {
        yield return new WaitForSeconds(patternPanelAutoCloseSeconds);

        if (patternPanel != null && patternPanel.activeSelf && !isParentModeActive)
        {
            Debug.Log("Pattern panel süre dolduğu için otomatik kapatıldı.");
            ClosePatternPanelOnly();
        }

        autoCloseCoroutine = null;
    }

    private void PauseControlledVoiceIfNeeded()
    {
        if (controlledVoiceSource == null)
            return;

        if (controlledVoiceSource.isPlaying)
        {
            wasVoicePlayingBeforePattern = true;
            pausedVoiceTime = controlledVoiceSource.time;
            controlledVoiceSource.Pause();

            Debug.Log("Ses duraklatıldı. Kaldığı süre: " + pausedVoiceTime);
        }
        else
        {
            wasVoicePlayingBeforePattern = false;
        }
    }

    private void ResumeControlledVoiceIfNeeded()
    {
        if (controlledVoiceSource == null)
            return;

        if (wasVoicePlayingBeforePattern)
        {
            controlledVoiceSource.time = pausedVoiceTime;
            controlledVoiceSource.UnPause();
            wasVoicePlayingBeforePattern = false;

            Debug.Log("Ses kaldığı yerden devam etti.");
        }
    }
}
