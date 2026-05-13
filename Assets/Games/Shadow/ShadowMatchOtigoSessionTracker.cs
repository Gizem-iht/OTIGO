using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ShadowMatchOtigoSessionTracker
{
    private static bool sessionStarted = false;
    private static bool finalSent = false;

    private static int activityId = 2;
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

        Debug.Log("ShadowMatch OTIGO session başladı. activityId: " + activityId);
    }

    public static bool HasSession()
    {
        return sessionStarted;
    }

    public static void AddOrUpdateLevelResult(int levelNumber, int durationSeconds, int mistakesMade, int helpCount = 0)
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("ShadowMatch session başlamadan level result eklenmeye çalışıldı.");
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

        Debug.Log("ShadowMatch level kaydedildi -> levelNumber: " + levelNumber +
                  ", durationSeconds: " + durationSeconds +
                  ", mistakesMade: " + mistakesMade +
                  ", helpCount: " + helpCount);

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
        {
            Debug.LogError("OtigoActivityResultSender.Instance bulunamadı!");
            return;
        }

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

        Debug.Log("ShadowMatch final result hazırlanıyor...");
        Debug.Log("activityId: " + activityId);
        Debug.Log("durationSeconds: " + totalDurationSeconds);
        Debug.Log("mistakesMade: " + totalMistakesMade);
        Debug.Log("parentHelpCount: " + totalParentHelpCount);
        Debug.Log("totalTargetCount: " + totalTargetCount);
        Debug.Log("levelPlayed: " + maxLevelPlayed);

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

    public static void AddParentHelp()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("ShadowMatch session başlamadan parent help eklendi.");
            return;
        }

        totalParentHelpCount++;
        Debug.Log("ShadowMatch parent help eklendi. Toplam: " + totalParentHelpCount);
    }

    public static int GetTotalParentHelpCount()
    {
        return totalParentHelpCount;
    }

    public static void SendFinalResultIfPossible()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("ShadowMatch session yok, final sonuç gönderilemez.");
            return;
        }

        if (finalSent)
        {
            Debug.LogWarning("ShadowMatch final sonuç zaten gönderildi.");
            return;
        }

        if (OtigoActivityResultSender.Instance == null)
        {
            Debug.LogError("OtigoActivityResultSender.Instance bulunamadı!");
            return;
        }

        if (levelResults.Count == 0)
        {
            Debug.LogWarning("Hiç ShadowMatch level sonucu yok, final gönderilmiyor.");
            return;
        }

        finalSent = true;
        TryPostAggregateSnapshot();
    }

    public static void ResetSession()
    {
        sessionStarted = false;
        finalSent = false;
        activityId = 2;
        totalParentHelpCount = 0;
        levelResults.Clear();

        Debug.Log("ShadowMatch OTIGO session sıfırlandı.");
    }
}