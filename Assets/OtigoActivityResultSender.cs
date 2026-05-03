using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OtigoActivityResultSender : MonoBehaviour
{
    public static OtigoActivityResultSender Instance { get; private set; }

    [Header("API Settings")]
    [SerializeField] private int childId = 1;
    [SerializeField] private string baseUrl = "https://otigo-app.onrender.com/api/v1/activities/result/";
    [SerializeField] private string bearerToken = "";

    private string parentPattern = "1,2,3,6,9";

    [System.Serializable]
    public class LevelResult
    {
        public int levelNumber;
        public int durationSeconds;
        public int mistakesMade;
        public int helpCount;
    }

    [System.Serializable]
    public class ActivityResultRequest
    {
        public int activityId;
        public int childId;
        public int durationSeconds;
        public int mistakesMade;
        public bool parentHelped;
        public int parentHelpCount;
        public int totalTargetCount;
        public int levelPlayed;
        public List<LevelResult> levelResults = new List<LevelResult>();
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

        Debug.Log("[OtigoActivityResultSender] Hazır. ChildId: " + childId);
    }

    public void SetChildId(int newChildId)
    {
        childId = newChildId;
        Debug.Log("[OtigoActivityResultSender] childId set edildi: " + childId);
    }

    public void SetToken(string newToken)
    {
        bearerToken = newToken;
        Debug.Log("[OtigoActivityResultSender] token set edildi.");
    }

    public void SetParentPattern(string pattern)
    {
        parentPattern = pattern;
        Debug.Log("[OtigoActivityResultSender] parentPattern set edildi: " + parentPattern);
    }

    public string GetParentPattern()
    {
        return parentPattern;
    }

    public int GetChildId()
    {
        return childId;
    }

    public void SendActivityResult(
        int activityId,
        int durationSeconds,
        int mistakesMade,
        int parentHelpCount,
        int totalTargetCount,
        int levelPlayed,
        List<LevelResult> levelResults = null
    )
    {
        Debug.Log("[OtigoActivityResultSender] SEND ACTIVITY RESULT CALISTI");

        if (string.IsNullOrEmpty(bearerToken))
        {
            Debug.LogError("[OtigoActivityResultSender] Bearer token boş! Önce SetToken çağrılmalı.");
            return;
        }

        if (childId <= 0)
        {
            Debug.LogError("[OtigoActivityResultSender] childId geçersiz! Gelen childId: " + childId);
            return;
        }

        if (levelResults == null)
            levelResults = new List<LevelResult>();

        ActivityResultRequest requestData = new ActivityResultRequest
        {
            activityId = activityId,
            childId = childId,
            durationSeconds = durationSeconds,
            mistakesMade = mistakesMade,
            parentHelped = parentHelpCount > 0,
            parentHelpCount = parentHelpCount,
            totalTargetCount = totalTargetCount,
            levelPlayed = levelPlayed,
            levelResults = levelResults
        };

        StartCoroutine(PostActivityResult(requestData));
    }

    private IEnumerator PostActivityResult(ActivityResultRequest requestData)
    {
        string url = baseUrl + childId;
        string json = JsonUtility.ToJson(requestData, true);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        Debug.Log("========== OTIGO POST REQUEST ==========");
        Debug.Log("[OtigoActivityResultSender] URL: " + url);
        Debug.Log("[OtigoActivityResultSender] BODY: " + json);
        Debug.Log("[OtigoActivityResultSender] Authorization var mı?: " + (!string.IsNullOrEmpty(bearerToken)));

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

            yield return request.SendWebRequest();

            Debug.Log("========== OTIGO POST RESPONSE ==========");
            Debug.Log("[OtigoActivityResultSender] Response Code: " + request.responseCode);
            Debug.Log("[OtigoActivityResultSender] Response Body: " + request.downloadHandler.text);

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[OtigoActivityResultSender] Activity result başarıyla gönderildi.");
            }
            else
            {
                Debug.LogError("[OtigoActivityResultSender] Activity result gönderilemedi!");
                Debug.LogError("[OtigoActivityResultSender] Error: " + request.error);
            }
        }
    }
}
