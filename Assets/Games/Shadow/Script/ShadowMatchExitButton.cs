using UnityEngine;

public class ShadowMatchExitButton : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Oyun kapatılıyor...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}