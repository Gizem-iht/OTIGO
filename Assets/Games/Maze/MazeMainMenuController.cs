using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeMainMenuController : MonoBehaviour
{
    [Header("İlk açılacak level")]
    public string firstLevelSceneName = "Maze_Level1";

    [Header("Kaydedilen level key")]
    public string lastLevelKey = "MazeLastLevel";

    [Header("Menu scene adı")]
    public string menuSceneName = "MazeMainMenu";

    public void StartGame()
    {
        string savedLevel = PlayerPrefs.GetString(lastLevelKey, "");

        Debug.Log("Kaydedilen level: " + savedLevel);

        // Kayıt yoksa ilk level
        if (string.IsNullOrEmpty(savedLevel))
        {
            Debug.Log("Kayıt yok, ilk level açılıyor: " + firstLevelSceneName);
            SceneManager.LoadScene(firstLevelSceneName);
            return;
        }

        // Eğer yanlışlıkla menu kaydedildiyse yine ilk levele git
        if (savedLevel == menuSceneName)
        {
            Debug.LogWarning("Kaydedilen scene menu olduğu için ilk levele yönlendiriliyor.");
            SceneManager.LoadScene(firstLevelSceneName);
            return;
        }

        // Build settings içinde varsa kayıtlı levele git
        if (Application.CanStreamedLevelBeLoaded(savedLevel))
        {
            Debug.Log("Kayıtlı level açılıyor: " + savedLevel);
            SceneManager.LoadScene(savedLevel);
        }
        else
        {
            Debug.LogWarning("Kaydedilen scene bulunamadı. İlk level açılıyor: " + firstLevelSceneName);
            SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    public void ExitGame()
    {
        Debug.Log("Oyun kapatılıyor.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ClearSavedProgress()
    {
        PlayerPrefs.DeleteKey(lastLevelKey);
        PlayerPrefs.Save();
        Debug.Log("Kaydedilen progress silindi.");
    }
}