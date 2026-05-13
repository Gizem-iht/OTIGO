#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Shadow Match Level 8 (<c>ShadowMatch_Level8</c>): <c>game</c> altındaki oyun kökleri
/// Variant_L8_A / Variant_L8_B olarak sarılır ve randomizer bağlanır.
/// </summary>
public static class ShadowMatchLevel8VariantSetup
{
    private const string Level8ScenePath = "Assets/Games/Shadow/level8/ShadowMatch_Level8.unity";

    private const string GameplayRootName = "game";

    [MenuItem("OTIGO/Shadow/Level 8 — Kur: Varyant A+B + Randomizer")]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog(
                "Shadow Level 8",
                "ShadowMatch_Level8 sahnesinde \"" + GameplayRootName +
                "\" objesinin doğrudan çocukları Variant_L8_A altına alınır, kopya Variant_L8_B " +
                "oluşturulur ve Randomizer eklenir. Devam?",
                "Evet",
                "İptal"))
            return;

        var scene = EditorSceneManager.OpenScene(Level8ScenePath, OpenSceneMode.Single);

        var rootGo = GameObject.Find(GameplayRootName);
        if (rootGo == null)
        {
            EditorUtility.DisplayDialog("Hata", "\"" + GameplayRootName + "\" bulunamadı.", "Tamam");
            return;
        }

        Transform root = rootGo.transform;

        if (root.Find("Variant_L8_A") != null)
        {
            EditorUtility.DisplayDialog(
                "Shadow Level 8",
                "Variant_L8_A zaten kurulu. Tekrar kurmak için Variant_L8_A/B ve Level8_VariantRandom " +
                "objelerini sil veya Undo ile geri al.",
                "Tamam");
            return;
        }

        DestroyIfExists(GameObject.Find("Level8_VariantRandom"));

        var children = new List<Transform>(root.childCount);
        for (int i = 0; i < root.childCount; i++)
            children.Add(root.GetChild(i));

        if (children.Count < 2)
        {
            EditorUtility.DisplayDialog(
                "Uyarı",
                "\"" + GameplayRootName + "\" altında en az 2 nesne bekleniyordu (Arac + Shadow; şu an: " +
                children.Count + ").",
                "Tamam");
            return;
        }

        var variantA = new GameObject("Variant_L8_A");
        variantA.transform.SetParent(root, false);
        variantA.transform.localPosition = Vector3.zero;
        variantA.transform.localRotation = Quaternion.identity;
        variantA.transform.localScale = Vector3.one;

        foreach (var ch in children)
            ch.SetParent(variantA.transform, true);

        var variantB = Object.Instantiate(variantA, root);
        variantB.name = "Variant_L8_B";

        TintColoredPiecesSlightly(variantB);
        variantB.SetActive(false);

        GameObject rnd = new GameObject("Level8_VariantRandom");
        rnd.transform.SetParent(null, false);
        rnd.transform.localPosition = Vector3.zero;
        rnd.transform.localRotation = Quaternion.identity;
        rnd.transform.localScale = Vector3.one;

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

        var levelController = GameObject.Find("LevelController")?.GetComponent<ShadowMatchLevelController>();
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
            "Level 8 varyant yapısı kaydedildi.\n\n" +
            "• Variant_L8_B şimdilik kopya + hafif renk tonudur.\n" +
            "• Farklı nesneler için Variant_L8_B içinde sprite’ları değiştir.\n\n" +
            "Restart / rastgele set davranışı diğer level’larla aynıdır.",
            "Tamam");
    }

    private static void DestroyIfExists(GameObject go)
    {
        if (go != null)
            Object.DestroyImmediate(go);
    }

    private static void TintColoredPiecesSlightly(GameObject root)
    {
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            float lum = (c.r + c.g + c.b) / 3f;

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
