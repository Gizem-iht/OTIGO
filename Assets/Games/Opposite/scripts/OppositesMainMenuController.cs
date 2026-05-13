using UnityEngine;
using UnityEngine.SceneManagement;

public class OppositesMainMenuController : MonoBehaviour
{
    private const string LastLevelKey = "Opposites_LastLevelSceneName";

    [Header("İlk açılacak level")]
    public string defaultFirstLevelSceneName = "Opposites_Level1";

    public void StartGame()
    {
        string targetScene = OtigoGameProgress.GetResumeScene(LastLevelKey, defaultFirstLevelSceneName);

        Debug.Log("StartGame -> Açılacak scene: " + targetScene);
        SceneManager.LoadScene(targetScene);
    }

    public void ExitGame()
    {
        Debug.Log("Oyundan çıkılıyor...");

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

    public void ResetProgress()
    {
        OtigoGameProgress.ClearGame("Opposites");

        Debug.Log("Opposites progress sıfırlandı.");
    }
}
