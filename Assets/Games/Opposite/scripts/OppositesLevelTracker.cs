using UnityEngine;
using UnityEngine.SceneManagement;

public class OppositesLevelTracker : MonoBehaviour
{
    private const string LastLevelKey = "OppositesLastLevel";

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LastLevelKey, currentScene);
        PlayerPrefs.Save();

        Debug.Log("Opposites son level kaydedildi: " + currentScene);
    }
}