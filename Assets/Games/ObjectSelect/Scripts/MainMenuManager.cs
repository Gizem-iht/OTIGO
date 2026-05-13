using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string mainLevelSceneName = "ObjectSelect_Level1";

    private const string LAST_LEVEL_KEY = "ObjectSelect_LastLevelSceneName";

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
        SceneManager.LoadScene(OtigoGameProgress.GetResumeScene(LAST_LEVEL_KEY, mainLevelSceneName));
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
