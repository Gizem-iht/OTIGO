using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleMenuManager : MonoBehaviour
{
    private const string LastLevelKey = "Puzzle_LastLevelName";
    private const string DefaultLevelName = "Puzzle_Level1";

    [Header("Scene Names")]
    public string defaultLevelSceneName = "Puzzle_Level1";

    public void StartGame()
    {
        string lastLevelName = PlayerPrefs.GetString(LastLevelKey, defaultLevelSceneName);

        if (!Application.CanStreamedLevelBeLoaded(lastLevelName))
        {
            lastLevelName = defaultLevelSceneName;
        }

        Debug.Log("[PuzzleMenuManager] StartGame, açılacak level = " + lastLevelName);
        SceneManager.LoadScene(lastLevelName);
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(LastLevelKey);
        PlayerPrefs.Save();
        Debug.Log("[PuzzleMenuManager] Puzzle ilerlemesi sıfırlandı.");
    }

    public void ExitGame()
    {
        Debug.Log("[PuzzleMenuManager] Uygulamadan çıkılıyor...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}