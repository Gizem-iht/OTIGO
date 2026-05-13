using UnityEngine;
using UnityEngine.UI;

public class StoryQuizTebrikManager : MonoBehaviour
{
    [Header("Button")]
    public Button exitButton;

    [Header("Progress")]
    public string lastLevelKey = "StoryQuiz_LastLevel";

    private void Start()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    public void ExitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                OtigoGameProgress.ClearGame("StoryQuiz");

                StoryQuizOtigoSessionTracker.ResetSession();

                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            1f);
    }
}
