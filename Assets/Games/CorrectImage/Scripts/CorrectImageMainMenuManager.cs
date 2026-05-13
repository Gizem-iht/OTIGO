using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CorrectImageMainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string categorySelectSceneName = "CorrectImage_CategorySelect";
    public string gameLevelSceneName = "CorrectImage_CorrectImage_GameLevel";

    private const string SELECTED_CATEGORY_KEY = "CorrectImage_SelectedCategory";
    private const string CURRENT_LEVEL_KEY = "CorrectImage_CurrentLevel";

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void StartGame()
    {
        bool hasCategory = !string.IsNullOrEmpty(PlayerPrefs.GetString(SELECTED_CATEGORY_KEY, ""));
        int savedLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 0);

        if (hasCategory && savedLevel > 0 && Application.CanStreamedLevelBeLoaded(gameLevelSceneName))
            SceneManager.LoadScene(gameLevelSceneName);
        else
            SceneManager.LoadScene(categorySelectSceneName);
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
