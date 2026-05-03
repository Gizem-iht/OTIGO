using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StoryQuizOtigoSessionTracker
{
    private static bool sessionStarted = false;
    private static bool finalSent = false;

    private static int activityId = 11; // gerçek activityId neyse onu yaz
    private static int totalParentHelpCount = 0;

    // levelNumber -> result
    private static Dictionary<int, OtigoActivityResultSender.LevelResult> levelResults
        = new Dictionary<int, OtigoActivityResultSender.LevelResult>();

    public static void BeginSession(int newActivityId)
    {
        sessionStarted = true;
        finalSent = false;
        activityId = newActivityId;
        totalParentHelpCount = 0;
        levelResults.Clear();

        Debug.Log("StoryQuiz OTIGO session başladı. activityId: " + activityId);
    }

    public static bool HasSession()
    {
        return sessionStarted;
    }

    public static void AddOrUpdateLevelResult(int levelNumber, int durationSeconds, int mistakesMade, int helpCount = 0)
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("StoryQuiz session başlamadan level result eklenmeye çalışıldı.");
            return;
        }

        var result = new OtigoActivityResultSender.LevelResult
        {
            levelNumber = levelNumber,
            durationSeconds = durationSeconds,
            mistakesMade = mistakesMade,
            helpCount = helpCount
        };

        levelResults[levelNumber] = result;

        Debug.Log("StoryQuiz level kaydedildi -> levelNumber: " + levelNumber +
                  ", durationSeconds: " + durationSeconds +
                  ", mistakesMade: " + mistakesMade +
                  ", helpCount: " + helpCount);
    }

    public static void AddParentHelp()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("StoryQuiz session başlamadan parent help eklendi.");
            return;
        }

        totalParentHelpCount++;
        Debug.Log("StoryQuiz parent help eklendi. Toplam: " + totalParentHelpCount);
    }

    public static int GetTotalParentHelpCount()
    {
        return totalParentHelpCount;
    }

    public static void SendFinalResultIfPossible()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("StoryQuiz session yok, final sonuç gönderilemez.");
            return;
        }

        if (finalSent)
        {
            Debug.LogWarning("StoryQuiz final sonuç zaten gönderildi.");
            return;
        }

        if (OtigoActivityResultSender.Instance == null)
        {
            Debug.LogError("OtigoActivityResultSender.Instance bulunamadı!");
            return;
        }

        if (levelResults.Count == 0)
        {
            Debug.LogWarning("Hiç StoryQuiz level sonucu yok, final gönderilmiyor.");
            return;
        }

        List<OtigoActivityResultSender.LevelResult> orderedResults = levelResults
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .ToList();

        int totalDurationSeconds = orderedResults.Sum(x => x.durationSeconds);
        int totalMistakesMade = orderedResults.Sum(x => x.mistakesMade);
        int totalTargetCount = orderedResults.Count;
        int maxLevelPlayed = orderedResults.Max(x => x.levelNumber);

        Debug.Log("StoryQuiz final result hazırlanıyor...");
        Debug.Log("activityId: " + activityId);
        Debug.Log("durationSeconds: " + totalDurationSeconds);
        Debug.Log("mistakesMade: " + totalMistakesMade);
        Debug.Log("parentHelpCount: " + totalParentHelpCount);
        Debug.Log("totalTargetCount: " + totalTargetCount);
        Debug.Log("levelPlayed: " + maxLevelPlayed);

        finalSent = true;

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

    public static void ResetSession()
    {
        sessionStarted = false;
        finalSent = false;
        activityId = 11;
        totalParentHelpCount = 0;
        levelResults.Clear();

        Debug.Log("StoryQuiz OTIGO session sıfırlandı.");
    }
}