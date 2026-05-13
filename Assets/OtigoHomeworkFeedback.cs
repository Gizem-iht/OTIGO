using UnityEngine;

/// <summary>
/// Ödev tamamlandığında bildirim: çocuk modunda kısa titreşim + tek kelime Toast + panel.
/// </summary>
public static class OtigoHomeworkFeedback
{
    /// <summary>Okuma gerektirmeyen kısa kutlama (ödev bitti).</summary>
    public static void ShowHomeworkCompleteForChild(int assignmentLevelCount)
    {
        Debug.Log("[OtigoHomework] Kutlama (cocuk modu), hedef level: " + assignmentLevelCount);

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Handheld.Vibrate();
        }
        catch { /* eski cihaz */ }
#endif

        OtigoHomeworkCompletionPanel.ShowForChild(assignmentLevelCount);
        ShowAndroidToastShort("Aferin!");
    }

    /// <summary>Uzun metin (eski / debug). Çocuk dostu akış için <see cref="ShowHomeworkCompleteForChild"/> kullanın.</summary>
    public static void Show(string title, string subtitle)
    {
        string body = string.IsNullOrEmpty(subtitle) ? title : title + "\n" + subtitle;
        Debug.Log("[OtigoHomework] " + body);

        OtigoHomeworkCompletionPanel.Show(title, subtitle);
        ShowAndroidToastShort(body);
    }

    private static void ShowAndroidToastShort(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
                    {
                        var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                        const int lengthShort = 0;
                        var toast = toastClass.CallStatic<AndroidJavaObject>(
                            "makeText", context, message, lengthShort);
                        toast.Call("show");
                    }
                }));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[OtigoHomework] Toast gösterilemedi: " + ex.Message);
        }
#endif
    }
}
