using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CorrectImageMainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string categorySelectSceneName = "CorrectImage_CategorySelect";

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
        SceneManager.LoadScene(categorySelectSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}