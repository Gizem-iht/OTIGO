using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class OppositesOtigoSessionTracker
{
    private static bool sessionStarted = false;
    private static bool finalSent = false;

    private static int activityId = 10;
    private static int totalParentHelpCount = 0;

    private static Dictionary<int, OtigoActivityResultSender.LevelResult> levelResults
        = new Dictionary<int, OtigoActivityResultSender.LevelResult>();

    private static Dictionary<int, int> levelHelpCounts
        = new Dictionary<int, int>();

    public static void BeginSession(int newActivityId)
    {
        sessionStarted = true;
        finalSent = false;
        activityId = newActivityId;
        totalParentHelpCount = 0;
        levelResults.Clear();
        levelHelpCounts.Clear();

        Debug.Log("Opposites session başladı. activityId: " + activityId);
    }

    public static bool HasSession()
    {
        return sessionStarted;
    }

    public static void AddOrUpdateLevelResult(int levelNumber, int durationSeconds, int mistakesMade)
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("Session başlamadan level sonucu eklenmeye çalışıldı.");
            return;
        }

        int helpCount = 0;
        if (levelHelpCounts.ContainsKey(levelNumber))
            helpCount = levelHelpCounts[levelNumber];

        var result = new OtigoActivityResultSender.LevelResult
        {
            levelNumber = levelNumber,
            durationSeconds = durationSeconds,
            mistakesMade = mistakesMade,
            helpCount = helpCount
        };

        levelResults[levelNumber] = result;

        FlushAndSend();

        OtigoHomeworkAssignment.NotifyDistinctLevelsCompleted(levelResults.Count);
    }

    public static bool HasUnflushedData()
    {
        return sessionStarted
            && levelResults.Count > 0
            && OtigoActivityResultSender.Instance != null;
    }

    public static void FlushAndSend()
    {
        TryPostAggregateSnapshot();
    }

    private static void TryPostAggregateSnapshot()
    {
        if (!sessionStarted)
            return;

        if (OtigoActivityResultSender.Instance == null)
            return;

        if (levelResults.Count == 0)
            return;

        List<OtigoActivityResultSender.LevelResult> orderedResults = levelResults
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        int totalDurationSeconds = orderedResults.Sum(x => x.durationSeconds);
        int totalMistakesMade = orderedResults.Sum(x => x.mistakesMade);
        int totalTargetCount = orderedResults.Count;
        int maxLevelPlayed = orderedResults.Max(x => x.levelNumber);

        OtigoActivityResultSender.Instance.SendActivityResult(
            activityId: activityId,
            durationSeconds: totalDurationSeconds,
            mistakesMade: totalMistakesMade,
            parentHelpCount: totalParentHelpCount,
            totalTargetCount: totalTargetCount,
            levelPlayed: maxLevelPlayed,
            levelResults: orderedResults
        );
    }

    public static void AddParentHelpForLevel(int levelNumber)
    {
        if (!sessionStarted) return;

        totalParentHelpCount++;

        if (!levelHelpCounts.ContainsKey(levelNumber))
            levelHelpCounts[levelNumber] = 0;

        levelHelpCounts[levelNumber]++;
    }

    public static void SendFinalResultIfPossible()
    {
        if (!sessionStarted || finalSent) return;
        if (OtigoActivityResultSender.Instance == null) return;
        if (levelResults.Count == 0) return;

        finalSent = true;
        TryPostAggregateSnapshot();
    }

    public static void ResetSession()
    {
        sessionStarted = false;
        finalSent = false;
        activityId = 10;
        totalParentHelpCount = 0;
        levelResults.Clear();
        levelHelpCounts.Clear();
    }
}