using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CategorySelectUIManager : MonoBehaviour
{
    [Header("Close Button")]
    public Button closeButton;

    [Header("Main Scene (Build Settings name)")]
    public string mainSceneName = "Main";

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(GoToMain);
        }
        else
        {
            Debug.LogWarning("CloseButton atanmadı!");
        }
    }

    public void GoToMain()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}
