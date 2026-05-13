using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameRouter : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[GameRouter] BUILD VERSION 2026-05-10-A");

        string gameId        = GetStringFromIntent("gameId");
        string token         = GetStringFromIntent("token");
        string childIdStr    = GetStringFromIntent("childId");
        string parentPattern = GetStringFromIntent("parentPattern");

        Debug.Log("Gelen gameId: " + gameId);
        Debug.Log("Gelen token: " + (string.IsNullOrEmpty(token) ? "BOŞ!" : "VAR"));
        Debug.Log("Gelen childIdStr: " + (string.IsNullOrEmpty(childIdStr) ? "BOŞ!" : childIdStr));
        Debug.Log("Gelen parentPattern: " + (string.IsNullOrEmpty(parentPattern) ? "YOK" : parentPattern));

        if (OtigoActivityResultSender.Instance == null)
        {
            StartCoroutine(WaitAndRoute(gameId, token, childIdStr, parentPattern));
            return;
        }

        ApplyAndRoute(gameId, token, childIdStr, parentPattern);
    }

    IEnumerator WaitAndRoute(string gameId, string token, string childIdStr, string parentPattern)
    {
        float timeout = 3f;

        while (OtigoActivityResultSender.Instance == null && timeout > 0)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        ApplyAndRoute(gameId, token, childIdStr, parentPattern);
    }

    void ApplyAndRoute(string gameId, string token, string childIdStr, string parentPattern)
    {
        if (string.IsNullOrEmpty(parentPattern))
            parentPattern = "1,2,3,6,9";

        if (OtigoActivityResultSender.Instance != null)
        {
            if (!string.IsNullOrEmpty(token))
                OtigoActivityResultSender.Instance.SetToken(token);

            if (int.TryParse(childIdStr, out int childId) && childId > 0)
                OtigoActivityResultSender.Instance.SetChildId(childId);

            OtigoActivityResultSender.Instance.SetParentPattern(parentPattern);
        }

        if (ParentPatternProvider.Instance != null)
            ParentPatternProvider.Instance.SetPattern(parentPattern);

        string homeworkAssignmentId = GetStringFromIntent("homeworkAssignmentId");
        string homeworkRequiredLevels = GetStringFromIntent("homeworkRequiredLevels");
        OtigoHomeworkAssignment.ConfigureFromLaunch(homeworkAssignmentId, homeworkRequiredLevels);

        // DEFAULT OYUN = OPPOSITES
        if (string.IsNullOrEmpty(gameId))
            gameId = "opposites";

        gameId = gameId.Trim().ToLowerInvariant();

        Debug.Log("[GameRouter] Scene yükleniyor. gameId: " + gameId);

        switch (gameId)
        {
            case "shadow_object_matching":
                SceneManager.LoadScene("ShadowMatch_MainMenu");
                break;

            case "different_object":
                SceneManager.LoadScene("ObjectSelect_MainMenu");
                break;

            case "correct_object":
                SceneManager.LoadScene("CorrectImage_Main");
                break;

            case "maze":
                SceneManager.LoadScene("MazeMainMenu");
                break;

            case "coloring":
                SceneManager.LoadScene("Coloring_MainMenu");
                break;

            case "number_object_matching":
                SceneManager.LoadScene("numberobjectmatching_MainMenu");
                break;

            case "puzzle":
                SceneManager.LoadScene("Puzzle_MainMenu");
                break;

            case "opposites":
                SceneManager.LoadScene("Opposites_MainMenu");
                break;

            case "story_quiz":
                SceneManager.LoadScene("StoryQuiz_MainMenu");
                break;

            default:
                Debug.LogWarning("[GameRouter] Bilinmeyen gameId/name: " + gameId + ". Varsayılan oyun açılıyor.");
                SceneManager.LoadScene("Opposites_MainMenu");
                break;
        }
    }

    string GetStringFromIntent(string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var intent   = activity.Call<AndroidJavaObject>("getIntent");
                string value = intent.Call<string>("getStringExtra", key);
                return string.IsNullOrEmpty(value) ? "" : value;
            }
        }
        catch
        {
            return "";
        }
#else
        // --- Editör (Play) sahte Intent: gerçek cihazda Android extra'ları kullanılır ---
        // Ödev panelini denemek için aşağıda "2" bırakın; 2 level bitirince panel çıkar. Kapatmak için "" yazın.
        if (key == "gameId")        return "opposites"; // Ödev panelini hızlı denemek için: "maze"
        if (key == "token")         return "eyJhbGciOiJIUzUxMiJ9.eyJyb2xlIjoiVkVMSSIsInN1YiI6ImxhcmFtaW5ha2FyYWRlbml6ekBnbWFpbC5jb20iLCJpYXQiOjE3Nzc4ODk2MzUsImV4cCI6MTc4MDQ4MTYzNX0.ay0mRm0Ive9kve-DG5WDTyPAs1ATaXaEBVmgRqM9KkbVF5M3Yp1Gc9pd3PV-ElawKh_eHuyXUHNJf-9iCyuDQg";
        if (key == "childId")       return "1";
        if (key == "parentPattern") return "1,2,3,6,9";
        if (key == "homeworkAssignmentId") return "editor-test";
        if (key == "homeworkRequiredLevels") return "2";
        return "";
#endif
    }
}
