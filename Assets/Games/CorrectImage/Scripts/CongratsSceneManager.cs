using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CongratsSceneManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string categorySceneName = "CorrectImage_CategorySelect";

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(GoToCategories);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void GoToCategories()
    {
        SceneManager.LoadScene(categorySceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}