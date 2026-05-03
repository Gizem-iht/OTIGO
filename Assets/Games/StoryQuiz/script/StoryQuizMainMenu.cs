using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryQuizMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Scene Names")]
    public string firstLevelSceneName = "StoryQuiz_Level1";

    [Header("Session")]
    public int activityId = 11;

    [Header("Progress")]
    public string lastLevelKey = "StoryQuiz_LastLevel";

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

        Debug.Log("StoryQuiz MainMenu kayıtlı level: " + PlayerPrefs.GetString(lastLevelKey, "KAYIT YOK"));
    }
    
public void StartGame()
{
    string saved = PlayerPrefs.GetString("StoryQuiz_LastLevel", "KAYIT YOK");
    Debug.Log("KAYIT = " + saved);

    SceneManager.LoadScene(saved == "KAYIT YOK" ? "StoryQuiz_Level1" : saved);
}

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}