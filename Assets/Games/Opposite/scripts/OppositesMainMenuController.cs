using UnityEngine;
using UnityEngine.SceneManagement;

public class OppositesMainMenuController : MonoBehaviour
{
    private const string LastLevelKey = "OppositesLastLevel";

    [Header("İlk açılacak level")]
    public string defaultFirstLevelSceneName = "Opposites_Level1";

    public void StartGame()
    {
        string targetScene = defaultFirstLevelSceneName;

        if (PlayerPrefs.HasKey(LastLevelKey))
        {
            string savedScene = PlayerPrefs.GetString(LastLevelKey);

            if (!string.IsNullOrEmpty(savedScene))
            {
                targetScene = savedScene;
            }
        }

        Debug.Log("StartGame -> Açılacak scene: " + targetScene);
        SceneManager.LoadScene(targetScene);
    }

    public void ExitGame()
    {
        Debug.Log("Oyundan çıkılıyor...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(LastLevelKey);
        PlayerPrefs.Save();

        Debug.Log("Opposites progress sıfırlandı.");
    }
}