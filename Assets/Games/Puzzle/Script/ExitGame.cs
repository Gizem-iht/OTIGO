using UnityEngine;

public class ExitGame : MonoBehaviour
{
    public void Exit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finish");
        }
#else
        Debug.Log("Application.Quit() (Editor only)");
        Application.Quit();
#endif
    }
}
