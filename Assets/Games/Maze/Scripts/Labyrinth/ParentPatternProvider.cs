using UnityEngine;

public class ParentPatternProvider : MonoBehaviour
{
    public static ParentPatternProvider Instance { get; private set; }

    [Header("Editor Test Pattern")]
    [SerializeField] private string editorTestPattern = "1,2,3,6,9";

    private string currentPattern;

    public string CurrentPattern
    {
        get
        {
            if (!string.IsNullOrEmpty(currentPattern))
                return currentPattern;

#if UNITY_EDITOR
            return editorTestPattern;
#else
            return "";
#endif
        }
    }

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

    public void SetPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            Debug.LogWarning("[ParentPatternProvider] Bos parentPattern geldi, mevcut deger korunuyor.");
            return;
        }

        currentPattern = pattern;
        Debug.Log("[ParentPatternProvider] parentPattern set edildi: " + currentPattern);
    }

    private void ReadPatternFromAndroidIntent()
    {
        currentPattern = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

                if (intent == null)
                {
                    Debug.LogWarning("[ParentPatternProvider] Android intent bulunamadi.");
                    return;
                }

                string incomingPattern = intent.Call<string>("getStringExtra", "parentPattern");

                if (!string.IsNullOrEmpty(incomingPattern))
                {
                    SetPattern(incomingPattern);
                }
                else
                {
                    Debug.LogWarning("[ParentPatternProvider] parentPattern intent extra gelmedi. Frontend kayitta belirlenen deseni gondermeli.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ParentPatternProvider] parentPattern Android intent'ten alinamadi. Hata: " + e.Message);
            currentPattern = "";
        }
#else
        Debug.Log("[ParentPatternProvider] Editor test pattern kullanilacak: " + CurrentPattern);
#endif
    }
}
