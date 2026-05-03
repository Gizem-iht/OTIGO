using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowMatchExitToMain : MonoBehaviour
{
    public string mainSceneName = "ShadowMatch_MainMenu";

    public void GoToMain()
    {
        Debug.Log("[ShadowMatchExitToMain] GoToMain çağrıldı. Yükleniyor: " + mainSceneName);
        SceneManager.LoadScene(mainSceneName);
    }
}