using UnityEngine;

public class ObjectSelect_ExitGameButton : MonoBehaviour
{
    public void ExitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                OtigoGameProgress.ClearGame("ObjectSelect");

                Debug.Log("Oyun kapatılıyor...");

                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            1f);
    }
}
