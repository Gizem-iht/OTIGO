using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchMainMenu : MonoBehaviour
{
    private const string LastLevelKey = "LastPlayedLevel";
    private const string DefaultLevelSceneName = "ShadowMatch_Level1";

    public void PlayGame()
    {
        string lastLevel = PlayerPrefs.GetString(LastLevelKey, DefaultLevelSceneName);
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