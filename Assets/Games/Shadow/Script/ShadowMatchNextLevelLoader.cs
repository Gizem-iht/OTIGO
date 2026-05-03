using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchNextLevelLoader : MonoBehaviour
{
    [Header("Bir sonraki sahnenin adı")]
    public string nextSceneName;

    public void LoadNextLevel()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("ShadowMatchNextLevelLoader: nextSceneName boş bırakılmış.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}