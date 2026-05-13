using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OppositesPairPoolMenu
{
    private static readonly string[] OppositesLevelScenePaths =
    {
        "Assets/Games/Opposite/schene/Opposites_Level1.unity",
        "Assets/Games/Opposite/schene/Opposites_Level2.unity",
        "Assets/Games/Opposite/schene/Opposites_Level3.unity",
        "Assets/Games/Opposite/schene/Opposites_Level4.unity",
        "Assets/Games/Opposite/schene/Opposites_Level5.unity",
        "Assets/Games/Opposite/schene/Opposites_Level6.unity",
    };

    [MenuItem("OTIGO/Opposites/Kartlardan Çift Havuzunu Aktif Sahneye Yaz")]
    public static void ApplyPoolToActiveScene()
    {
        OppositesMemoryLevelManager mgr = Object.FindFirstObjectByType<OppositesMemoryLevelManager>();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog(
                "Opposites",
                "Aktif sahnede OppositesMemoryLevelManager bulunamadı.",
                "Tamam");
            return;
        }

        Undo.RecordObject(mgr, "Opposite havuz kartlardan");
        mgr.randomizeOppositePairsFromPool = true;
        mgr.RebuildOppositePairPoolFromInspectors();
        EditorUtility.SetDirty(mgr);
        UnityEngine.SceneManagement.Scene scene = mgr.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Opposites",
            "Çift havuzu (" + mgr.oppositePairPool.Count + " çift) yazıldı ve sahne kaydedildi.",
            "Tamam");
    }

    [MenuItem("OTIGO/Opposites/Tüm Level Sahnelerinde Çift Havuzunu Güncelle")]
    public static void ApplyPoolToAllLevelScenes()
    {
        int updated = 0;

        for (int i = 0; i < OppositesLevelScenePaths.Length; i++)
        {
            string path = OppositesLevelScenePaths[i];
            if (!AssetDatabase.LoadAssetAtPath<Object>(path))
            {
                Debug.LogWarning("[Opposites] Dosya yok: " + path);
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            OppositesMemoryLevelManager mgr = Object.FindFirstObjectByType<OppositesMemoryLevelManager>();
            if (mgr == null)
            {
                Debug.LogWarning("[Opposites] Sahne içinde manager yok, atlanıyor: " + path);
                continue;
            }

            Undo.RecordObject(mgr, "Opposite havuz toplu");
            mgr.randomizeOppositePairsFromPool = true;
            mgr.RebuildOppositePairPoolFromInspectors();
            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            updated++;
        }

        EditorUtility.DisplayDialog(
            "Opposites",
            updated + " sahne güncellendi.",
            "Tamam");
    }

    [MenuItem("OTIGO/Opposites/EXTRA - Diğer levellerin çiftleri (tüm sahneler)")]
    [MenuItem("OTIGO/Extra Opposites Havuz (tum sahneler)")]
    public static void MergeOtherLevelsIntoExtraOppositePoolAllScenes()
    {
        List<OppositePairConfig>[] snapshots = new List<OppositePairConfig>[OppositesLevelScenePaths.Length];

        for (int i = 0; i < OppositesLevelScenePaths.Length; i++)
        {
            string path = OppositesLevelScenePaths[i];
            if (!AssetDatabase.LoadAssetAtPath<Object>(path))
            {
                Debug.LogWarning("[Opposites] Dosya yok: " + path);
                snapshots[i] = new List<OppositePairConfig>();
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            OppositesMemoryLevelManager mgr = Object.FindFirstObjectByType<OppositesMemoryLevelManager>();
            if (mgr == null)
            {
                Debug.LogWarning("[Opposites] Manager yok: " + path);
                snapshots[i] = new List<OppositePairConfig>();
                continue;
            }

            Undo.RecordObject(mgr, "Opposite snapshot havuz");
            mgr.RebuildOppositePairPoolFromInspectors();
            snapshots[i] = DeepCopyPool(mgr.oppositePairPool);
            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        int updated = 0;
        for (int i = 0; i < OppositesLevelScenePaths.Length; i++)
        {
            string path = OppositesLevelScenePaths[i];
            if (!AssetDatabase.LoadAssetAtPath<Object>(path))
                continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            OppositesMemoryLevelManager mgr = Object.FindFirstObjectByType<OppositesMemoryLevelManager>();
            if (mgr == null)
                continue;

            HashSet<string> ownKeys = BuildConceptKeys(snapshots[i]);
            var extra = new List<OppositePairConfig>();
            var extraKeys = new HashSet<string>();

            for (int j = 0; j < snapshots.Length; j++)
            {
                if (j == i) continue;
                foreach (OppositePairConfig p in snapshots[j])
                {
                    string k = StablePairConceptKey(p);
                    if (string.IsNullOrEmpty(k)) continue;
                    if (ownKeys.Contains(k)) continue;
                    if (!extraKeys.Add(k)) continue;
                    extra.Add(DeepCopyPair(p));
                }
            }

            Undo.RecordObject(mgr, "Opposite extra havuz birleştir");
            mgr.randomizeOppositePairsFromPool = true;
            mgr.extraOppositePairPool = extra;
            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            updated++;
        }

        EditorUtility.DisplayDialog(
            "Opposites",
            updated + " sahneye extra havuz yazildi. Restart sonrasinda havuz tahtadan buyuk oldugu icin farkli ciftler secilebilir.",
            "Tamam");
    }

    private static List<OppositePairConfig> DeepCopyPool(List<OppositePairConfig> src)
    {
        var list = new List<OppositePairConfig>();
        if (src == null) return list;
        foreach (OppositePairConfig c in src)
            list.Add(DeepCopyPair(c));
        return list;
    }

    private static OppositePairConfig DeepCopyPair(OppositePairConfig c)
    {
        if (c == null)
            return new OppositePairConfig { sideOne = new OppositeFaceConfig(), sideTwo = new OppositeFaceConfig() };

        return new OppositePairConfig
        {
            sideOne = DeepCopyFace(c.sideOne),
            sideTwo = DeepCopyFace(c.sideTwo)
        };
    }

    private static OppositeFaceConfig DeepCopyFace(OppositeFaceConfig f)
    {
        if (f == null)
            return new OppositeFaceConfig();

        return new OppositeFaceConfig
        {
            conceptId = f.conceptId,
            frontSprite = f.frontSprite,
            cardVoiceClip = f.cardVoiceClip
        };
    }

    private static HashSet<string> BuildConceptKeys(List<OppositePairConfig> pairs)
    {
        var set = new HashSet<string>();
        if (pairs == null) return set;

        foreach (OppositePairConfig p in pairs)
        {
            string k = StablePairConceptKey(p);
            if (!string.IsNullOrEmpty(k))
                set.Add(k);
        }

        return set;
    }

    private static string StablePairConceptKey(OppositePairConfig c)
    {
        if (c == null || c.sideOne == null || c.sideTwo == null)
            return null;

        string a = c.sideOne.conceptId ?? "";
        string b = c.sideTwo.conceptId ?? "";
        int cmp = string.CompareOrdinal(a, b);
        return cmp <= 0 ? a + "\u001f" + b : b + "\u001f" + a;
    }
}
