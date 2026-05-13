using System;
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
    [SerializeField] private string baseUrl = "https://otigo-app.onrender.com/api/v1/activity-results/child/";
    [SerializeField] private string bearerToken = "";

    private string parentPattern = "";

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

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions();
    }

    private void OnApplicationQuit()
    {
        OtigoSessionLifecycleCoordinator.FlushAllActiveGameSessions();
    }

    /// <summary>
    /// Çıkıştan önce flush coroutine'ini bu bileşende çalıştırır (DontDestroyOnLoad).
    /// </summary>
    public static void RequestQuitWithFlush(Action flushAction, Action quitAction, float delaySeconds = 1f)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.CoQuitWithFlush(flushAction, quitAction, delaySeconds));
            return;
        }

        flushAction?.Invoke();
        quitAction?.Invoke();
    }

    private IEnumerator CoQuitWithFlush(Action flushAction, Action quitAction, float delaySeconds)
    {
        flushAction?.Invoke();
        yield return new WaitForSecondsRealtime(delaySeconds);
        quitAction?.Invoke();
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
        if (string.IsNullOrEmpty(pattern))
        {
            Debug.LogWarning("[OtigoActivityResultSender] parentPattern bos geldi, mevcut deger korunuyor.");
            return;
        }

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
        Debug.Log("SEND ACTIVITY RESULT CALISTI");

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
        Debug.Log("OTIGO POST REQUEST URL: " + url);
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
            Debug.Log("Response Code: " + request.responseCode);
            Debug.Log("[OtigoActivityResultSender] Response Body: " + request.downloadHandler.text);

            if (request.responseCode == 400 || request.responseCode == 401 || request.responseCode == 403)
            {
                Debug.LogError("OTIGO ERROR RESPONSE CODE: " + request.responseCode);
                Debug.LogError("OTIGO ERROR RESPONSE BODY: " + request.downloadHandler.text);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[OtigoActivityResultSender] Activity result basariyla gonderildi. Beklenen basari kodu: 201 Created, gelen kod: " + request.responseCode);
            }
            else
            {
                Debug.LogError("[OtigoActivityResultSender] Activity result gönderilemedi!");
                Debug.LogError("[OtigoActivityResultSender] Error: " + request.error);
            }
        }
    }
}
