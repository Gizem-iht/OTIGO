using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ColoringMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button startButton;
    public Button continueButton;
    public Button quitButton;

    [Header("Scenes")]
    public string firstLevelSceneName = "Coloring_Level1";

    [Header("Progress")]
    public string lastLevelKey = "Coloring_LastLevel";
    public string savedTaskProgressKeysKey = "Coloring_TaskProgressKeys";

    private static readonly string[] legacyTaskProgressKeys =
    {
        "Coloring_Level_1_Task_ball_Completed",
        "Coloring_Level_2_Task_pear_Completed",
        "Coloring_Level_2_Task_orange_Completed",
        "Coloring_Level_3_Task_Bot_Completed",
        "Coloring_Level_3_Task_Glass_Completed",
        "Coloring_Level_4_Task_hediye_Completed",
        "Coloring_Level_4_Task_kutu_Completed",
        "Coloring_Level_4_Task_koltuk_Completed",
        "Coloring_Level_5_Task_Flag_Completed",
        "Coloring_Level_5_Task_Jumper_Completed",
        "Coloring_Level_5_Task_Car_Completed",
        "Coloring_Level_5_Task_Flower_Completed",
        "Coloring_Level_6_Task_Headphone_Completed",
        "Coloring_Level_6_Task_Lamp_Completed",
        "Coloring_Level_6_Task_Bag_Completed"
    };

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartOrContinueGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(StartOrContinueGame);
            continueButton.gameObject.SetActive(PlayerPrefs.HasKey(lastLevelKey));
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void StartOrContinueGame()
    {
        bool hasSavedLevel = PlayerPrefs.HasKey(lastLevelKey);

        if (!hasSavedLevel)
            ClearColoringTaskProgress();

        string sceneToLoad = PlayerPrefs.GetString(lastLevelKey, firstLevelSceneName);
        SceneManager.LoadScene(sceneToLoad);
    }

    private void ClearColoringTaskProgress()
    {
        string savedKeys = PlayerPrefs.GetString(savedTaskProgressKeysKey, "");
        string[] keys = savedKeys.Split('|');

        foreach (string key in keys)
        {
            if (!string.IsNullOrEmpty(key))
                PlayerPrefs.DeleteKey(key);
        }

        foreach (string key in legacyTaskProgressKeys)
        {
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.DeleteKey(savedTaskProgressKeysKey);
        PlayerPrefs.Save();
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
