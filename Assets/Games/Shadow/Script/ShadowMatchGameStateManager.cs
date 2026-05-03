using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchGameStateManager : MonoBehaviour
{
    public const string MainMenuSceneName = "ShadowMatch_MainMenu";

    public void SaveAndReturnToMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }
}