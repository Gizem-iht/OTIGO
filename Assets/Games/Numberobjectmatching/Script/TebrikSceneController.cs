using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TebrikSceneController : MonoBehaviour
{
    public Button exitButton;

    void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(QuitGame);
    }

    public void QuitGame()
    {
        OtigoActivityResultSender.RequestQuitWithFlush(
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions,
            () =>
            {
                OtigoGameProgress.ClearGame("NumberObjectMatching");

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
