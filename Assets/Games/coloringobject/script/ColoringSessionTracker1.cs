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

    public static ColoringSessionTracker EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject trackerObject = new GameObject("ColoringSessionTracker");
        Instance = trackerObject.AddComponent<ColoringSessionTracker>();
        DontDestroyOnLoad(trackerObject);
        return Instance;
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

        FlushAndSend();

        OtigoHomeworkAssignment.NotifyDistinctLevelsCompleted(levelResults.Count);
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
            List<OtigoActivityResultSender.LevelResult> otigoLevelResults =
                new List<OtigoActivityResultSender.LevelResult>();

            foreach (LevelResult result in levelResults)
            {
                otigoLevelResults.Add(new OtigoActivityResultSender.LevelResult
                {
                    levelNumber = result.level,
                    durationSeconds = result.durationSeconds,
                    mistakesMade = result.mistakesMade,
                    helpCount = 0
                });
            }

            OtigoActivityResultSender.Instance.SendActivityResult(
                activityId,
                totalDurationSeconds,
                totalMistakesMade,
                parentHelpCount,
                levelResults.Count,
                levelPlayed,
                otigoLevelResults
            );
        }
        else
        {
            Debug.LogError("COLORING FINAL RESULT gonderilemedi: OtigoActivityResultSender.Instance yok.");
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

    public bool HasUnflushedProgress()
    {
        return levelResults != null && levelResults.Count > 0;
    }

    /// <summary>
    /// Kayıtlı level verisi varsa backend'e gönderir (tekrarlı POST upsert ile sorun değil).
    /// </summary>
    public void FlushAndSend()
    {
        if (!HasUnflushedProgress() || OtigoActivityResultSender.Instance == null)
            return;

        SendFinalResult();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            FlushAndSend();
    }

    private void OnApplicationQuit()
    {
        FlushAndSend();
    }
}
