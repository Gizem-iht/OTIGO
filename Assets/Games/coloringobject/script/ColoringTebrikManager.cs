using UnityEngine;
using UnityEngine.UI;

public class ColoringTebrikManager : MonoBehaviour
{
    [Header("Button")]
    public Button exitButton;

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
                OtigoGameProgress.ClearGame("Coloring");
                ClearColoringProgress();

                ColoringSessionTracker.ResetSession();

                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            },
            1f);
    }

    private void ClearColoringProgress()
    {
        PlayerPrefs.DeleteKey(lastLevelKey);

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
}
