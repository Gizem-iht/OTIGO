using UnityEngine;
using UnityEngine.SceneManagement;

public class CorrectImageCategorySelectManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameLevelSceneName = "CorrectImage_CorrectImage_GameLevel";

    private const string SELECTED_CATEGORY_KEY = "CorrectImage_SelectedCategory";
    private const string CURRENT_LEVEL_KEY = "CorrectImage_CurrentLevel";

    public void SelectCategory(string categoryId)
    {
        PlayerPrefs.SetString(SELECTED_CATEGORY_KEY, categoryId);
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameLevelSceneName);
    }
}