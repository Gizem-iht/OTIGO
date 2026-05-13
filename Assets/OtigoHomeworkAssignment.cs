using UnityEngine;

/// <summary>
/// Uzman ödevi: oyun açılışında Intent ile gelen hedef level sayısına göre tamamlanma bildirimi.
/// Native tarafta extra anahtarlar: homeworkRequiredLevels (örn. "2"), homeworkAssignmentId (opsiyonel, Kerem API için).
/// </summary>
public static class OtigoHomeworkAssignment
{
    private static string assignmentId = "";
    private static int requiredDistinctLevels;
    private static bool completionShown;

    public static string AssignmentId => assignmentId;
    public static int RequiredDistinctLevels => requiredDistinctLevels;
    public static bool HasActiveAssignment => requiredDistinctLevels > 0;

    public static void ConfigureFromLaunch(string homeworkAssignmentId, string homeworkRequiredLevels)
    {
        assignmentId = homeworkAssignmentId ?? "";
        completionShown = false;

        if (string.IsNullOrWhiteSpace(homeworkRequiredLevels) ||
            !int.TryParse(homeworkRequiredLevels.Trim(), out int parsed) ||
            parsed < 1)
        {
            requiredDistinctLevels = 0;
            return;
        }

        requiredDistinctLevels = parsed;
        Debug.Log("[OtigoHomework] Ödev yüklendi. Hedef level: " + requiredDistinctLevels +
                  (string.IsNullOrEmpty(assignmentId) ? "" : ", assignmentId: " + assignmentId));
    }

    /// <summary>
    /// Bu oturumda tamamlanan farklı level sayısı (tracker'daki mevcut sayı) ile çağrılır.
    /// </summary>
    public static void NotifyDistinctLevelsCompleted(int completedDistinctLevelCount)
    {
        if (requiredDistinctLevels <= 0 || completionShown)
            return;

        if (completedDistinctLevelCount < requiredDistinctLevels)
            return;

        completionShown = true;

        OtigoHomeworkFeedback.ShowHomeworkCompleteForChild(requiredDistinctLevels);
    }
}
