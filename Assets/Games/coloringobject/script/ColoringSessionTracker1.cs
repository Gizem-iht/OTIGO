using UnityEngine;
using System.Collections.Generic;

public class ColoringSessionTracker : MonoBehaviour
{
    public static ColoringSessionTracker Instance;

    [Header("Session")]
    public int activityId = 6;

    private int totalDurationSeconds = 0;
    private int totalMistakesMade = 0;
    private int parentHelpCount = 0;
    private int levelPlayed = 0;

    private List<LevelResult> levelResults = new List<LevelResult>();

    [System.Serializable]
    public class LevelResult
    {
        public int level;
        public int durationSeconds;
        public int mistakesMade;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddOrUpdateLevelResult(int level, int durationSeconds, int mistakesMade)
    {
        LevelResult existing = levelResults.Find(x => x.level == level);

        if (existing != null)
        {
            existing.durationSeconds = durationSeconds;
            existing.mistakesMade = mistakesMade;
        }
        else
        {
            LevelResult result = new LevelResult();
            result.level = level;
            result.durationSeconds = durationSeconds;
            result.mistakesMade = mistakesMade;
            levelResults.Add(result);
        }

        RecalculateTotals();
    }

    public void AddParentHelp()
    {
        parentHelpCount++;
    }

    private void RecalculateTotals()
    {
        totalDurationSeconds = 0;
        totalMistakesMade = 0;
        levelPlayed = levelResults.Count;

        foreach (LevelResult result in levelResults)
        {
            totalDurationSeconds += result.durationSeconds;
            totalMistakesMade += result.mistakesMade;
        }
    }

    public void SendFinalResult()
    {

        Debug.Log("COLORING FINAL RESULT");
        Debug.Log("ActivityId: " + activityId);
        Debug.Log("Duration: " + totalDurationSeconds);
        Debug.Log("Mistakes: " + totalMistakesMade);
        Debug.Log("ParentHelp: " + parentHelpCount);
        Debug.Log("LevelPlayed: " + levelPlayed);

        if (OtigoActivityResultSender.Instance != null)
        {
            OtigoActivityResultSender.Instance.SendActivityResult(
                activityId,
                totalDurationSeconds,
                totalMistakesMade,
                parentHelpCount,
                levelPlayed,
                levelResults.Count
            );
        }
    }

    public static void ResetSession()
    {
        if (Instance == null)
            return;

        Instance.totalDurationSeconds = 0;
        Instance.totalMistakesMade = 0;
        Instance.parentHelpCount = 0;
        Instance.levelPlayed = 0;
        Instance.levelResults.Clear();
    }
}
