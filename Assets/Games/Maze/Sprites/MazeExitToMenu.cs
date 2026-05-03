using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MazeExitToMenu : MonoBehaviour
{
    private const string LastLevelKey = "MazeLastLevel";

    [Header("Scene Settings")]
    [SerializeField] private string menuScene = "MazeMainMenu";

    private bool isExiting = false;

    public void ExitToMenuButton()
    {
        if (isExiting)
            return;

        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        isExiting = true;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != menuScene)
        {
            PlayerPrefs.SetString(LastLevelKey, currentScene);
            PlayerPrefs.Save();
            Debug.Log("Çıkışta kaydedilen level: " + currentScene);
        }

        if (!Application.CanStreamedLevelBeLoaded(menuScene))
        {
            Debug.LogError("Menü sahnesi bulunamadı: " + menuScene);
            isExiting = false;
            yield break;
        }

        yield return null; // UI click işlemi temiz bitsin diye 1 frame bekle

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(menuScene);
        while (!loadOp.isDone)
        {
            yield return null;
        }
    }
}