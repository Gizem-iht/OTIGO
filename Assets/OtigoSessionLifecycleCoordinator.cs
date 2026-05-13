using UnityEngine;

/// <summary>
/// Uygulama pause/quit sırasında tüm oyunların OTIGO oturum verisini backend'e gönderir.
/// </summary>
public static class OtigoSessionLifecycleCoordinator
{
    public static void FlushAllActiveGameSessions()
    {
        MazeOtigoSessionTracker.FlushAndSend();
        PuzzleOtigoSessionTracker.FlushAndSend();
        ObjectSelectOtigoSessionTracker.FlushAndSend();
        OppositesOtigoSessionTracker.FlushAndSend();
        NumberObjectOtigoSessionTracker.FlushAndSend();
        ShadowMatchOtigoSessionTracker.FlushAndSend();
        StoryQuizOtigoSessionTracker.FlushAndSend();

        if (ColoringSessionTracker.Instance != null)
            ColoringSessionTracker.Instance.FlushAndSend();

        if (CorrectImageGameLevelController.CurrentInstance != null)
            CorrectImageGameLevelController.CurrentInstance.FlushProgressSnapshot();
    }
}
