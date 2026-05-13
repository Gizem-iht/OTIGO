using UnityEngine;
using UnityEngine.SceneManagement;

public static class OtigoGameProgress
{
    public const string ObjectSelectKey = "ObjectSelect_LastLevelSceneName";
    public const string NumberObjectKey = "NumberObjectMatching_LastLevelSceneName";
    public const string MazeKey = "MazeLastLevel";
    public const string ShadowKey = "LastPlayedLevel";
    public const string OppositesKey = "Opposites_LastLevelSceneName";
    public const string PuzzleKey = "Puzzle_LastLevelName";
    public const string ColoringKey = "Coloring_LastLevel";
    public const string StoryQuizKey = "StoryQuiz_LastLevel";
    public const string CorrectImageLevelKey = "CorrectImage_CurrentLevel";

    public static string GetResumeScene(string key, string firstLevelSceneName)
    {
        string savedScene = PlayerPrefs.GetString(key, "");

        if (!string.IsNullOrEmpty(savedScene) && Application.CanStreamedLevelBeLoaded(savedScene))
            return savedScene;

        return firstLevelSceneName;
    }

    public static void SaveCurrentLevel(string key)
    {
        SaveLevel(key, SceneManager.GetActiveScene().name);
    }

    public static void SaveLevel(string key, string sceneName)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sceneName))
            return;

        PlayerPrefs.SetString(key, sceneName);
        PlayerPrefs.Save();
    }

    public static void SaveNextOrClearForFinal(string key, string nextSceneName, string finalSceneName)
    {
        if (IsFinalScene(nextSceneName, finalSceneName))
            ClearKey(key);
        else
            SaveLevel(key, nextSceneName);
    }

    public static bool IsFinalScene(string sceneName, string finalSceneName = "")
    {
        if (string.IsNullOrEmpty(sceneName))
            return true;

        if (!string.IsNullOrEmpty(finalSceneName) && sceneName == finalSceneName)
            return true;

        string lower = sceneName.ToLowerInvariant();
        return lower.Contains("tebrik") || lower.Contains("congrats");
    }

    public static void ClearKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    public static void ClearGame(string gameId)
    {
        switch (gameId)
        {
            case "ObjectSelect":
                ClearKey(ObjectSelectKey);
                break;
            case "NumberObjectMatching":
                ClearKey(NumberObjectKey);
                break;
            case "Maze":
                ClearKey(MazeKey);
                break;
            case "ShadowMatch":
                ClearKey(ShadowKey);
                break;
            case "Opposites":
                ClearKey(OppositesKey);
                PlayerPrefs.DeleteKey("OppositesLastLevel");
                PlayerPrefs.Save();
                break;
            case "Puzzle":
                ClearKey(PuzzleKey);
                break;
            case "Coloring":
                ClearKey(ColoringKey);
                break;
            case "StoryQuiz":
                ClearKey(StoryQuizKey);
                break;
            case "CorrectImage":
                PlayerPrefs.SetInt(CorrectImageLevelKey, 0);
                PlayerPrefs.DeleteKey("CorrectImage_LastLevel");
                PlayerPrefs.Save();
                break;
        }
    }
}
