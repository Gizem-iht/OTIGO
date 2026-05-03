using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchRestartLevelButton : MonoBehaviour
{
    public void RestartCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        Debug.Log("Seviye yeniden başlatılıyor: " + currentSceneName);
    }
}