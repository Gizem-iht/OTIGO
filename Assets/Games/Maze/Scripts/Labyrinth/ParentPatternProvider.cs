using UnityEngine;

public class ParentPatternProvider : MonoBehaviour
{
    public static ParentPatternProvider Instance { get; private set; }

    [Header("Default Pattern")]
    [SerializeField] private string defaultPattern = "1,2,3,6,9";

    private string currentPattern;

    public string CurrentPattern => string.IsNullOrEmpty(currentPattern) ? defaultPattern : currentPattern;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ReadPatternFromAndroidIntent();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ReadPatternFromAndroidIntent()
    {
        currentPattern = defaultPattern;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

                if (intent != null)
                {
                    string incomingPattern = intent.Call<string>("getStringExtra", "parentPattern");

                    if (!string.IsNullOrEmpty(incomingPattern))
                    {
                        currentPattern = incomingPattern;
                        Debug.Log("Android intent üzerinden gelen parent pattern: " + currentPattern);
                    }
                    else
                    {
                        Debug.Log("Intent'te parentPattern yok. Varsayılan kullanılacak: " + defaultPattern);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Pattern Android intent'ten alınamadı. Varsayılan kullanılacak. Hata: " + e.Message);
            currentPattern = defaultPattern;
        }
#else
        Debug.Log("Editor modunda varsayılan pattern kullanılıyor: " + currentPattern);
#endif
    }
}