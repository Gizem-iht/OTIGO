using UnityEngine;

public class OppositesExitButton : MonoBehaviour
{
    public void ExitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                OtigoGameProgress.ClearGame("Opposites");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            },
            1f);
    }
}
