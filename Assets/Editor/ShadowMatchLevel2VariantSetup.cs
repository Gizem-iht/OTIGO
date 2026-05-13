#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Shadow Match Level 2: GameArea altındaki oyun nesnelerini Variant_L2_A / Variant_L2_B olarak sarar,
/// ShadowMatchLevelVariantRandomizer bağlar ve LevelController gölgelerini günceller.
/// Unity menüsünden bir kez çalıştır; sahneyi kaydeder.
/// </summary>
public static class ShadowMatchLevel2VariantSetup
{
    private const string Level2ScenePath = "Assets/Games/Shadow/level2/ShadowMatch_Level2.unity";

    [MenuItem("OTIGO/Shadow/Level 2 — Kur: Varyant A+B + Randomizer")]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog(
                "Shadow Level 2",
                "ShadowMatch_Level2 sahnesinde GameArea’nın doğrudan çocukları (cat, dog, gölgeler) " +
                "Variant_L2_A altına alınır, kopya Variant_L2_B oluşturulur ve Randomizer eklenir. Devam?",
                "Evet",
                "İptal"))
            return;

        var scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);

        var gameAreaGo = GameObject.Find("GameArea");
        if (gameAreaGo == null)
        {
            EditorUtility.DisplayDialog("Hata", "GameArea bulunamadı.", "Tamam");
            return;
        }

        Transform ga = gameAreaGo.transform;

        if (ga.Find("Variant_L2_A") != null)
        {
            EditorUtility.DisplayDialog(
                "Shadow Level 2",
                "Variant_L2_A zaten kurulu. Tekrar kurmak için sahneyi geri al veya Variant_L2_A/B ile Level2_VariantRandom objelerini el ile sil.",
                "Tamam");
            return;
        }

        DestroyIfExists(GameObject.Find("Level2_VariantRandom"));

        var children = new List<Transform>(ga.childCount);
        for (int i = 0; i < ga.childCount; i++)
            children.Add(ga.GetChild(i));

        if (children.Count < 4)
        {
            EditorUtility.DisplayDialog(
                "Uyarı",
                "GameArea altında en az 4 nesne bekleniyordu (şu an: " + children.Count + ").",
                "Tamam");
            return;
        }

        var variantA = new GameObject("Variant_L2_A");
        variantA.transform.SetParent(ga, false);
        variantA.transform.localPosition = Vector3.zero;
        variantA.transform.localRotation = Quaternion.identity;
        variantA.transform.localScale = Vector3.one;

        foreach (var ch in children)
            ch.SetParent(variantA.transform, true);

        var variantB = Object.Instantiate(variantA, ga);
        variantB.name = "Variant_L2_B";

        TintColoredPiecesSlightly(variantB);

        variantB.SetActive(false);

        GameObject rnd = new GameObject("Level2_VariantRandom");
        rnd.transform.SetParent(ga, false);
        rnd.transform.localPosition = Vector3.zero;

        var randomizer = rnd.AddComponent<ShadowMatchLevelVariantRandomizer>();

        SerializedObject sor = new SerializedObject(randomizer);
        SerializedProperty vr = sor.FindProperty("variantRoots");
        vr.arraySize = 2;
        vr.GetArrayElementAtIndex(0).objectReferenceValue = variantA;
        vr.GetArrayElementAtIndex(1).objectReferenceValue = variantB;

        SerializedProperty apr = sor.FindProperty("avoidRepeatWhenPossible");
        if (apr != null)
            apr.boolValue = true;

        sor.ApplyModifiedPropertiesWithoutUndo();

        var levelController = GameObject.Find("LevelController")
            ?.GetComponent<ShadowMatchLevelController>();
        if (levelController != null)
        {
            var shadowTargets = new List<Transform>();
            CollectDistinctShadowTargets(variantA, shadowTargets);
            CollectDistinctShadowTargets(variantB, shadowTargets);

            SerializedObject sol = new SerializedObject(levelController);
            SerializedProperty all = sol.FindProperty("allShadows");
            all.ClearArray();
            for (int i = 0; i < shadowTargets.Count; i++)
            {
                all.InsertArrayElementAtIndex(i);
                all.GetArrayElementAtIndex(i).objectReferenceValue = shadowTargets[i];
            }

            sol.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Bitti",
            "Level 2 varyant yapısı kaydedildi.\n\n" +
            "• Variant_L2_B şimdilik Variant_A’nın çoğaltması + hafif renk düşümü ile ayırt edilir.\n" +
            "• Gerçek “başka nesne” görsellerini Unity’de Variant_L2_B altındaki sprite’ları değiştirerek ver.\n\n" +
            "Play modunda sahne yüklendiğinde randomizer iki varyanttan birini seçer.",
            "Tamam");
    }

    private static void DestroyIfExists(GameObject go)
    {
        if (go != null)
            Object.DestroyImmediate(go);
    }

    /// <summary>
    /// Gölgeleri (çok sayıda siyah) bozmadan, renkli parçaya hafif ton verir ki B’nin seçildiği anında fark görülsün.
    /// </summary>
    private static void TintColoredPiecesSlightly(GameObject root)
    {
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            float lum = (c.r + c.g + c.b) / 3f;

            // Silüetleri (sıkça sıfıra yakın parlaklık) atla
            if (lum < 0.06f && c.a > 0.5f && c.r + c.g + c.b < 0.2f)
                continue;

            sr.color = new Color(
                Mathf.Clamp01(c.r * 0.92f + 0.04f),
                Mathf.Clamp01(c.g * 0.93f),
                Mathf.Clamp01(c.b * 1.05f),
                c.a);
        }
    }

    private static void CollectDistinctShadowTargets(GameObject variantRoot, List<Transform> list)
    {
        foreach (ShadowMatchDragSnap snap in variantRoot.GetComponentsInChildren<ShadowMatchDragSnap>(true))
        {
            if (snap == null || snap.target == null)
                continue;

            if (!list.Contains(snap.target))
                list.Add(snap.target);
        }
    }
}
#endif
