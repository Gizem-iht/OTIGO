using UnityEngine;

public class ShadowMatchExitButton : MonoBehaviour
{
    public void QuitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                if (sceneName.ToLowerInvariant().Contains("maze"))
                    OtigoGameProgress.ClearGame("Maze");
                else
                    OtigoGameProgress.ClearGame("ShadowMatch");

                Debug.Log("Oyun kapatılıyor...");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            },
            1f);
    }
}
