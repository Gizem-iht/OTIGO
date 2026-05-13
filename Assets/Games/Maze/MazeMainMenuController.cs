using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeMainMenuController : MonoBehaviour
{
    [Header("Ilk acilacak level")]
    public string firstLevelSceneName = "Maze_Level1";

    [Header("Kaydedilen level key")]
    public string lastLevelKey = "MazeLastLevel";

    [Header("Menu scene adi")]
    public string menuSceneName = "MazeMainMenu";

    public void StartGame()
    {
        string targetScene = OtigoGameProgress.GetResumeScene(lastLevelKey, firstLevelSceneName);

        if (targetScene == menuSceneName)
            targetScene = firstLevelSceneName;

        Debug.Log("Maze StartGame -> acilacak level: " + targetScene);
        SceneManager.LoadScene(targetScene);
    }

    public void ExitGame()
    {
        Debug.Log("Oyun kapatiliyor.");

        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            },
            1f);
    }

    public void ClearSavedProgress()
    {
        OtigoGameProgress.ClearGame("Maze");
        Debug.Log("Kaydedilen progress silindi.");
    }
}
