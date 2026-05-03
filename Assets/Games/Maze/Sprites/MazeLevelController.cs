using UnityEngine;
using UnityEngine.SceneManagement;

public class MazeLevelController : MonoBehaviour
{
    private const string LastLevelKey = "MazeLastLevel";

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LastLevelKey, currentScene);
        PlayerPrefs.Save();

        Debug.Log("Kaydedilen level: " + currentScene);
    }
}