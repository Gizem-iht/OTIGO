using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchTutorialController : MonoBehaviour
{
    private const string LastLevelKey = "LastPlayedLevel";
    private const string DefaultLevelSceneName = "ShadowMatch_Level1";

    public void GoBackToLastLevel()
    {
        string lastLevel = PlayerPrefs.GetString(LastLevelKey, DefaultLevelSceneName);
        Debug.Log("[ShadowMatchTutorialController] Gidilen level: " + lastLevel);
        SceneManager.LoadScene(lastLevel);
    }
}