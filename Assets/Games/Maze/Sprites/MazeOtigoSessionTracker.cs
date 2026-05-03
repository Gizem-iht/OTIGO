using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MazeOtigoSessionTracker
{
    private static bool sessionStarted = false;
    private static bool finalSent = false;

    private static int activityId = 5;
    private static int totalParentHelpCount = 0;

    // levelNumber -> result
    private static Dictionary<int, OtigoActivityResultSender.LevelResult> levelResults
        = new Dictionary<int, OtigoActivityResultSender.LevelResult>();

    // levelNumber -> help count
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

        Debug.Log("Maze OTIGO session başladı. activityId: " + activityId);
    }

    public static bool HasSession()
    {
        return sessionStarted;
    }

    public static void AddOrUpdateLevelResult(int levelNumber, int durationSeconds, int mistakesMade)
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("Maze session başlamadan level result eklenmeye çalışıldı.");
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

        Debug.Log("Maze level kaydedildi -> levelNumber: " + levelNumber +
                  ", durationSeconds: " + durationSeconds +
                  ", mistakesMade: " + mistakesMade +
                  ", helpCount: " + helpCount);
    }

    public static void AddParentHelp()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("Maze session başlamadan parent help eklendi.");
            return;
        }

        totalParentHelpCount++;
        Debug.Log("Maze parent help eklendi. Toplam: " + totalParentHelpCount);
    }

    public static void AddParentHelpForLevel(int levelNumber)
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("Maze session başlamadan level bazlı parent help eklendi.");
            return;
        }

        totalParentHelpCount++;

        if (!levelHelpCounts.ContainsKey(levelNumber))
            levelHelpCounts[levelNumber] = 0;

        levelHelpCounts[levelNumber]++;

        Debug.Log("Maze parent help eklendi -> levelNumber: " + levelNumber +
                  ", levelHelpCount: " + levelHelpCounts[levelNumber] +
                  ", totalParentHelpCount: " + totalParentHelpCount);
    }

    public static void SendFinalResultIfPossible()
    {
        if (!sessionStarted)
        {
            Debug.LogWarning("Maze session yok, final sonuç gönderilemez.");
            return;
        }

        if (finalSent)
        {
            Debug.LogWarning("Maze final sonuç zaten gönderildi.");
            return;
        }

        if (OtigoActivityResultSender.Instance == null)
        {
            Debug.LogError("OtigoActivityResultSender.Instance bulunamadı!");
            return;
        }

        if (levelResults.Count == 0)
        {
            Debug.LogWarning("Hiç level sonucu yok, final gönderilmiyor.");
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

        Debug.Log("Maze final result hazırlanıyor...");
        Debug.Log("activityId: " + activityId);
        Debug.Log("durationSeconds: " + totalDurationSeconds);
        Debug.Log("mistakesMade: " + totalMistakesMade);
        Debug.Log("parentHelpCount: " + totalParentHelpCount);
        Debug.Log("totalTargetCount: " + totalTargetCount);
        Debug.Log("levelPlayed: " + maxLevelPlayed);

        for (int i = 0; i < orderedResults.Count; i++)
        {
            Debug.Log(
                "levelResults[" + i + "] => " +
                "levelNumber: " + orderedResults[i].levelNumber +
                ", durationSeconds: " + orderedResults[i].durationSeconds +
                ", mistakesMade: " + orderedResults[i].mistakesMade +
                ", helpCount: " + orderedResults[i].helpCount
            );
        }

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
        activityId = 5;
        totalParentHelpCount = 0;
        levelResults.Clear();
        levelHelpCounts.Clear();

        Debug.Log("Maze OTIGO session sıfırlandı.");
    }
}