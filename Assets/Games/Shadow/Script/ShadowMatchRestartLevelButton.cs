using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahne Restart butonu bu sınıf adıyla bağlıdır (YAML: RestartLevelButton).
/// </summary>
public class RestartLevelButton : MonoBehaviour
{
    public void RestartCurrentLevel()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        ShadowMatchLevelVariantRandomizer.MarkReloadShouldPreferDifferentVariant();
        SceneManager.LoadScene(currentSceneName);
        Debug.Log("Seviye yeniden başlatılıyor: " + currentSceneName);
    }
}
