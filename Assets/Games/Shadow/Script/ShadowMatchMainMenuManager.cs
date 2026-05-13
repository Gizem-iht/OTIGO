using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchMainMenuManager : MonoBehaviour
{
    private const string LastLevelKey = "LastPlayedLevel";

    [Header("Nasıl Oynanır Ekranı")]
    public GameObject howToPlayPanel;
    public GameObject[] mainMenuButtons;

    [Header("Sesler")]
    public AudioSource narrationSource;
    public AudioClip howToPlayClip;
    public AudioSource backgroundMusic;

    [Header("Varsayılan Level (ilk girişte)")]
    public string defaultLevelSceneName = "ShadowMatch_Level1";

    private void Start()
    {
        ShadowMatchSoundToggle.SyncAudioListenerFromPlayerPrefs();

        SetMainMenuButtonsActive(false);

        if (backgroundMusic != null)
            backgroundMusic.Stop();

        ShowHowToPlay();
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);

        if (narrationSource != null && howToPlayClip != null)
        {
            narrationSource.clip = howToPlayClip;
            narrationSource.Play();
        }
    }

    public void CloseHowToPlay()
    {
        if (narrationSource != null && narrationSource.isPlaying)
            narrationSource.Stop();

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        SetMainMenuButtonsActive(true);

        if (backgroundMusic != null)
            backgroundMusic.Play();
    }

    private void SetMainMenuButtonsActive(bool active)
    {
        if (mainMenuButtons == null) return;

        foreach (var btn in mainMenuButtons)
        {
            if (btn != null)
                btn.SetActive(active);
        }
    }

    public void LoadGame()
    {
        string lastLevel = OtigoGameProgress.GetResumeScene(LastLevelKey, defaultLevelSceneName);
        SceneManager.LoadScene(lastLevel);
    }

    public void QuitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            1f);
    }
}
