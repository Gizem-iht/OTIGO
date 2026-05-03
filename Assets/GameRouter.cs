using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameRouter : MonoBehaviour
{
    void Start()
    {
        string gameId        = GetStringFromIntent("gameId");
        string token         = GetStringFromIntent("token");
        string childIdStr    = GetStringFromIntent("childId");
        string parentPattern = GetStringFromIntent("parentPattern");

        Debug.Log("Gelen gameId: " + gameId);
        Debug.Log("Gelen token: " + (string.IsNullOrEmpty(token) ? "BOŞ!" : "VAR"));
        Debug.Log("Gelen childIdStr: " + (string.IsNullOrEmpty(childIdStr) ? "BOŞ!" : childIdStr));
        Debug.Log("Gelen parentPattern: " + (string.IsNullOrEmpty(parentPattern) ? "YOK, default kullanılacak" : parentPattern));

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
        if (OtigoActivityResultSender.Instance != null)
        {
            if (!string.IsNullOrEmpty(token))
                OtigoActivityResultSender.Instance.SetToken(token);

            if (int.TryParse(childIdStr, out int childId) && childId > 0)
                OtigoActivityResultSender.Instance.SetChildId(childId);

            if (string.IsNullOrEmpty(parentPattern))
                parentPattern = "1,2,3,6,9";

            OtigoActivityResultSender.Instance.SetParentPattern(parentPattern);
        }

        // DEFAULT OYUN = COLORING
        if (string.IsNullOrEmpty(gameId))
            gameId = "renk boyama";

        gameId = gameId.Trim().ToLowerInvariant();

        Debug.Log("[GameRouter] Scene yükleniyor. gameId: " + gameId);

        switch (gameId)
        {
            case "gölge-nesne eşleştirme":
                SceneManager.LoadScene("ShadowMatch_MainMenu");
                break;

            case "farklı cisim bulma":
                SceneManager.LoadScene("ObjectSelect_MainMenu");
                break;

            case "doğru nesneyi seçme":
                SceneManager.LoadScene("CorrectImage_Main");
                break;

            case "labirent takibi":
                SceneManager.LoadScene("MazeMainMenu");
                break;

            case "renk boyama":
                SceneManager.LoadScene("Coloring_MainMenu");
                break;

            case "sayı-nesne eşleştirme":
                SceneManager.LoadScene("numberobjectmatching_MainMenu");
                break;

            case "yapboz":
                SceneManager.LoadScene("Puzzle_MainMenu");
                break;

            case "zıt kavramlar":
                SceneManager.LoadScene("Opposites_MainMenu");
                break;

            case "hikaye dinleyip soru cevaplama":
                SceneManager.LoadScene("StoryQuiz_MainMenu");
                break;

            default:
                Debug.LogWarning("[GameRouter] Bilinmeyen gameId/name: " + gameId + ". Varsayılan oyun açılıyor.");
                SceneManager.LoadScene("Coloring_MainMenu");
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
        if (key == "gameId")        return "Renk Boyama";
        if (key == "token")         return "TEST_TOKEN";
        if (key == "childId")       return "1";
        if (key == "parentPattern") return "1,2,3,6,9";
        return "";
#endif
    }
}
