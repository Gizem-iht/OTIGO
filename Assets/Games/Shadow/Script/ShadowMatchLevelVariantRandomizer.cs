using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aynı level içinde birden fazla "varyant" kök GameObject tanımlayıp her sahne yüklemesinde
/// yalnızca birini açar (replay / restart sonrası farklı görsel, aynı mekanik).
/// Birden fazla varyant varken kayıt anahtarları <see cref="ActiveVariantIndex"/> ile ayrılır.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ShadowMatchLevelVariantRandomizer : MonoBehaviour
{
    public static int ActiveVariantIndex { get; private set; }

    /// <summary>
    /// İki veya daha fazla varyant kullanılıyorsa true; tek set ise eski save prefix korunur.
    /// </summary>
    public static bool UsesVariantSaveSuffix { get; private set; }

    [Tooltip("Her biri içinde tam bir oyun seti (ör. MoveS + ShadowS) olan üst objeler.")]
    [SerializeField]
    private GameObject[] variantRoots = System.Array.Empty<GameObject>();

    [Tooltip("Birden fazla varyant varsa bir önceki yüklemede seçileni mümkünse tekrar seçme.")]
    [SerializeField]
    private bool avoidRepeatWhenPossible = true;

    /// <summary>
    /// Sahneyi <see cref="SceneManager.LoadScene"/> ile yeniden yüklemeden hemen önce çağrılır.
    /// Oyunda gerçekten açık olan varyant kökünü yazıcıya kaydeder; statik indeks yanlış olsa bile tutarlıdır.
    /// </summary>
    public static void MarkReloadShouldPreferDifferentVariant()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
            return;

        ShadowMatchLevelVariantRandomizer rnd = FindRandomizerInScene(active);
        if (rnd == null || !rnd.HasMultipleVariantRoots())
            return;

        string sn = active.name;
        string lastKey = LastPickPrefsKey(sn);
        // Son seçilen kök her zaman burada; hiyerarşi (ikisi de açık görünse) yanıltıcı olmasın.
        int exclude = PlayerPrefs.HasKey(lastKey)
            ? PlayerPrefs.GetInt(lastKey)
            : rnd.GetIndexOfActiveVariantRoot();

        ShadowMatchVariantRestartCarrier.Register(sn, exclude);
        PlayerPrefs.SetInt(RestartExcludePrefsKey(sn), exclude);
        PlayerPrefs.Save();
    }

    private static ShadowMatchLevelVariantRandomizer FindRandomizerInScene(Scene scene)
    {
        foreach (ShadowMatchLevelVariantRandomizer r in FindObjectsByType<ShadowMatchLevelVariantRandomizer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r == null || !r.gameObject.scene.IsValid())
                continue;

            if (r.gameObject.scene.handle != scene.handle)
                continue;

            return r;
        }

        return null;
    }

    /// <summary>
    /// Oyunda kullanıcıya gösterilen (açık) varyant kökünün <see cref="variantRoots"/> indeksi.
    /// </summary>
    public int GetIndexOfActiveVariantRoot()
    {
        if (variantRoots == null)
            return 0;

        int lastActiveSelf = -1;
        int activeSelfCount = 0;
        for (int i = 0; i < variantRoots.Length; i++)
        {
            GameObject go = variantRoots[i];
            if (go == null || !go.activeSelf)
                continue;

            activeSelfCount++;
            lastActiveSelf = i;
        }

        if (activeSelfCount == 1)
            return lastActiveSelf;

        if (activeSelfCount > 1 &&
            ActiveVariantIndex >= 0 &&
            ActiveVariantIndex < variantRoots.Length &&
            variantRoots[ActiveVariantIndex] != null &&
            variantRoots[ActiveVariantIndex].activeSelf)
            return ActiveVariantIndex;

        int lastHierarchy = -1;
        int hierarchyCount = 0;
        for (int i = 0; i < variantRoots.Length; i++)
        {
            GameObject go = variantRoots[i];
            if (go == null || !go.activeInHierarchy)
                continue;

            hierarchyCount++;
            lastHierarchy = i;
        }

        if (hierarchyCount == 1)
            return lastHierarchy;

        if (hierarchyCount > 1 &&
            ActiveVariantIndex >= 0 &&
            ActiveVariantIndex < variantRoots.Length &&
            variantRoots[ActiveVariantIndex] != null &&
            variantRoots[ActiveVariantIndex].activeInHierarchy)
            return ActiveVariantIndex;

        int maxIx = Mathf.Max(0, variantRoots.Length - 1);
        if (lastActiveSelf >= 0)
            return Mathf.Clamp(lastActiveSelf, 0, maxIx);
        if (lastHierarchy >= 0)
            return Mathf.Clamp(lastHierarchy, 0, maxIx);

        return Mathf.Clamp(ActiveVariantIndex, 0, maxIx);
    }

    public bool HasMultipleVariantRoots()
    {
        return CountValidVariantRoots() >= 2;
    }

    private int CountValidVariantRoots()
    {
        if (variantRoots == null || variantRoots.Length == 0)
            return 0;

        int n = 0;
        for (int i = 0; i < variantRoots.Length; i++)
        {
            if (variantRoots[i] != null)
                n++;
        }

        return n;
    }

    private static string LastPickPrefsKey(string sceneName)
    {
        return "ShadowMatch_VariantPick_" + sceneName;
    }

    private static string RestartExcludePrefsKey(string sceneName)
    {
        return "ShadowMatch_RestartExclude_" + sceneName;
    }

    private void Awake()
    {
        UsesVariantSaveSuffix = false;
        ActiveVariantIndex = 0;
        ApplyRandomVariant();
    }

    private void ApplyRandomVariant()
    {
        if (variantRoots == null || variantRoots.Length == 0)
            return;

        var validIndices = new List<int>(variantRoots.Length);
        for (int i = 0; i < variantRoots.Length; i++)
        {
            if (variantRoots[i] != null)
                validIndices.Add(i);
        }

        if (validIndices.Count == 0)
            return;

        if (validIndices.Count == 1)
        {
            variantRoots[validIndices[0]].SetActive(true);
            UsesVariantSaveSuffix = false;
            ActiveVariantIndex = validIndices[0];
            return;
        }

        foreach (int i in validIndices)
            variantRoots[i].SetActive(false);

        string sceneName = SceneManager.GetActiveScene().name;

        if (TryConsumeRestartExclude(sceneName, validIndices, out int forcedChoice))
        {
            PlayerPrefs.SetInt(LastPickPrefsKey(sceneName), forcedChoice);
            PlayerPrefs.Save();

            ActiveVariantIndex = forcedChoice;
            UsesVariantSaveSuffix = true;
            variantRoots[forcedChoice].SetActive(true);
            return;
        }

        int lastPick = PlayerPrefs.GetInt(LastPickPrefsKey(sceneName), -1);

        int choiceIndexInList = Random.Range(0, validIndices.Count);

        if (avoidRepeatWhenPossible && validIndices.Count > 1)
        {
            for (int attempt = 0;
                 attempt < 12 &&
                 validIndices[choiceIndexInList] == lastPick;
                 attempt++)
            {
                choiceIndexInList = Random.Range(0, validIndices.Count);
            }
        }

        int chosenRoot = validIndices[choiceIndexInList];
        PlayerPrefs.SetInt(LastPickPrefsKey(sceneName), chosenRoot);
        PlayerPrefs.Save();

        ActiveVariantIndex = chosenRoot;
        UsesVariantSaveSuffix = true;
        variantRoots[chosenRoot].SetActive(true);
    }

    private static bool TryConsumeRestartExclude(
        string sceneName,
        List<int> validIndices,
        out int chosenRoot)
    {
        if (ShadowMatchVariantRestartCarrier.TryTakeExcludeForScene(sceneName, out int carrierExclude) &&
            PickVariantExcluding(validIndices, carrierExclude, out chosenRoot))
        {
            string backupKey = RestartExcludePrefsKey(sceneName);
            if (PlayerPrefs.HasKey(backupKey))
            {
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.Save();
            }

            return true;
        }

        string prefsKey = RestartExcludePrefsKey(sceneName);
        if (!PlayerPrefs.HasKey(prefsKey))
        {
            chosenRoot = 0;
            return false;
        }

        int exclude = PlayerPrefs.GetInt(prefsKey, -1);
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.Save();

        return PickVariantExcluding(validIndices, exclude, out chosenRoot);
    }

    private static bool PickVariantExcluding(
        List<int> validIndices,
        int exclude,
        out int chosenRoot)
    {
        chosenRoot = 0;
        var candidates = new List<int>(validIndices.Count);
        for (int i = 0; i < validIndices.Count; i++)
        {
            int idx = validIndices[i];
            if (idx != exclude)
                candidates.Add(idx);
        }

        if (candidates.Count == 0)
            candidates.AddRange(validIndices);

        chosenRoot = candidates.Count == 1 ? candidates[0] : candidates[Random.Range(0, candidates.Count)];
        return true;
    }
}
