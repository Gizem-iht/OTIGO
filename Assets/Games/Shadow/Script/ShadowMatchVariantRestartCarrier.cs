using UnityEngine;

/// <summary>
/// Restart’ta “şu an seçili varyant” bilgisini sahne unload olurken kaybetmemek için DontDestroyOnLoad taşır.
/// </summary>
public sealed class ShadowMatchVariantRestartCarrier : MonoBehaviour
{
    private const int StaleCarrierMaxFrames = 180;

    public string TargetSceneName;
    public int ExcludeVariantRootIndex;
    public int SpawnedAtFrame;

    public static void Register(string targetSceneName, int excludeVariantRootIndex)
    {
        ShadowMatchVariantRestartCarrier[] old = Object.FindObjectsByType<ShadowMatchVariantRestartCarrier>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < old.Length; i++)
        {
            if (old[i] != null && old[i].TargetSceneName == targetSceneName)
                Object.Destroy(old[i].gameObject);
        }

        GameObject go = new GameObject("[ShadowMatchVariantRestart]");
        ShadowMatchVariantRestartCarrier c = go.AddComponent<ShadowMatchVariantRestartCarrier>();
        c.TargetSceneName = targetSceneName;
        c.ExcludeVariantRootIndex = excludeVariantRootIndex;
        c.SpawnedAtFrame = Time.frameCount;
        Object.DontDestroyOnLoad(go);
    }

    public static bool TryTakeExcludeForScene(string loadedSceneName, out int excludeVariantRootIndex)
    {
        excludeVariantRootIndex = -1;

        ShadowMatchVariantRestartCarrier[] list = Object.FindObjectsByType<ShadowMatchVariantRestartCarrier>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < list.Length; i++)
        {
            ShadowMatchVariantRestartCarrier c = list[i];
            if (c == null || string.IsNullOrEmpty(c.TargetSceneName))
                continue;

            int age = Time.frameCount - c.SpawnedAtFrame;
            if (age < 0 || age > StaleCarrierMaxFrames)
            {
                Object.Destroy(c.gameObject);
                continue;
            }

            if (c.TargetSceneName != loadedSceneName)
                continue;

            excludeVariantRootIndex = c.ExcludeVariantRootIndex;
            Object.Destroy(c.gameObject);
            return true;
        }

        return false;
    }
}
