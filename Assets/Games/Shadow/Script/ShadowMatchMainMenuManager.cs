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
        if (!PlayerPrefs.HasKey(LastLevelKey))
        {
            PlayerPrefs.SetString(LastLevelKey, defaultLevelSceneName);
            PlayerPrefs.Save();
        }

        string lastLevel = PlayerPrefs.GetString(LastLevelKey, defaultLevelSceneName);
        SceneManager.LoadScene(lastLevel);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}