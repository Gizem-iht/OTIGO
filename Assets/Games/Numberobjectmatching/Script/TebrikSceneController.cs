using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TebrikSceneController : MonoBehaviour
{
    public Button exitButton;

    void Start()
    {
        if (exitButton != null)
            exitButton.onClick.AddListener(QuitGame);
    }

    public void QuitGame()
    {
        Debug.Log("Oyun kapatılıyor...");

#if UNITY_EDITOR
        // Editor'de oyunu durdurur
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Android / build'de uygulamayı kapatır
        Application.Quit();
#endif
    }
}