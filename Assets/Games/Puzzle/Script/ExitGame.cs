using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void Exit()
    {
        Debug.Log("[Puzzle ExitGame] Exit button pressed.");

        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                OtigoGameProgress.ClearGame("Puzzle");
                PuzzleOtigoSessionTracker.ResetSession();

#if UNITY_ANDROID && !UNITY_EDITOR
                QuitAndroidActivity();
#else
                Debug.Log("[Puzzle ExitGame] Application.Quit called.");
                Application.Quit();
#endif

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            1f);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void QuitAndroidActivity()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    Debug.Log("[Puzzle ExitGame] Android activity finish called.");
                    activity.Call("finish");
                }));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Puzzle ExitGame] Android activity kapatılamadı: " + ex.Message);
            Application.Quit();
        }
    }
#endif
}
