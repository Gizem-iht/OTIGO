using UnityEngine;

public class ObjectSelect_ExitGameButton : MonoBehaviour
{
    public void ExitGame()
    {
        Debug.Log("Oyun kapatılıyor...");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}